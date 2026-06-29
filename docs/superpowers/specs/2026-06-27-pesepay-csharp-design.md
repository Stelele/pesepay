# PesePay C# Library — Design Specification

**Date:** 2026-06-27
**Status:** Draft
**Source:** Port of [pesepay](https://www.npmjs.com/package/pesepay) TypeScript library (v1.0.4) to idiomatic C#.

## 1. Summary

A .NET library for integrating with the PesePay payment gateway. Supports redirect payments, seamless payments, and payment status checking. Payloads are AES-256-CBC encrypted/decrypted using the merchant's encryption key.

## 2. Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Scope | Idiomatic C# redesign | Beyond 1:1 port; leverage C# patterns |
| Target frameworks | net8.0, net9.0, net10.0 | Broad reach, LTS + current |
| HTTP | Internal static HttpClient | Simple, no external DI required |
| Error handling | Hybrid: Result objects + exceptions | API errors → Result; transport → Exception |
| Model types | Immutable records | Value semantics, with-expressions, thread-safe |
| Encapsulation | Single C# project with namespace folders | Simple structure, no multi-project overhead |
| JSON | System.Text.Json | Zero external dependencies |
| Encryption | System.Security.Cryptography.Aes (BCL) | Zero external dependencies |

## 3. Architecture

Three namespaces within a single project (`PesePay.csproj`):

```
PesePay/                          (root namespace)
├── Domain/                       (PesePay.Domain)
│   └── Immutable record types, enums, result types
├── Crypto/                       (PesePay.Crypto)
│   └── IPayloadCrypto + AesCbcPayloadCrypto
├── IPesePayClient.cs
├── PesePayClient.cs
├── PesePayConfiguration.cs
└── ServiceCollectionExtensions.cs
```

**Dependency flow:** Domain ← none | Crypto ← none | Core ← Domain + Crypto

## 4. Domain Models

### 4.1 Enums

```csharp
public enum CurrencyCode
{
    [EnumMember(Value = "USD")] Usd,
    [EnumMember(Value = "ZWL")] Zwl
}

public enum TransactionType
{
    [EnumMember(Value = "BASIC")] Basic
}

public enum EnvironmentType
{
    Sandbox,
    Production
}
```

### 4.2 Records

```csharp
public readonly record struct Amount(decimal Value, CurrencyCode Currency);

public record Customer(string? Email, string? PhoneNumber, string? Name)
{
    // Validation: Email or PhoneNumber must be non-null
}

public record Transaction(
    Amount AmountDetails,
    CurrencyCode CurrencyCode,
    string ReasonForPayment,
    string? MerchantReference = null,
    TransactionType Type = TransactionType.Basic)
{
    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }
}

public record Payment(
    CurrencyCode CurrencyCode,
    string PaymentMethodCode,
    Customer Customer)
{
    public string? ReasonForPayment { get; set; }
    public Amount? AmountDetails { get; set; }
    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }
    public Dictionary<string, string>? RequiredFields { get; set; }
}
```

Note: `ResultUrl` and `ReturnUrl` are settable properties on `Transaction` and `Payment` (not constructor params) because they are set by `PesePayClient` before sending.

### 4.3 Result Types

```csharp
public class PesepayResult<T>
    where T : notnull
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    // Success factory
    public static PesepayResult<T> Ok(T data);

    // Failure factory
    public static PesepayResult<T> Fail(string errorMessage);
}
```

API-level error responses (e.g., invalid payment) return `PesepayResult<T>.Fail()`. Network/transport errors throw `PesepayException`.

## 5. API Surface

### 5.1 IPesePayClient

```csharp
public interface IPesePayClient
{
    // Configuration
    string? ResultUrl { get; set; }
    string? ReturnUrl { get; set; }

    // Factory methods (pure object creation, no HTTP)
    Transaction CreateTransaction(decimal amount, CurrencyCode currency, string reason, string? merchantRef = null);
    Payment CreatePayment(CurrencyCode currency, string methodCode, string? email, string? phone, string? name);

    // API methods (async HTTP calls)
    Task<PesepayResult<InitiateResponse>> InitiateTransactionAsync(Transaction transaction, CancellationToken ct = default);
    Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(Payment payment, string reason, decimal amount, Dictionary<string, string>? fields = null, CancellationToken ct = default);
    Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(string referenceNumber, CancellationToken ct = default);
    Task<PesepayResult<PaymentStatus>> PollTransactionAsync(Uri pollUrl, CancellationToken ct = default);
}
```

### 5.2 Response Models

```csharp
public record InitiateResponse(string ReferenceNumber, Uri PollUrl, Uri RedirectUrl);
public record PaymentResponse(string ReferenceNumber, Uri PollUrl, Uri? RedirectUrl, bool IsPaid);
public record PaymentStatus(string ReferenceNumber, Uri PollUrl, Uri? RedirectUrl, bool IsPaid);
```

## 6. Encryption

- **Algorithm:** AES-256-CBC (matching the TS library)
- **Key:** Encryption key bytes (UTF-8 encoded)
- **IV:** First 16 bytes of the encryption key (UTF-8 encoded)
- **Input:** JSON string → encrypt → Base64 string
- **Output:** Base64 string → decrypt → JSON string
- **Implementation:** `System.Security.Cryptography.Aes` (BCL, zero deps)

```csharp
public interface IPayloadCrypto
{
    string Encrypt(string jsonPayload);
    string Decrypt(string base64Payload);
}
```

## 7. HTTP & Configuration

### 7.1 Base URLs

| Environment | URL |
|-------------|-----|
| Sandbox | `https://api.test.sandbox.pesepay.com/payments-engine` |
| Production | `https://api.pesepay.com/api/payments-engine` |

### 7.2 API Endpoints

- `POST {base}/v1/payments/initiate` — Initiate redirect transaction
- `POST {base}/v2/payments/make-payment` — Make seamless payment
- `GET {base}/v1/payments/check-payment?referenceNumber={ref}` — Check payment status

### 7.3 Internal HttpClient

- Single static `HttpClient` instance per `EnvironmentType` (avoid socket exhaustion)
- `BaseAddress` set from environment URL
- Default headers: `key` = IntegrationKey, `Content-Type: application/json`
- Timeout: 30 seconds

### 7.4 Configuration

```csharp
public class PesePayConfiguration
{
    public string IntegrationKey { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public EnvironmentType Environment { get; set; } = EnvironmentType.Sandbox;
    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }
}
```

### 7.5 DI Registration

```csharp
// Delegate-based
services.AddPesePay(options => {
    options.IntegrationKey = "key";
    options.EncryptionKey = "enc-key";
    options.Environment = EnvironmentType.Sandbox;
});

// IConfiguration-based
services.AddPesePay(configuration.GetSection("PesePay"));
```

Registers `IPesePayClient` as singleton (due to static HttpClient).

## 8. JSON Serialization

- `System.Text.Json` with `JsonSerializerOptions`
- `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower` for API compatibility
- `EnumMember` attributes on enums for value mapping (USD, ZWL, BASIC)
- `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`

## 9. Error Handling Strategy

| Error Type | Mechanism | Example |
|-----------|-----------|---------|
| API business error | `PesepayResult<T>.Fail()` | Invalid payment, bad currency |
| Network/HTTP error | `PesepayException` | Timeout, DNS failure, 5xx |
| Validation error | `PesepayException` | Missing email and phone on Customer |
| Configuration error | `PesepayException` | Missing ResultUrl when initiating |

## 10. Dependencies

**Zero external NuGet dependencies.** All functionality uses .NET BCL:
- `System.Net.Http` — HTTP calls
- `System.Security.Cryptography` — AES encryption
- `System.Text.Json` — JSON serialization
- `Microsoft.Extensions.DependencyInjection` — DI registration (optional, reference-only)
- `Microsoft.Extensions.Configuration` — IConfiguration binding (optional, reference-only)

## 11. Testing Strategy

- **Unit tests (xunit):** Domain model validation, encryption round-trip, result pattern, configuration binding
- **Integration tests:** Mock `HttpMessageHandler` to simulate API responses without network
- **Contract tests:** Verify JSON serialization matches expected API format (snake_case, enum values)

## 12. Counterparts to TS API

| TypeScript | C# |
|------------|-----|
| `new Pesepay(key, encKey)` | `new PesePayClient(key, encKey)` or `AddPesePay()` |
| `pesepay.resultUrl = "..."` | `client.ResultUrl = "..."` |
| `pesepay.createPayment(...)` | `client.CreatePayment(...)` |
| `pesepay.makeSeamlessPayment(...)` | `client.MakeSeamlessPaymentAsync(...)` |
| `pesepay.createTransaction(...)` | `client.CreateTransaction(...)` |
| `pesepay.initiateTransaction(...)` | `client.InitiateTransactionAsync(...)` |
| `pesepay.checkPayment(...)` | `client.CheckPaymentStatusAsync(...)` |
| `pesepay.pollTransaction(...)` | `client.PollTransactionAsync(...)` |
| `PesepayResponse` | `PesepayResult<T>` |
