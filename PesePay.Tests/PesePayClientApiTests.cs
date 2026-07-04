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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task InitiateTransactionAsync_Sends_Encrypted_Payload()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new InitiateResponse("REF001", new Uri("https://poll.example.com/REF001"), new Uri("https://redirect.example.com/REF001"), "INT-001", "INITIATED", 0, "Initiated");
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
            new Amount(10m, CurrencyCode.USD),
            "Test payment");

        var result = await client.InitiateTransactionAsync(txn);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("REF001", result.Data.ReferenceNumber);
        Assert.Equal("https://poll.example.com/REF001", result.Data.PollUrl.ToString());
        Assert.Equal("https://redirect.example.com/REF001", result.Data.RedirectUrl.ToString());
        Assert.Equal("INT-001", result.Data.InternalReference);
    }

    [Fact]
    public async Task InitiateTransactionAsync_Throws_When_ResultUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
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
            new Amount(10m, CurrencyCode.USD),
            "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
    }

    [Fact]
    public async Task MakeSeamlessPaymentAsync_Sends_Encrypted_Payload()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse("REF002", new Uri("https://poll.example.com/REF002"), null, "INT-002", "SUCCESS", 1, "Payment successful");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result"
        };

        var payment = new Payment(CurrencyCode.ZWL, "PZW211", new Customer("a@b.com", null, null));

        var result = await client.MakeSeamlessPaymentAsync(payment, "Invoice #456", 500m, "ORDER-001", new Dictionary<string, string> { { "customerPhoneNumber", "0771234567" } });

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

        var payment = new Payment(CurrencyCode.USD, "PZW211", new Customer("a@b.com", null, null));

        await Assert.ThrowsAsync<PesePayException>(() =>
            client.MakeSeamlessPaymentAsync(payment, "test", 10m, "MERCH01"));
    }

    [Fact]
    public async Task CheckPaymentStatusAsync_Returns_PaymentStatus()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedStatus = new PaymentStatus("REF003", new Uri("https://poll.example.com/REF003"), null, "INT-003", "SUCCESS", 1, "Success");
        var responseJson = JsonSerializer.Serialize(expectedStatus, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
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
        var expectedStatus = new PaymentStatus("REF004", new Uri("https://poll.example.com/REF004"), null, "INT-004", "PENDING", 2, "Pending");
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

    [Fact]
    public async Task GetActiveCurrenciesAsync_Returns_Currencies()
    {
        var expectedCurrencies = new List<CurrencyInfo>
        {
            new(true, "USD", true, "United States Dollar", 1, "United States Dollar"),
            new(true, "ZWL", false, "Zimbabwe Dollar", 2, "Zimbabwe Dollar")
        };

        var handler = new FakeHttpMessageHandler();
        var responseJson = JsonSerializer.Serialize(expectedCurrencies, ApiOptions);
        handler.SetResponse(HttpStatusCode.OK, responseJson);

        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var result = await client.GetActiveCurrenciesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("USD", result.Data[0].Code);
        Assert.Equal("ZWL", result.Data[1].Code);
    }

    [Fact]
    public async Task GetPaymentMethodsAsync_Returns_Methods()
    {
        var expectedMethods = new List<PaymentMethodInfo>
        {
            new(true, "PZW211", new List<string> { "ZWL" }, "EcoCash ZWL", 1, 50000m, 1m, "EcoCash ZWL", "Processing...", true, "https://pay.example.com", new List<PaymentMethodRequiredField>())
        };

        var handler = new FakeHttpMessageHandler();
        var responseJson = JsonSerializer.Serialize(expectedMethods, ApiOptions);
        handler.SetResponse(HttpStatusCode.OK, responseJson);

        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var result = await client.GetPaymentMethodsAsync("ZWL");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("PZW211", result.Data![0].Code);
    }

    [Fact]
    public async Task InitiateTransactionAsync_Convenience_Sets_Urls()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new InitiateResponse("REF-CONV", new Uri("https://poll.example.com/REF-CONV"), new Uri("https://redirect.example.com/REF-CONV"), "INT-CONV", "INITIATED", 0, "Initiated");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") };
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var result = await client.InitiateTransactionAsync(
            10m, CurrencyCode.USD, "Test reason", "MERCH-CONV",
            resultUrl: "https://example.com/result",
            returnUrl: "https://example.com/return");

        Assert.True(result.IsSuccess);
        Assert.Equal("REF-CONV", result.Data!.ReferenceNumber);
        Assert.Equal("https://example.com/result", client.ResultUrl);
        Assert.Equal("https://example.com/return", client.ReturnUrl);
    }

    [Fact]
    public async Task MakeSeamlessPaymentAsync_MobileMoney_Convenience_Succeeds()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse("REF-MOBILE", new Uri("https://poll.example.com/REF-MOBILE"), null, "INT-MOBILE", "SUCCESS", 1, "Paid");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result"
        };

        var result = await client.MakeSeamlessPaymentAsync(
            PaymentMethodCode.EcoCash, CurrencyCode.ZiG, 500m,
            "0771234567", "John Doe", "Invoice #456", "ORDER-001");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
        Assert.Equal("REF-MOBILE", result.Data.ReferenceNumber);
    }

    [Fact]
    public async Task MakeSeamlessCardPaymentAsync_Convenience_Succeeds()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse("REF-CARD", new Uri("https://poll.example.com/REF-CARD"), null, "INT-CARD", "SUCCESS", 1, "Paid");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result"
        };

        var card = new CardDetails("4867960000005461", "608", "12/30", "John Doe");
        var result = await client.MakeSeamlessCardPaymentAsync(
            PaymentMethodCode.EcoCash, CurrencyCode.USD, 10m,
            card, "john@example.com", "John Doe", "Card payment", "ORDER-CARD");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
        Assert.Equal("REF-CARD", result.Data.ReferenceNumber);
    }

    [Fact]
    public async Task MakeSeamlessCardPaymentAsync_Without_HolderName()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse("REF-CARD2", new Uri("https://poll.example.com/REF-CARD2"), null, "INT-CARD2", "SUCCESS", 1, "Paid");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result"
        };

        var card = new CardDetails("4867960000005461", "608", "12/30");
        var result = await client.MakeSeamlessCardPaymentAsync(
            PaymentMethodCode.EcoCash, CurrencyCode.USD, 10m,
            card, "test@example.com", null, "Card payment", "ORDER-CARD2");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
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
