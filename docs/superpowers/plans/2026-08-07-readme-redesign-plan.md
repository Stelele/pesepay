# README Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite README.md with accurate API documentation and fix stale XML doc cref references.

**Architecture:** Single-file README rewrite with 14 sections following Stripe/Square/Polly-style progressive disclosure pattern. Three supporting XML doc comment fixes in domain types. No code changes to library logic.

**Tech Stack:** Markdown, C# XML doc comments

**Spec:** `docs/superpowers/specs/2026-08-07-readme-redesign-design.md`

---

### Task 1: Fix stale XML doc cref references

**Files:**
- Modify: `PesePay/Domain/PaymentResponse.cs:4`
- Modify: `PesePay/Domain/InitiateResponse.cs:4`
- Modify: `PesePay/Domain/PaymentStatus.cs:4`

Each file has a `<see cref="..."/>` reference pointing to a method name that no longer exists.

- [ ] **Step 1: Fix PaymentResponse.cs line 4**

Change from:
```csharp
/// Response from <see cref="IPesePayClient.MakeSeamlessPaymentAsync"/>.
```
to:
```csharp
/// Response from <see cref="IPesePayClient.InitiateSeamlessPaymentAsync"/>.
```

- [ ] **Step 2: Fix InitiateResponse.cs line 4**

Change from:
```csharp
/// Response from <see cref="IPesePayClient.InitiateTransactionAsync"/>.
```
to:
```csharp
/// Response from <see cref="IPesePayClient.InitiateRedirectPaymentAsync"/>.
```

- [ ] **Step 3: Fix PaymentStatus.cs line 4**

Change from:
```csharp
/// Payment status returned from <see cref="IPesePayClient.CheckPaymentStatusAsync"/> and <see cref="IPesePayClient.PollTransactionAsync"/>.
```
to:
```csharp
/// Payment status returned from <see cref="IPesePayClient.CheckPaymentStatusAsync"/> and <see cref="IPesePayClient.PollPaymentAsync"/>.
```

- [ ] **Step 4: Verify build**

```bash
dotnet build
```
Expected: Build succeeds with no cref-related warnings.

- [ ] **Step 5: Commit**

```bash
git add PesePay/Domain/PaymentResponse.cs PesePay/Domain/InitiateResponse.cs PesePay/Domain/PaymentStatus.cs
git commit -m "docs: fix stale XML doc cref references"
```

---

### Task 2: Write the complete new README

**Files:**
- Write: `README.md` (replace entire file)

- [ ] **Step 1: Write `README.md`**

Write the following complete content to `README.md`:

<!-- BEGIN_README -->
```markdown
[![NuGet](https://img.shields.io/nuget/v/Stelele.PesePay?label=NuGet)](https://www.nuget.org/packages/Stelele.PesePay)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/Stelele/pesepay/blob/main/LICENSE)
[![Build](https://github.com/Stelele/pesepay/actions/workflows/ci.yml/badge.svg)](https://github.com/Stelele/pesepay/actions/workflows/ci.yml)

# PesePay for .NET

A .NET library for integrating with the PesePay payment gateway in Zimbabwe.

- **Zero third-party runtime dependencies** — only .NET BCL
- **Typed, enum-safe API** — compile-time safety for payment methods and currencies
- **All payment methods** — EcoCash, InnBucks, Visa, MasterCard, Zimswitch, Omari, PayGo across USD and ZiG

---

## Installation

```shell
dotnet add package Stelele.PesePay
```

Requires .NET 8.0 SDK or later.

---

## Client Setup

### Without Dependency Injection

Create a single `PesePayClient` instance and reuse it — it is thread-safe and manages its own `HttpClient`.

```csharp
using PesePay;
using PesePay.Domain;

var client = new PesePayClient(
    integrationKey: "YOUR_INTEGRATION_KEY",
    encryptionKey: "YOUR_ENCRYPTION_KEY",
    environment: EnvironmentType.Sandbox,
    resultUrl: "https://example.com/pesepay/callback",
    returnUrl: "https://example.com/payment/return");
```

> **Note:** `resultUrl` must be a public HTTPS endpoint reachable by PesePay's servers. For local development, use ngrok or dev tunnels. `returnUrl` is required for redirect payments; `resultUrl` is required for both redirect and seamless.

### With ASP.NET Core Dependency Injection

```csharp
// Program.cs — delegate-based
builder.Services.AddPesePay(options =>
{
    options.IntegrationKey = "YOUR_INTEGRATION_KEY";
    options.EncryptionKey = "YOUR_ENCRYPTION_KEY";
    options.Environment = EnvironmentType.Sandbox;
    options.ResultUrl = "https://example.com/pesepay/callback";
    options.ReturnUrl = "https://example.com/payment/return";
});

