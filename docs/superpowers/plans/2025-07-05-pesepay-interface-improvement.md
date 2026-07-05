# PesePay Interface DevEx Improvement — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve DevEx by replacing verbose method overloads with request DTOs, removing redundant URL parameters from the interface, normalizing naming, and enabling XML doc shipping in the NuGet package.

**Architecture:** Two new request DTOs (`RedirectPaymentRequest`, `SeamlessPaymentRequest`) replace factory methods and long parameter lists. The interface collapses to 6 clean methods. `ResultUrl`/`ReturnUrl` become private on the client, set via constructor from DI config. The `Transaction` and `Payment` domain records remain for internal wire-format serialization.

**Tech Stack:** C# 12, .NET 8-10, xUnit, Moq-free tests

---

### Task 1: Create RedirectPaymentRequest DTO

**Files:**
- Create: `Domain/RedirectPaymentRequest.cs`

- [ ] **Step 1: Write RedirectPaymentRequest record**

```csharp
namespace PesePay.Domain;

/// <summary>
/// Request to initiate a redirect payment where the customer is sent to
/// a PesePay-hosted payment page to complete the transaction.
/// </summary>
/// <param name="Amount">The payment amount.</param>
/// <param name="Currency">Currency code (USD or ZWL).</param>
/// <param name="Reason">Reason for the payment.</param>
/// <param name="MerchantReference">Optional merchant reference.</param>
public record RedirectPaymentRequest(
    decimal Amount,
    CurrencyCode Currency,
    string Reason,
    string? MerchantReference = null);
```

- [ ] **Step 2: Verify it compiles**

```bash
dotnet build PesePay.csproj
```

Expected: Build succeeds.

---

### Task 2: Create SeamlessPaymentRequest DTO

**Files:**
- Create: `Domain/SeamlessPaymentRequest.cs`

- [ ] **Step 1: Write SeamlessPaymentRequest record**

```csharp
namespace PesePay.Domain;

/// <summary>
/// Request to initiate a seamless (server-to-server) payment.
/// Set <see cref="Card"/> for card payments or <see cref="PhoneNumber"/> for mobile money.
/// If both are set, <see cref="Card"/> takes priority.
/// </summary>
/// <param name="Method">The payment method (e.g. EcoCash, Visa).</param>
/// <param name="Currency">Currency code (USD or ZWL).</param>
/// <param name="Amount">The payment amount.</param>
/// <param name="Reason">Reason for the payment.</param>
/// <param name="MerchantReference">Your merchant reference for this transaction (required).</param>
/// <param name="Email">Customer email.</param>
/// <param name="CustomerName">Optional customer name.</param>
/// <param name="PhoneNumber">Customer phone number for mobile money payments.</param>
/// <param name="Card">Card details for card payments.</param>
public record SeamlessPaymentRequest(
    PaymentMethodCode Method,
    CurrencyCode Currency,
    decimal Amount,
    string Reason,
    string MerchantReference,
    string? Email = null,
    string? CustomerName = null,
    string? PhoneNumber = null,
    CardDetails? Card = null);
```

- [ ] **Step 2: Verify it compiles**

```bash
dotnet build PesePay.csproj
```

Expected: Build succeeds.

---

### Task 3: Rewrite IPesePayClient interface

**Files:**
- Modify: `IPesePayClient.cs` (entire file)

- [ ] **Step 1: Replace IPesePayClient.cs**

