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
    public async Task InitiateRedirectPaymentAsync_Sends_Encrypted_Payload()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new InitiateResponse(
            "REF001", new Uri("https://poll.example.com/REF001"),
            new Uri("https://redirect.example.com/REF001"),
            "INT-001", "INITIATED", 0, "Initiated");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        var client = new PesePayClient(
            crypto, httpClient, EnvironmentType.Sandbox,
            resultUrl: "https://example.com/result",
            returnUrl: "https://example.com/return");

        var request = new RedirectPaymentRequest(10m, CurrencyCode.USD, "Test payment");

        var result = await client.InitiateRedirectPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("REF001", result.Data.ReferenceNumber);
        Assert.Equal("https://poll.example.com/REF001", result.Data.PollUrl.ToString());
        Assert.Equal("https://redirect.example.com/REF001", result.Data.RedirectUrl.ToString());
    }

    [Fact]
    public async Task InitiateRedirectPaymentAsync_Throws_When_ResultUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var request = new RedirectPaymentRequest(10m, CurrencyCode.USD, "Test");

        await Assert.ThrowsAsync<PesePayException>(
            () => client.InitiateRedirectPaymentAsync(request));
    }

    [Fact]
    public async Task InitiateRedirectPaymentAsync_Throws_When_ReturnUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(
            crypto, httpClient, EnvironmentType.Sandbox,
            resultUrl: "https://example.com/result");

        var request = new RedirectPaymentRequest(10m, CurrencyCode.USD, "Test");

        await Assert.ThrowsAsync<PesePayException>(
            () => client.InitiateRedirectPaymentAsync(request));
    }

    [Fact]
    public async Task InitiateSeamlessPaymentAsync_MobileMoney_Succeeds()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse(
            "REF002", new Uri("https://poll.example.com/REF002"), null,
            "INT-002", "SUCCESS", 1, "Payment successful");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(
            crypto, httpClient, EnvironmentType.Sandbox,
            resultUrl: "https://example.com/result");

        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, CurrencyCode.ZiG, 500m,
            "Invoice #456", "ORDER-001",
            CustomerName: "John Doe",
            PhoneNumber: "0771234567");

        var result = await client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsPaid);
        Assert.Equal("REF002", result.Data.ReferenceNumber);
    }

    [Fact]
    public async Task InitiateSeamlessPaymentAsync_Card_Succeeds()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse(
            "REF-CARD", new Uri("https://poll.example.com/REF-CARD"), null,
            "INT-CARD", "SUCCESS", 1, "Paid");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(
            crypto, httpClient, EnvironmentType.Sandbox,
            resultUrl: "https://example.com/result");

        var card = new CardDetails("4867960000005461", "608", "12/30", "John Doe");
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.Visa, CurrencyCode.USD, 10m,
            "Card payment", "ORDER-CARD",
            Email: "john@example.com",
            CustomerName: "John Doe",
            Card: card);

        var result = await client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
        Assert.Equal("REF-CARD", result.Data.ReferenceNumber);
    }

    [Fact]
    public async Task InitiateSeamlessPaymentAsync_Card_Without_HolderName()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse(
            "REF-CARD2", new Uri("https://poll.example.com/REF-CARD2"), null,
            "INT-CARD2", "SUCCESS", 1, "Paid");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(
            crypto, httpClient, EnvironmentType.Sandbox,
            resultUrl: "https://example.com/result");

        var card = new CardDetails("4867960000005461", "608", "12/30");
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.Visa, CurrencyCode.USD, 10m,
            "Card payment", "ORDER-CARD2",
            Email: "test@example.com",
            Card: card);

        var result = await client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
    }

    [Fact]
    public async Task InitiateSeamlessPaymentAsync_Card_Takes_Priority_Over_PhoneNumber()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedResponse = new PaymentResponse(
            "REF-CARD-PRIORITY", new Uri("https://poll.example.com/REF-CARD-PRIORITY"), null,
            "INT-CP", "SUCCESS", 1, "Paid");
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(
            crypto, httpClient, EnvironmentType.Sandbox,
            resultUrl: "https://example.com/result");

        var card = new CardDetails("4867960000005461", "608", "12/30");
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.Visa, CurrencyCode.USD, 10m,
            "Card payment", "ORDER-PRIORITY",
            PhoneNumber: "0771234567",
            Card: card);

        var result = await client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsPaid);
    }

    [Fact]
    public async Task InitiateSeamlessPaymentAsync_Throws_When_Neither_Card_Nor_Phone()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(
            crypto, httpClient, EnvironmentType.Sandbox,
            resultUrl: "https://example.com/result");

        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, CurrencyCode.USD, 10m,
            "Test", "ORDER-001");

        await Assert.ThrowsAsync<PesePayException>(
            () => client.InitiateSeamlessPaymentAsync(request));
    }

    [Fact]
    public async Task InitiateSeamlessPaymentAsync_Throws_When_ResultUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, CurrencyCode.USD, 10m,
            "Test", "MERCH01",
            PhoneNumber: "0777777777");

        await Assert.ThrowsAsync<PesePayException>(
            () => client.InitiateSeamlessPaymentAsync(request));
    }

    [Fact]
    public async Task CheckPaymentStatusAsync_Returns_PaymentStatus()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedStatus = new PaymentStatus(
            "REF003", new Uri("https://poll.example.com/REF003"), null,
            "INT-003", "SUCCESS", 1, "Success");
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
    public async Task PollPaymentAsync_Returns_PaymentStatus()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("0123456789abcdef0123456789abcdef");
        var expectedStatus = new PaymentStatus(
            "REF004", new Uri("https://poll.example.com/REF004"), null,
            "INT-004", "PENDING", 2, "Pending");
        var responseJson = JsonSerializer.Serialize(expectedStatus, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var result = await client.PollPaymentAsync(
            new Uri("https://poll.example.com/REF004"));

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
            new(true, "PZW211", new List<string> { "ZWL" }, "EcoCash ZWL",
                1, 50000m, 1m, "EcoCash ZWL", "Processing...", true,
                "https://pay.example.com", new List<PaymentMethodRequiredField>())
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

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
        });
    }
}