// Or bind from appsettings.json
builder.Services.AddPesePay(builder.Configuration.GetSection("PesePay"));
```

`appsettings.json`:
```json
{
  "PesePay": {
    "IntegrationKey": "YOUR_INTEGRATION_KEY",
    "EncryptionKey": "YOUR_ENCRYPTION_KEY",
    "Environment": "Sandbox",
    "ResultUrl": "https://example.com/pesepay/callback",
    "ReturnUrl": "https://example.com/payment/return"
  }
}
```

Then inject `IPesePayClient` into your controllers:

```csharp
public class PaymentController : ControllerBase
{
    private readonly IPesePayClient _pesepay;

    public PaymentController(IPesePayClient pesepay) => _pesepay = pesepay;
}
```

---

## Which Payment Flow Should I Use?

| Flow | Description | Use When | URLs Required |
|------|------------|----------|---------------|
| **Redirect** | PesePay hosts the checkout page. Customer is redirected to complete payment. | Card payments, or when you want minimal PCI scope. | `resultUrl` + `returnUrl` |
| **Seamless** | Server-to-server. You build the checkout UI. Customer receives a USSD push (mobile money) or enters card details on your page. | Mobile money (EcoCash, etc.), or when you want full UX control. | `resultUrl` |

**For production, prefer webhooks over polling** for payment status updates.

---

## Webhooks (Receiving Callbacks)

PesePay sends a POST request to your `resultUrl` when a payment completes. This is how you get notified of payment success without polling.

```csharp
[ApiController]
[Route("api/[controller]")]
public class PesePayController : ControllerBase
{
    [HttpPost("callback")]
    public async Task<IActionResult> Callback()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        // Request body is a JSON envelope: {"payload": "<base64-encrypted>"}
        var envelope = JsonSerializer.Deserialize<JsonElement>(body);
        var encryptedPayload = envelope.GetProperty("payload").GetString()!;

        // Decrypt with your encryption key
        var crypto = new AesCbcPayloadCrypto("YOUR_ENCRYPTION_KEY");
        var json = crypto.Decrypt(encryptedPayload);
        var status = JsonSerializer.Deserialize<PaymentStatus>(json)!;

        if (status.IsPaid)
        {
            // Fulfill the order
        }

        // Return 200 OK to acknowledge (otherwise PesePay retries)
        return Ok();
    }
}
```

> **Important:**
> - PesePay may send the same callback multiple times — always check `ReferenceNumber` and skip if already processed.
> - A successfully decrypted payload was encrypted with your shared secret key (AES-CBC, key length determines cipher strength). This provides confidentiality. Verify the payload structure and reference number after decryption.
> - **Keep your encryption key secret.** If compromised, rotate it in the PesePay dashboard and update your configuration.
> - For local development, use ngrok or dev tunnels to expose a public HTTPS endpoint.
> - If you cannot expose a public endpoint, use polling (see [Checking Payment Status](#checking-payment-status)) instead.

---

## Redirect Payment

Send the customer to PesePay's hosted checkout page.

```csharp
var client = new PesePayClient(
    "YOUR_INTEGRATION_KEY",
    "YOUR_ENCRYPTION_KEY",
    EnvironmentType.Sandbox,
    resultUrl: "https://example.com/pesepay/callback",
    returnUrl: "https://example.com/payment/return");

var result = await client.InitiateRedirectPaymentAsync(new RedirectPaymentRequest(
    Amount: 100m,
    Currency: CurrencyCode.USD,
    Reason: "Payment for order #789",
    MerchantReference: "ORDER-789"));  // optional — use unique values for idempotency

var data = result.Data!;
// RedirectUrl is non-nullable — safe to dereference
return Redirect(data.RedirectUrl.ToString());
```

---

## Seamless Payment

Server-to-server for mobile money or card payments. Set either `PhoneNumber` **or** `Card` — never both.

### Mobile Money (EcoCash, InnBucks, Omari, PayGo)

```csharp
var client = new PesePayClient(
    "YOUR_INTEGRATION_KEY",
    "YOUR_ENCRYPTION_KEY",
    EnvironmentType.Sandbox,
    resultUrl: "https://example.com/pesepay/callback");

var result = await client.InitiateSeamlessPaymentAsync(new SeamlessPaymentRequest(
    Method: PaymentMethodCode.EcoCash,
    Currency: CurrencyCode.ZiG,
    Amount: 500m,
    Reason: "Invoice #456",
    MerchantReference: "ORDER-456",
    Email: "customer@example.com",
    CustomerName: "John Doe",
    PhoneNumber: "0771234567"));  // Zimbabwe mobile format: "077..." or "+263..."