```csharp
using PesePay.Domain;

namespace PesePay;

/// <summary>
/// Client for interacting with the PesePay payment gateway.
/// Supports redirect payments, seamless payments, and payment status checking.
/// </summary>
/// <remarks>
/// Use <see cref="ServiceCollectionExtensions.AddPesePay(IServiceCollection, Action{PesePayConfiguration})"/>
/// for ASP.NET Core dependency injection, or construct directly with integration/encryption keys.
/// </remarks>
public interface IPesePayClient
{
    /// <summary>
    /// Initiates a redirect transaction where the customer is sent to a
    /// PesePay-hosted payment page.
    /// </summary>
    /// <param name="request">The redirect payment request details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the redirect URL, reference number, and poll URL on success.</returns>
    Task<PesepayResult<InitiateResponse>> InitiateRedirectPaymentAsync(
        RedirectPaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Makes a seamless (server-to-server) payment for mobile money or card.
    /// Set <see cref="SeamlessPaymentRequest.Card"/> for card payments or
    /// <see cref="SeamlessPaymentRequest.PhoneNumber"/> for mobile money.
    /// </summary>
    /// <param name="request">The seamless payment request details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the reference number, poll URL, and payment status on success.</returns>
    Task<PesepayResult<PaymentResponse>> InitiateSeamlessPaymentAsync(
        SeamlessPaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Checks the status of a payment by its reference number.
    /// </summary>
    /// <param name="referenceNumber">The payment reference number.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(
        string referenceNumber, CancellationToken ct = default);

    /// <summary>
    /// Checks the status of a payment by its poll URL.
    /// </summary>
    /// <param name="pollUrl">The poll URL from the payment response.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<PaymentStatus>> PollPaymentAsync(
        Uri pollUrl, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active currencies on the PesePay gateway.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<List<CurrencyInfo>>> GetActiveCurrenciesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets payment methods available for a given currency.
    /// </summary>
    /// <param name="currencyCode">The currency code (e.g. "USD" or "ZWL").</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PesepayResult<List<PaymentMethodInfo>>> GetPaymentMethodsAsync(
        string currencyCode, CancellationToken ct = default);
}
```

- [ ] **Step 2: Verify it compiles (PesePayClient.cs will have errors — expected)**

```bash
dotnet build PesePay.csproj
```

Expected: Build fails because PesePayClient.cs doesn't implement the new interface yet.

---

### Task 4: Rewrite PesePayClient implementation

**Files:**
- Modify: `PesePayClient.cs` (entire file)

- [ ] **Step 1: Replace PesePayClient.cs**

