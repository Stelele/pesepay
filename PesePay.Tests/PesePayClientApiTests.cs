using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PesePay.Crypto;
using PesePay.Domain;

namespace PesePay.Tests;

public class PesePayClientApiTests
{
    private static JsonSerializerOptions ApiOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task InitiateTransactionAsync_Sends_Encrypted_Payload()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new InitiateResponse("REF001", new Uri("https://poll.example.com/REF001"), new Uri("https://redirect.example.com/REF001"));
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") };
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result",
            ReturnUrl = "https://example.com/return"
        };

        var txn = new Transaction(
            new Amount(10m, CurrencyCode.Usd),
            "Test payment");

        var result = await client.InitiateTransactionAsync(txn);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("REF001", result.Data.ReferenceNumber);
        Assert.Equal("https://poll.example.com/REF001", result.Data.PollUrl.ToString());
        Assert.Equal("https://redirect.example.com/REF001", result.Data.RedirectUrl.ToString());
    }

    [Fact]
    public async Task InitiateTransactionAsync_Throws_When_ResultUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var txn = new Transaction(
            new Amount(10m, CurrencyCode.Usd),
            "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
    }

    [Fact]
    public async Task InitiateTransactionAsync_Throws_When_ReturnUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result"
        };

        var txn = new Transaction(
            new Amount(10m, CurrencyCode.Usd),
            "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
    }

    [Fact]
    public async Task MakeSeamlessPaymentAsync_Sends_Encrypted_Payload()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse("REF002", new Uri("https://poll.example.com/REF002"), null, "SUCCESS");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") };
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result"
        };

        var payment = new Payment(CurrencyCode.Zwl, "ecocash", new Customer("a@b.com", null, null));

        var result = await client.MakeSeamlessPaymentAsync(payment, "Invoice #456", 500m, new Dictionary<string, string> { { "field1", "val1" } });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("REF002", result.Data.ReferenceNumber);
        Assert.True(result.Data.IsPaid);
        Assert.Null(result.Data.RedirectUrl);
    }

    [Fact]
    public async Task MakeSeamlessPaymentAsync_Throws_When_ResultUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var payment = new Payment(CurrencyCode.Usd, "visa", new Customer("a@b.com", null, null));

        await Assert.ThrowsAsync<PesePayException>(() =>
            client.MakeSeamlessPaymentAsync(payment, "test", 10m));
    }

    [Fact]
    public async Task CheckPaymentStatusAsync_Returns_PaymentStatus()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedStatus = new PaymentStatus("REF003", new Uri("https://poll.example.com/REF003"), null, "SUCCESS");
        var responseJson = JsonSerializer.Serialize(expectedStatus, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") };
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var result = await client.CheckPaymentStatusAsync("REF003");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
        Assert.Equal("REF003", result.Data.ReferenceNumber);
    }

    [Fact]
    public async Task PollTransactionAsync_Returns_PaymentStatus()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedStatus = new PaymentStatus("REF004", new Uri("https://poll.example.com/REF004"), null, "PENDING");
        var responseJson = JsonSerializer.Serialize(expectedStatus, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var result = await client.PollTransactionAsync(new Uri("https://poll.example.com/REF004"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsPaid);
    }
}

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _responseContent = "{}";

    public void SetResponse(HttpStatusCode code, string content)
    {
        _statusCode = code;
        _responseContent = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
        });
    }
}