if (result.Data is { IsPaid: true })
{
    // Payment successful
}
```

### Card (Visa, MasterCard, Zimswitch)

```csharp
var client = new PesePayClient(
    "YOUR_INTEGRATION_KEY",
    "YOUR_ENCRYPTION_KEY",
    EnvironmentType.Sandbox,
    resultUrl: "https://example.com/pesepay/callback");

var result = await client.InitiateSeamlessPaymentAsync(new SeamlessPaymentRequest(
    Method: PaymentMethodCode.Visa,
    Currency: CurrencyCode.USD,
    Amount: 10m,
    Reason: "Card payment",
    MerchantReference: "ORDER-789",
    Email: "customer@example.com",
    CustomerName: "John Doe",
    Card: new CardDetails(
        Number: "4867960000005461",     // digits only
        Cvv: "608",                      // 3-4 digits
        ExpiryDate: "12/30",            // format: MM/YY
        HolderName: "John Doe")));       // optional

if (result.Data is { IsPaid: true })
{
    // Payment successful
}
```

> **Warning:** If both `Card` and `PhoneNumber` are set, `Card` takes priority silently — always set only one.
>
> `PaymentResponse.RedirectUrl` is nullable (null on seamless mobile money). Check before dereferencing.
>
> **PCI note:** Never log card details.

---

## Payment Method Codes

| Enum Value | USD | ZiG |
|-----------|-----|-----|
| `PaymentMethodCode.EcoCash` | PZW211 | PZW201 |
| `PaymentMethodCode.InnBucks` | PZW212 | — |
| `PaymentMethodCode.Visa` | PZW204 | — |
| `PaymentMethodCode.MasterCard` | PZW205 | — |
| `PaymentMethodCode.Zimswitch` | PZW215 | — |
| `PaymentMethodCode.Omari` | PZW216 | — |
| `PaymentMethodCode.PayGo` | — | PZW210 |

> Call `GetPaymentMethodsAsync` for the full current list. This table covers the most common methods. ZWL is deprecated — use **ZiG** (Zimbabwe Gold) instead.

---

## Dynamic Discovery

### Get Active Currencies

```csharp
var result = await client.GetActiveCurrenciesAsync();
foreach (var currency in result.Data!)
{
    Console.WriteLine($"{currency.Code}: {currency.Name} (active: {currency.IsActive})");
}
```

### Get Payment Methods by Currency

```csharp
var result = await client.GetPaymentMethodsAsync("ZiG");
foreach (var method in result.Data!)
{
    Console.WriteLine($"{method.Code}: {method.Name}");
    Console.WriteLine($"  Min: {method.MinimumAmount}, Max: {method.MaximumAmount}");
    foreach (var field in method.RequiredFields)
        Console.WriteLine($"  Required: {field.DisplayName} ({field.FieldType})");
}
```

---

## Checking Payment Status

### By Reference Number

```csharp
var result = await client.CheckPaymentStatusAsync("REF123");

if (result.Data is { IsPaid: true })
{
    // Payment successful
}
```

### By Poll URL

```csharp
// Store pollUrl from the payment response, convert back to Uri when reading
var result = await client.PollPaymentAsync(new Uri(storedPollUrl));
```

> **Polling guidance:** Poll every 10 seconds. If still pending after 30 minutes, treat as failed and investigate. Check PesePay docs for method-specific timeouts. For production, prefer webhooks over polling.
>
> **Note:** `PesepayResult<T>.IsSuccess` is always `true` when no exception is thrown. The `IsSuccess` property and `Fail()` factory exist but are not used in the current implementation. Checking `IsSuccess` is harmless but unnecessary — a result without a thrown exception means success.

---

## Environment Switching

Change `EnvironmentType.Sandbox` to `EnvironmentType.Production` and swap to your production keys. Never commit production keys to source control.

```csharp
// Development (from appsettings.Development.json)
builder.Services.AddPesePay(builder.Configuration.GetSection("PesePay"));

// Production — keys come from environment variables or a secrets manager
builder.Services.AddPesePay(options =>
{
    options.IntegrationKey = Environment.GetEnvironmentVariable("PESEPAY_INTEGRATION_KEY")!;
    options.EncryptionKey = Environment.GetEnvironmentVariable("PESEPAY_ENCRYPTION_KEY")!;
    options.Environment = EnvironmentType.Production;
    options.ResultUrl = "https://yourdomain.com/pesepay/callback";
    options.ReturnUrl = "https://yourdomain.com/payment/return";
});
```

---

## Cancellation & Timeout

All async methods accept a `CancellationToken`. The `HttpClient` has a 30-second hard timeout. Pass a shorter token to cancel before the HTTP timeout fires.

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

try
{
    var result = await client.InitiateRedirectPaymentAsync(
        new RedirectPaymentRequest(100m, CurrencyCode.USD, "Order", "REF"),
        cts.Token);
}
catch (OperationCanceledException)
{
    // Request timed out
}
```