```csharp
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PesePay.Crypto;
using PesePay.Domain;

namespace PesePay;

public class PesePayClient : IPesePayClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _integrationKey;
    private readonly EnvironmentType _environment;
    private readonly IPayloadCrypto _crypto;
    private readonly HttpClient _httpClient;
    private readonly string? _resultUrl;
    private readonly string? _returnUrl;

    public PesePayClient(
        string integrationKey,
        string encryptionKey,
        EnvironmentType environment = EnvironmentType.Sandbox,
        string? resultUrl = null,
        string? returnUrl = null)
    {
        _integrationKey = integrationKey;
        _environment = environment;
        _crypto = new AesCbcPayloadCrypto(encryptionKey);
        _resultUrl = resultUrl;
        _returnUrl = returnUrl;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GetBaseUrl(environment)),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("authorization", integrationKey);
    }

    internal PesePayClient(
        IPayloadCrypto crypto,
        HttpClient httpClient,
        EnvironmentType environment = EnvironmentType.Sandbox,
        string? resultUrl = null,
        string? returnUrl = null)
    {
        _crypto = crypto;
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri(GetBaseUrl(environment));
        _environment = environment;
        _integrationKey = string.Empty;
        _resultUrl = resultUrl;
        _returnUrl = returnUrl;
    }

    private static string GetBaseUrl(EnvironmentType environment) => environment switch
    {
        EnvironmentType.Production => "https://api.pesepay.com/api/payments-engine/",
        EnvironmentType.Sandbox => "https://api.test.sandbox.pesepay.com/payments-engine/",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    private string MakePaymentPath => "v2/payments/make-payment";

    private static readonly Dictionary<(PaymentMethodCode, CurrencyCode), string> _methodCodes = new()
    {
        [(PaymentMethodCode.EcoCash, CurrencyCode.USD)] = "PZW211",
        [(PaymentMethodCode.EcoCash, CurrencyCode.ZiG)] = "PZW201",
        [(PaymentMethodCode.InnBucks, CurrencyCode.USD)] = "PZW212",
        [(PaymentMethodCode.Visa, CurrencyCode.USD)] = "PZW204",
        [(PaymentMethodCode.MasterCard, CurrencyCode.USD)] = "PZW205",
        [(PaymentMethodCode.Zimswitch, CurrencyCode.USD)] = "PZW215",
        [(PaymentMethodCode.Omari, CurrencyCode.USD)] = "PZW216",
        [(PaymentMethodCode.PayGo, CurrencyCode.ZiG)] = "PZW210",
    };

    internal static string GetPaymentMethodCode(PaymentMethodCode method, CurrencyCode currency)
    {
        if (_methodCodes.TryGetValue((method, currency), out var code))
            return code;

        throw new PesePayException($"Payment method {method} is not available for {currency}.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}");
        }
    }

    private string DecryptResponsePayload(string responseBody)
    {
        var raw = JsonSerializer.Deserialize<JsonElement>(responseBody);
        if (raw.TryGetProperty("payload", out var payloadElement))
        {
            try
            {
                return _crypto.Decrypt(payloadElement.GetString()!);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return responseBody;
            }
        }

        return responseBody;
    }

    public async Task<PesepayResult<InitiateResponse>> InitiateRedirectPaymentAsync(
        RedirectPaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_resultUrl))
            throw new PesePayException("Result URL has not been specified.");

        if (string.IsNullOrEmpty(_returnUrl))
            throw new PesePayException("Return URL has not been specified.");

        var transaction = new Transaction(
            new Amount(request.Amount, request.Currency),
            request.Reason,
            request.MerchantReference)
        {
            ResultUrl = _resultUrl,
            ReturnUrl = _returnUrl
        };

        var payload = _crypto.Encrypt(JsonSerializer.Serialize(transaction, _jsonOptions));

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { payload }, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("v1/payments/initiate", content, ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var json = DecryptResponsePayload(responseBody);
            var result = JsonSerializer.Deserialize<InitiateResponse>(json, _jsonOptions)!;

            return PesepayResult<InitiateResponse>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<PaymentResponse>> InitiateSeamlessPaymentAsync(
        SeamlessPaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_resultUrl))
            throw new PesePayException("Result URL has not been specified.");

        var methodCode = GetPaymentMethodCode(request.Method, request.Currency);
        var customer = new Customer(request.Email, request.PhoneNumber, request.CustomerName);
        var payment = new Payment(request.Currency, methodCode, customer)
        {
            ResultUrl = _resultUrl,
            ReturnUrl = _returnUrl,
            ReasonForPayment = request.Reason,
            AmountDetails = new Amount(request.Amount, request.Currency),
            MerchantReference = request.MerchantReference
        };

        var fields = new Dictionary<string, string>();

        if (request.Card != null)
        {
            fields["creditCardNumber"] = request.Card.Number;
            fields["creditCardSecurityNumber"] = request.Card.Cvv;
            fields["creditCardExpiryDate"] = request.Card.ExpiryDate;
            if (!string.IsNullOrEmpty(request.Card.HolderName))
                fields["creditCardHolder"] = request.Card.HolderName;
        }
        else if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            fields["customerPhoneNumber"] = request.PhoneNumber;
        }
        else
        {
            throw new PesePayException(
                "Seamless payment requires either a Card (for card payments) or PhoneNumber (for mobile money).");
        }

        payment.PaymentMethodRequiredFields = fields;
        payment.PaymentRequestFields = fields;

        var payload = _crypto.Encrypt(JsonSerializer.Serialize(payment, _jsonOptions));

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { payload }, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(MakePaymentPath, content, ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var json = DecryptResponsePayload(responseBody);
            var result = JsonSerializer.Deserialize<PaymentResponse>(json, _jsonOptions)!;

            return PesepayResult<PaymentResponse>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(
        string referenceNumber, CancellationToken ct = default)
    {
        var url = $"v1/payments/check-payment?referenceNumber={Uri.EscapeDataString(referenceNumber)}";
        return await PollPaymentAsync(new Uri(url, UriKind.Relative), ct);
    }

    public async Task<PesepayResult<PaymentStatus>> PollPaymentAsync(
        Uri pollUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(pollUrl, ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var json = DecryptResponsePayload(responseBody);
            var result = JsonSerializer.Deserialize<PaymentStatus>(json, _jsonOptions)!;

            return PesepayResult<PaymentStatus>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<List<CurrencyInfo>>> GetActiveCurrenciesAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("v1/currencies/active", ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<List<CurrencyInfo>>(responseBody, _jsonOptions)!;

            return PesepayResult<List<CurrencyInfo>>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }

    public async Task<PesepayResult<List<PaymentMethodInfo>>> GetPaymentMethodsAsync(
        string currencyCode, CancellationToken ct = default)
    {
        try
        {
            var url = $"v1/payment-methods/for-currency?currencyCode={Uri.EscapeDataString(currencyCode)}";
            var response = await _httpClient.GetAsync(url, ct);
            await EnsureSuccessAsync(response);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<List<PaymentMethodInfo>>(responseBody, _jsonOptions)!;

            return PesepayResult<List<PaymentMethodInfo>>.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            throw new PesePayException(ex.Message, ex);
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
dotnet build PesePay.csproj
```

