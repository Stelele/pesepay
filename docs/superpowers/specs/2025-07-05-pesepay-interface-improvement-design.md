# PesePay Interface DevEx Improvement Design

**Date:** 2025-07-05  
**Status:** Approved

## Problem Statement

The `IPesePayClient` interface has three DevEx issues:

1. **Too many function arguments** — `InitiateTransactionAsync` has 7 params, `MakeSeamlessPaymentAsync` has 9 params, making the API noisy and hard to use with IntelliSense.
2. **Redundant URL parameters** — `InitiateTransactionAsync` takes `resultUrl`/`returnUrl` as method parameters, but every client already has those as properties on the interface.
3. **XML doc comments not in NuGet** — The `.csproj` lacks `<GenerateDocumentationFile>true</GenerateDocumentationFile>`, so `///` comments don't appear in published packages.

Additional improvements scoped in: inconsistent method naming, factory method clutter, and mutable URL properties on the singleton client.

## Solution: Clean Slate Interface (Approach A)

Introduce request DTOs, remove URL properties from the interface, normalize naming, and enable XML doc shipping.

### Request DTOs

**`RedirectPaymentRequest`** — replaces 7-param `InitiateTransactionAsync`:

| Field | Type | Required |
|---|---|---|
| Amount | decimal | Yes |
| Currency | CurrencyCode | Yes |
| Reason | string | Yes |
| MerchantReference | string? | No |

URLs are not on this DTO — they come from client configuration at DI time.

**`SeamlessPaymentRequest`** — replaces both 9-param `MakeSeamlessPaymentAsync` and `MakeSeamlessCardPaymentAsync`:

| Field | Type | Required |
|---|---|---|
| Method | PaymentMethodCode | Yes |
| Currency | CurrencyCode | Yes |
| Amount | decimal | Yes |
| Reason | string | Yes |
| MerchantReference | string | Yes |
| Email | string? | No |
| CustomerName | string? | No |
| PhoneNumber | string? | Required for mobile money |
| Card | CardDetails? | Required for card payments |

One DTO covers both payment types. The client dispatches internally: `Card` takes priority — if set, it builds card fields. If only `PhoneNumber` is set, it builds mobile money fields. If neither is set, a `PesePayException` is thrown. Both should not be needed simultaneously; if both are provided, `Card` wins.

Both records live in `PesePay.Domain`.

### New Interface

```csharp
public interface IPesePayClient
{
    Task<PesepayResult<InitiateResponse>> InitiateRedirectPaymentAsync(
        RedirectPaymentRequest request, CancellationToken ct = default);

    Task<PesepayResult<PaymentResponse>> InitiateSeamlessPaymentAsync(
        SeamlessPaymentRequest request, CancellationToken ct = default);

    Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(
        string referenceNumber, CancellationToken ct = default);

    Task<PesepayResult<PaymentStatus>> PollPaymentAsync(
        Uri pollUrl, CancellationToken ct = default);

    Task<PesepayResult<List<CurrencyInfo>>> GetActiveCurrenciesAsync(
        CancellationToken ct = default);

    Task<PesepayResult<List<PaymentMethodInfo>>> GetPaymentMethodsAsync(
        string currencyCode, CancellationToken ct = default);
}
```

### What is Removed

- `ResultUrl` / `ReturnUrl` properties from the interface
- `CreateTransaction(...)` factory method
- Both `CreatePayment(...)` overloads
- 7-param `InitiateTransactionAsync` overload
- 9-param `MakeSeamlessPaymentAsync` and `MakeSeamlessCardPaymentAsync` convenience overloads
- Low-level `MakeSeamlessPaymentAsync(Payment, ...)` overload (becomes internal implementation detail)

### What is Renamed

| Old Name | New Name |
|---|---|
| `InitiateTransactionAsync` | `InitiateRedirectPaymentAsync` |
| `MakeSeamlessPaymentAsync` | `InitiateSeamlessPaymentAsync` |
| `PollTransactionAsync` | `PollPaymentAsync` |

### URL Management

`ResultUrl`/`ReturnUrl` remain on `PesePayConfiguration` and are set at DI time. The client stores them privately; the interface never exposes them. `ServiceCollectionExtensions` wiring is unchanged.

### XML Docs

Add to `PesePay.csproj`:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

### What Stays Unchanged

All domain types: `Amount`, `CurrencyCode`, `PaymentMethodCode`, `Customer`, `CardDetails`, `InitiateResponse`, `PaymentResponse`, `PaymentStatus`, `PesepayResult<T>`, `PesePayException`  
All configuration: `PesePayConfiguration`, `ServiceCollectionExtensions`  
Internal: `IPayloadCrypto`, internal test constructor, `Transaction` and `Payment` records (used internally)

## Implementation Notes

- This is a breaking change; bump major version
- `Transaction` and `Payment` domain records remain but are no longer part of the public API surface consumers interact with
- The internal `MakeSeamlessPaymentAsync(Payment, string, decimal, string, Dictionary<string, string>?, CancellationToken)` method on `PesePayClient` becomes private/internal — it's no longer on the interface
- `PesePayConfiguration` already has `ResultUrl`/`ReturnUrl` and they're wired via `ServiceCollectionExtensions` — these feed into the client's private fields