---

## Error Handling

All failure paths throw `PesePayException` (wraps `HttpRequestException` for transport errors, thrown directly for configuration errors). Catch `PesePayException` for all failure cases.

```csharp
try
{
    var result = await client.InitiateRedirectPaymentAsync(
        new RedirectPaymentRequest(100m, CurrencyCode.USD, "Order #123", "REF001"));

    // No exception = success — use result.Data directly
    var redirectUrl = result.Data!.RedirectUrl;
}
catch (PesePayException ex)
{
    Console.WriteLine($"Payment error: {ex.Message}");
}
```

> **Known limitations:** Malformed JSON responses may throw `JsonException`. Wrong encryption keys surface as deserialization errors. For production robustness, consider adding a Polly retry policy — the library has no built-in retries.

---

## Full API Reference

Full API reference coming soon. In the meantime, see the public interface at [IPesePayClient.cs](https://github.com/Stelele/pesepay/blob/main/PesePay/IPesePayClient.cs).
```
<!-- END_README -->

- [ ] **Step 2: Verify NuGet.org rendering**

Check all links are absolute URLs and the file uses only standard markdown (no HTML extras, no relative links).

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: rewrite README with accurate API documentation

- Replace all outdated method names with current API
- Add hero code, client setup, flow decision guide
- Document webhooks with payload decryption, redirect/seamless payments
- Add payment method codes table, dynamic discovery, status checking
- Cover environment switching, cancellation, error handling
- Remove non-existent low-level API section"
```

---

### Task 3: Build, pack, and verify

**Files:**
- Verify: `README.md` is in build output

- [ ] **Step 1: Build**

```bash
dotnet build -c Release
```
Expected: Build succeeds. README is copied to output via `PackageReadmeFile`.

- [ ] **Step 2: Verify word count**

```bash
wc -w README.md
```
Expected: ~1,100-1,500 words.

- [ ] **Step 3: Commit** (if any adjustments needed)

```bash
git commit --allow-empty -m "docs: final word count and build verification pass"
```

---

### Task 4: Final review against spec success criteria

**Files:**
- Read: `README.md`
- Read: `docs/superpowers/specs/2026-08-07-readme-redesign-design.md`

- [ ] **Step 1: Verify each success criterion**

Open `docs/superpowers/specs/2026-08-07-readme-redesign-design.md` Success Criteria section. Verify all 28 checkboxes:

1. Code examples use actual API → search for `InitiateRedirectPaymentAsync`, `InitiateSeamlessPaymentAsync`, `CheckPaymentStatusAsync`, `PollPaymentAsync`, `GetActiveCurrenciesAsync`, `GetPaymentMethodsAsync`, `RedirectPaymentRequest`, `SeamlessPaymentRequest`, `CardDetails`
2. No old method names → search for `InitiateTransactionAsync`, `MakeSeamlessPaymentAsync`, `PollTransactionAsync`, `CreateTransaction`, `CreatePayment` — must match 0
3. No `client.ResultUrl = "..."` → search, must match 0
4. Redirect examples construct client with resultUrl + returnUrl
5. CardDetails inside SeamlessPaymentRequest.Card
6. Card-vs-PhoneNumber exclusivity noted
7. Card field formats documented
8. Phone format documented (Zimbabwe)
9. Webhook decryption with envelope unwrap, HTTP 200, duplicate detection, trust model, key secrecy, ngrok
10. Webhook polling fallback
11. Error handling: exception-only, IsSuccess always true, known limitations
12. InitiateResponse.RedirectUrl (non-nullable) vs PaymentResponse.RedirectUrl (nullable)
13. resultUrl nullable vs runtime exception contradiction
14. HttpClient thread-safety noted
15. PCI note present
16. appsettings.json with PascalCase keys
17. EnvironmentType string binding note
18. Polling defaults (10s/30min)
19. PollPaymentAsync shows `new Uri(string)`
20. Idempotency noted
21. ".NET 8.0 SDK or later"
22. Retry note (Polly)
23. Payment method table with disclaimer, ZWL deprecation, empty cells
24. All links absolute
25. No stale cref refs
26. Feature bullets under tagline
27. Client setup merged DI + non-DI
28. ~1,100-1,500 words, ~25% code density

If any item fails, fix the README and repeat this step.

- [ ] **Step 2: Commit final fixes**

```bash
git add README.md
git commit -m "docs: final review fixes against spec criteria"
```