Expected: Build succeeds. If ServiceCollectionExtensions.cs still references old constructors/properties, that failure is expected — we'll fix it next.

---

### Task 5: Update ServiceCollectionExtensions

**Files:**
- Modify: `ServiceCollectionExtensions.cs` (entire file)

- [ ] **Step 1: Replace ServiceCollectionExtensions.cs**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PesePay.Domain;

namespace PesePay;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPesePay(
        this IServiceCollection services, Action<PesePayConfiguration> configure)
    {
        var config = new PesePayConfiguration();
        configure(config);

        var client = new PesePayClient(
            config.IntegrationKey,
            config.EncryptionKey,
            config.Environment,
            config.ResultUrl,
            config.ReturnUrl);

        services.AddSingleton<IPesePayClient>(client);
        return services;
    }

    public static IServiceCollection AddPesePay(
        this IServiceCollection services, IConfiguration configuration)
    {
        var config = new PesePayConfiguration();
        configuration.Bind(config);

        var client = new PesePayClient(
            config.IntegrationKey,
            config.EncryptionKey,
            config.Environment,
            config.ResultUrl,
            config.ReturnUrl);

        services.AddSingleton<IPesePayClient>(client);
        return services;
    }
}
```

- [ ] **Step 2: Verify the main project builds**

```bash
dotnet build PesePay.csproj
```

Expected: Build succeeds (tests will still fail — expected).

---

### Task 6: Enable XML documentation in the NuGet package

**Files:**
- Modify: `PesePay.csproj`

- [ ] **Step 1: Add GenerateDocumentationFile to PesePay.csproj**

Add this line inside the `<PropertyGroup>`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

The `<PropertyGroup>` should now contain:

```xml
<PropertyGroup>
    <RootNamespace>PesePay</RootNamespace>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <DefaultItemExcludes>$(DefaultItemExcludes);PesePay.Tests/**;PesePay.Tests.Integration/**</DefaultItemExcludes>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <GenerateAssemblyCompanyAttribute>false</GenerateAssemblyCompanyAttribute>
    <GenerateAssemblyConfigurationAttribute>false</GenerateAssemblyConfigurationAttribute>
    <GenerateAssemblyFileVersionAttribute>false</GenerateAssemblyFileVersionAttribute>
    <GenerateAssemblyInformationalVersionAttribute>false</GenerateAssemblyInformationalVersionAttribute>
    <GenerateAssemblyProductAttribute>false</GenerateAssemblyProductAttribute>
    <GenerateAssemblyTitleAttribute>false</GenerateAssemblyTitleAttribute>
    <GenerateAssemblyVersionAttribute>false</GenerateAssemblyVersionAttribute>

    <PackageId>Stelele.PesePay</PackageId>
    <Version>1.0.0</Version>
    <Authors>Gift Mugweni</Authors>
    <Company></Company>
    <Description>A simple library for handling PesePay integrations.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/Stelele/pesepay</RepositoryUrl>
</PropertyGroup>
```

- [ ] **Step 2: Verify it builds and generates the XML doc file**

```bash
dotnet build PesePay.csproj
```

Expected: Build succeeds. Verify the XML doc file exists:
```bash
ls -la bin/Debug/net8.0/PesePay.xml
```

---

### Task 7: Update IPesePayClientTests (interface member names)

**Files:**
- Modify: `PesePay.Tests/IPesePayClientTests.cs` (entire file)

- [ ] **Step 1: Replace IPesePayClientTests.cs**

```csharp
using PesePay.Domain;

namespace PesePay.Tests;

public class IPesePayClientTests
{
    [Theory]
    [InlineData("InitiateRedirectPaymentAsync")]
    [InlineData("InitiateSeamlessPaymentAsync")]
    [InlineData("CheckPaymentStatusAsync")]
    [InlineData("PollPaymentAsync")]
    [InlineData("GetActiveCurrenciesAsync")]
    [InlineData("GetPaymentMethodsAsync")]
    public void Interface_Defines_Member(string memberName)
    {
        var members = typeof(IPesePayClient).GetMembers()
            .Select(m => m.Name)
            .Distinct();

        Assert.Contains(memberName, members);
    }
}
```

- [ ] **Step 2: Run the test**

```bash
dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "FullyQualifiedName~IPesePayClientTests"
```

Expected: All 6 tests pass.

---

### Task 8: Update PesePayClientFactoryTests (replace with payment code mapping tests)

**Files:**
- Modify: `PesePay.Tests/PesePayClientFactoryTests.cs` (entire file)

- [ ] **Step 1: Replace PesePayClientFactoryTests.cs**

Since `CreateTransaction` and `CreatePayment` are removed from the public API, replace factory tests with tests verifying the internal payment method code mapping.

```csharp
using PesePay.Domain;

namespace PesePay.Tests;

public class PesePayClientFactoryTests
{
    [Theory]
    [InlineData(PaymentMethodCode.EcoCash, CurrencyCode.USD, "PZW211")]
    [InlineData(PaymentMethodCode.EcoCash, CurrencyCode.ZiG, "PZW201")]
    [InlineData(PaymentMethodCode.InnBucks, CurrencyCode.USD, "PZW212")]
    [InlineData(PaymentMethodCode.Visa, CurrencyCode.USD, "PZW204")]
    [InlineData(PaymentMethodCode.MasterCard, CurrencyCode.USD, "PZW205")]
    [InlineData(PaymentMethodCode.Zimswitch, CurrencyCode.USD, "PZW215")]
    [InlineData(PaymentMethodCode.Omari, CurrencyCode.USD, "PZW216")]
    [InlineData(PaymentMethodCode.PayGo, CurrencyCode.ZiG, "PZW210")]
    public void GetPaymentMethodCode_Resolves_Correct_Code(
        PaymentMethodCode method, CurrencyCode currency, string expectedCode)
    {
        var code = PesePayClient.GetPaymentMethodCode(method, currency);

        Assert.Equal(expectedCode, code);
    }

    [Fact]
    public void GetPaymentMethodCode_Throws_When_Unsupported_Combination()
    {
        Assert.Throws<PesePayException>(
            () => PesePayClient.GetPaymentMethodCode(PaymentMethodCode.EcoCash, CurrencyCode.ZWL));
    }
}
```

- [ ] **Step 2: Run the tests**

```bash
dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "FullyQualifiedName~PesePayClientFactoryTests"
```

Expected: All tests pass.

---

### Task 9: Rewrite PesePayClientApiTests for new API

**Files:**
- Modify: `PesePay.Tests/PesePayClientApiTests.cs` (entire file)

- [ ] **Step 1: Replace PesePayClientApiTests.cs**

```csharp
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
```

- [ ] **Step 2: Run the tests**

```bash
dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "FullyQualifiedName~PesePayClientApiTests"
```

Expected: All 12 tests pass.

---

### Task 10: Update SandboxCredentials for new constructor

**Files:**
- Modify: `PesePay.Tests.Integration/SandboxCredentials.cs` (entire file)

- [ ] **Step 1: Replace SandboxCredentials.cs**

```csharp
using PesePay.Domain;

namespace PesePay.Tests.Integration;

public static class SandboxCredentials
{
    public static string IntegrationKey =>
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_INTEGRATION_KEY")!;

    public static string EncryptionKey =>
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_ENCRYPTION_KEY")!;

    public static string ResultUrl =>
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_RESULT_URL")!;

    public static string ReturnUrl =>
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_RETURN_URL")!;

    public static PesePayClient CreateClient()
    {
        return new PesePayClient(IntegrationKey, EncryptionKey, EnvironmentType.Sandbox);
    }

    public static PesePayClient CreateClientWithUrls()
    {
        return new PesePayClient(
            IntegrationKey, EncryptionKey, EnvironmentType.Sandbox,
            resultUrl: ResultUrl,
            returnUrl: ReturnUrl);
    }

    public static void PrintConfigurationBanner()
    {
        Console.WriteLine("=== PesePay Sandbox Integration Tests ===");
        Console.WriteLine(
            $"Integration Key: {(string.IsNullOrEmpty(IntegrationKey) ? "✗ missing" : "✓ configured")}");
        Console.WriteLine(
            $"Encryption Key:  {(string.IsNullOrEmpty(EncryptionKey) ? "✗ missing" : "✓ configured")}");
        Console.WriteLine(
            $"Result URL:     {(string.IsNullOrEmpty(ResultUrl) ? "✗ missing" : "✓ configured")}");
        Console.WriteLine(
            $"Return URL:     {(string.IsNullOrEmpty(ReturnUrl) ? "✗ missing" : "✓ configured")}");
        Console.WriteLine("Sandbox API:    https://api.test.sandbox.pesepay.com/payments-engine");
        Console.WriteLine("=========================================");
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
dotnet build PesePay.Tests.Integration/PesePay.Tests.Integration.csproj
```

---

### Task 11: Update integration tests

**Files:**
- Modify: `PesePay.Tests.Integration/PesePaySandboxTests.cs` (entire file)

- [ ] **Step 1: Replace PesePaySandboxTests.cs**

```csharp
using PesePay.Domain;

namespace PesePay.Tests.Integration;

public class SandboxDiscoveryTests
{
    public SandboxDiscoveryTests()
    {
        SandboxCredentials.PrintConfigurationBanner();
    }

    [SandboxFact]
    public async Task GetActiveCurrencies_Returns_Currencies()
    {
        var client = SandboxCredentials.CreateClient();
        var result = await client.GetActiveCurrenciesAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Active currencies ({result.Data!.Count}):");
        foreach (var currency in result.Data!)
            Console.WriteLine($"  {currency.Code} ({currency.Name}) - Active: {currency.IsActive}");
    }

    [SandboxFact]
    public async Task GetPaymentMethods_USD_Returns_Card_Methods()
    {
        var client = SandboxCredentials.CreateClient();
        var result = await client.GetPaymentMethodsAsync("USD");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Payment methods for USD ({result.Data!.Count}):");
        foreach (var method in result.Data!)
            Console.WriteLine(
                $"  {method.Code} - {method.Name} (redirect: {method.RedirectRequired}, " +
                $"fields: {method.RequiredFields?.Count ?? 0})");
    }

    [SandboxFact]
    public async Task GetPaymentMethods_ZiG_Returns_Mobile_Money_Methods()
    {
        var client = SandboxCredentials.CreateClient();
        var result = await client.GetPaymentMethodsAsync("ZiG");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Payment methods for ZiG ({result.Data!.Count}):");
        foreach (var method in result.Data!)
            Console.WriteLine(
                $"  {method.Code} - {method.Name} (redirect: {method.RedirectRequired}, " +
                $"fields: {method.RequiredFields?.Count ?? 0})");
    }
}

public class SandboxPaymentTests
{
    private static PesePayClient CreateClient()
    {
        return SandboxCredentials.CreateClientWithUrls();
    }

    [SandboxWebhookFact]
    public async Task InitiateRedirectPayment_Returns_RedirectUrl_And_PollUrl()
    {
        var client = CreateClient();
        var request = new RedirectPaymentRequest(
            5m, CurrencyCode.USD, "Redirect payment test",
            "RDR-" + Guid.NewGuid().ToString("N")[..8]);

        var result = await client.InitiateRedirectPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ReferenceNumber);

        Console.WriteLine($"Reference:     {result.Data.ReferenceNumber}");
        Console.WriteLine($"PollUrl:       {result.Data.PollUrl}");
        Console.WriteLine($"RedirectUrl:   {result.Data.RedirectUrl}");
        Console.WriteLine($"InternalRef:   {result.Data.InternalReference}");
        Console.WriteLine($"Status:        {result.Data.TransactionStatus}");
        Console.WriteLine($"StatusCode:    {result.Data.TransactionStatusCode}");
        Console.WriteLine($"Description:   {result.Data.TransactionStatusDescription}");
    }

    [SandboxFact]
    public async Task InitiateRedirectPayment_Throws_When_ResultUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        var request = new RedirectPaymentRequest(10m, CurrencyCode.USD, "Test");

        await Assert.ThrowsAsync<PesePayException>(
            () => client.InitiateRedirectPaymentAsync(request));
    }

    [SandboxWebhookFact]
    public async Task CheckPaymentStatus_Returns_Valid_Status()
    {
        var client = CreateClient();
        var request = new RedirectPaymentRequest(
            5m, CurrencyCode.USD, "Status check test",
            "CHK-" + Guid.NewGuid().ToString("N")[..8]);
        var initResult = await client.InitiateRedirectPaymentAsync(request);
        Assert.True(initResult.IsSuccess);

        var result = await client.CheckPaymentStatusAsync(initResult.Data!.ReferenceNumber);

        Assert.True(result.IsSuccess);

        if (result.Data?.ReferenceNumber == null)
        {
            Console.WriteLine(
                "CheckPaymentStatus succeeded but fields are null - poll decryption mismatch");
            return;
        }

        Assert.NotEmpty(result.Data.ReferenceNumber);
        Assert.NotNull(result.Data.TransactionStatus);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
        Console.WriteLine($"IsPaid:       {result.Data.IsPaid}");
    }

    [SandboxWebhookFact]
    public async Task PollPayment_Returns_Valid_Status()
    {
        var client = CreateClient();
        var request = new RedirectPaymentRequest(
            5m, CurrencyCode.USD, "Poll test",
            "POL-" + Guid.NewGuid().ToString("N")[..8]);
        var initResult = await client.InitiateRedirectPaymentAsync(request);
        Assert.True(initResult.IsSuccess);

        var result = await client.PollPaymentAsync(initResult.Data!.PollUrl);

        Assert.True(result.IsSuccess);

        if (result.Data?.TransactionStatus == null)
        {
            Console.WriteLine(
                "PollPaymentAsync succeeded but fields are null - poll decryption mismatch");
            return;
        }

        Assert.NotNull(result.Data.TransactionStatus);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"IsPaid:       {result.Data.IsPaid}");
    }
}

public class SandboxSeamlessPaymentTests : IAsyncLifetime
{
    private PesePayClient _client = null!;
    private List<PaymentMethodInfo> _usdMethods = new();
    private List<PaymentMethodInfo> _zigMethods = new();

    public async Task InitializeAsync()
    {
        _client = SandboxCredentials.CreateClientWithUrls();

        var usdResult = await _client.GetPaymentMethodsAsync("USD");
        if (usdResult.IsSuccess) _usdMethods = usdResult.Data!;

        var zigResult = await _client.GetPaymentMethodsAsync("ZiG");
        if (zigResult.IsSuccess) _zigMethods = zigResult.Data!;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private PaymentMethodInfo? FindMethod(string nameKeyword)
    {
        return _usdMethods?.FirstOrDefault(m =>
            m.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase) ||
            m.Description.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase) ||
            m.Code.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase))
            ??
            _zigMethods?.FirstOrDefault(m =>
                m.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase) ||
                m.Code.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase));
    }

    private CurrencyCode GetCurrencyForMethod(PaymentMethodInfo method)
    {
        if (_zigMethods?.Contains(method) == true) return CurrencyCode.ZiG;
        return CurrencyCode.USD;
    }

    [SandboxWebhookFact]
    public async Task InitiateSeamlessPayment_EcoCash_Success()
    {
        var ecoCashMethod = FindMethod("EcoCash");
        if (ecoCashMethod == null) return;
        var currency = GetCurrencyForMethod(ecoCashMethod);

        Console.WriteLine($"Using method {ecoCashMethod.Code} with currency {currency}");

        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, currency, 10m,
            "EcoCash success test",
            "ECO-SUCCESS-" + Guid.NewGuid().ToString("N")[..8],
            Email: "test@example.com",
            CustomerName: "Test User",
            PhoneNumber: "0777777777");

        var result = await _client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);

        if (result.Data?.ReferenceNumber == null)
        {
            Console.WriteLine(
                "EcoCash response succeeded but reference is null - deserialization mismatch");
            return;
        }

        Assert.NotEmpty(result.Data.ReferenceNumber);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");
        Console.WriteLine($"IsPaid:     {result.Data.IsPaid}");

        Assert.Equal("SUCCESS", result.Data.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task InitiateSeamlessPayment_EcoCash_Failure()
    {
        var ecoCashMethod = FindMethod("EcoCash");
        if (ecoCashMethod == null) return;
        var currency = GetCurrencyForMethod(ecoCashMethod);

        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, currency, 10m,
            "EcoCash failure test",
            "ECO-FAIL-" + Guid.NewGuid().ToString("N")[..8],
            Email: "test@example.com",
            CustomerName: "Test User",
            PhoneNumber: "0770000000");

        var result = await _client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.IsPaid);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
    }

    [SandboxWebhookFact]
    public async Task InitiateSeamlessPayment_VISA_Success()
    {
        var visaMethod = FindMethod("Visa");
        if (visaMethod == null) return;

        var card = new CardDetails("4867960000005461", "608", "12/30", "Test User");
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.Visa, CurrencyCode.USD, 10m,
            "VISA test",
            "VISA-" + Guid.NewGuid().ToString("N")[..8],
            Email: "test@example.com",
            CustomerName: "Test User",
            Card: card);

        var result = await _client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");

        Assert.Equal("SUCCESS", result.Data!.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task InitiateSeamlessPayment_VISA_Failure()
    {
        var visaMethod = FindMethod("Visa");
        if (visaMethod == null) return;

        var card = new CardDetails("4867965005005002", "994", "12/30", "Test User");
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.Visa, CurrencyCode.USD, 10m,
            "VISA failure test",
            "VISA-FAIL-" + Guid.NewGuid().ToString("N")[..8],
            Email: "test@example.com",
            CustomerName: "Test User",
            Card: card);

        var result = await _client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.IsPaid);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
    }

    [SandboxFact]
    public async Task InitiateSeamlessPayment_Throws_When_ResultUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, CurrencyCode.USD, 10m,
            "test", "MERCH01",
            PhoneNumber: "0777777777");

        await Assert.ThrowsAsync<PesePayException>(
            () => client.InitiateSeamlessPaymentAsync(request));
    }
}
```

- [ ] **Step 2: Verify integration tests build**

```bash
dotnet build PesePay.Tests.Integration/PesePay.Tests.Integration.csproj
```

Expected: Build succeeds.

---

### Task 12: Run full test suite

- [ ] **Step 1: Run all unit tests**

```bash
dotnet test PesePay.Tests/PesePay.Tests.csproj
```

Expected: All unit tests pass.

- [ ] **Step 2: Run integration tests (requires sandbox credentials)**

```bash
dotnet test PesePay.Tests.Integration/PesePay.Tests.Integration.csproj
```

Expected: Integration tests pass (sandbox-credential-dependent).

---

### Task 13: Verify NuGet packaging

- [ ] **Step 1: Pack the NuGet package**

```bash
dotnet pack PesePay.csproj -c Release
```

- [ ] **Step 2: Verify XML docs are included**

```bash
unzip -l bin/Release/*.nupkg | grep PesePay.xml
```

Expected: Output shows `PesePay.xml` in the package.
