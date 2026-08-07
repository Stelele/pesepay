# README Redesign Design Spec

**Date:** 2026-08-07 (revised after adversarial review rounds 1-4)
**Status:** Draft

## Problem

The current README has three problems:
1. API examples are outdated — references methods that don't exist (`InitiateTransactionAsync`, `MakeSeamlessPaymentAsync`, `CreateTransaction`, `CreatePayment`, `PollTransactionAsync`, `client.ResultUrl = "..."`)
2. Payment flow guidance is unclear — no explanation of when to use redirect vs seamless
3. Users feel they need to read source code to understand the library

## Research

Researched ~20 top NuGet libraries (Stripe.net, Serilog, Polly, Humanizer, Refit, Square, Mollie, PayPal, MediatR, MailKit, Moq, etc.).

### Findings
- Consensus pattern: `Badges → Tagline → Hero Code → Install → Quick Start → Key Features → API Reference Link`
- Nobody inlines full API reference in README — everyone links to a docs site, wiki, or XML docs
- Sweet spot: ~1,100-1,500 words, ~25% code density
- Best payment libraries (Stripe, Square, Mollie) use progressive disclosure
- Hero code snippet is the most effective pattern — shows value in <10 lines before user scrolls
- README ships in NuGet package — must be NuGet.org safe (absolute URLs, no relative links)

### Audience
Both junior Zimbabwean developers (need copy-paste examples) and experienced .NET developers (need accurate API reference).

## Design

### README Structure

| # | Section | Purpose |
|---|---------|---------|
| 1 | Badges + tagline | NuGet version, license, build status. "A .NET library for integrating with the PesePay payment gateway in Zimbabwe." Below tagline, 3 feature bullets: zero third-party runtime dependencies, typed enum-safe API, supports all payment methods. |
| 2 | Hero code | 7-10 lines: create client with constructor URLs, initiate redirect payment, check status. Note: "Get your sandbox keys at [PesePay dashboard URL]" (replace with actual URL when available). |
| 3 | Installation | `dotnet add package Stelele.PesePay`. Requires .NET 8.0 SDK or later. |
| 4 | Client Setup | Two sub-sections: **Without DI** — `new PesePayClient(key, encKey, Env, resultUrl, returnUrl)`. Reuse a single instance (thread-safe, manages its own `HttpClient`). **With DI** — `AddPesePay` delegate-based and `IConfiguration`-based, with exact `appsettings.json` keys: `IntegrationKey`, `EncryptionKey`, `Environment`, `ResultUrl`, `ReturnUrl`. `Environment` accepts string values `"Sandbox"` / `"Production"` via `IConfiguration.Bind`. | Note: `resultUrl` parameter is nullable in the constructor signature but `InitiateRedirectPaymentAsync` and `InitiateSeamlessPaymentAsync` throw `PesePayException` at call time if it was not provided. `returnUrl` is only required for redirect payments. |
| 5 | Which flow? | Decision guide: **Redirect** = PesePay hosts checkout (recommended for card, less PCI scope). Needs `resultUrl` + `returnUrl`. **Seamless** = you build the UI, server-to-server (required for mobile money USSD push, or full UX control). Needs `resultUrl` only. **For production, prefer webhooks over polling.** |
| 6 | Webhooks (Receiving Callbacks) | Minimal ASP.NET Core `[HttpPost]` controller. The request body is a JSON envelope `{"payload": "<base64>"}` — extract the payload string via `JsonSerializer.Deserialize<JsonElement>(body).GetProperty("payload").GetString()`, then decrypt with `new AesCbcPayloadCrypto(encryptionKey).Decrypt(payload)`. Deserialize the result to `PaymentStatus`. **Return HTTP 200 OK** to acknowledge receipt (otherwise PesePay retries). **Duplicate webhooks:** PesePay may send the same callback multiple times — check `ReferenceNumber` and skip if already processed. **Trust model:** a successfully decrypted payload was encrypted with your shared secret key (AES-CBC, key length determines cipher strength). This provides confidentiality — for additional security, verify the payload structure and reference number after decryption. **Keep your encryption key secret** — if compromised, rotate it in the PesePay dashboard and update your config. **For local dev:** use ngrok or dev tunnels (resultUrl must be a public HTTPS endpoint). **If you cannot expose a public endpoint,** use polling (Section 10) instead — but webhooks are strongly preferred. |
| 7 | Redirect Payment | Complete example: create client with URLs in constructor, build `RedirectPaymentRequest`, call `InitiateRedirectPaymentAsync`, redirect user to `InitiateResponse.RedirectUrl` (non-nullable, safe to dereference). `MerchantReference` is optional — use a unique value per payment for idempotency. Copy-paste ready. |
| 8 | Seamless Payment | Single progressive example covering both mobile money and card. **(a) Mobile money:** `SeamlessPaymentRequest` with `PhoneNumber` (no `Card`). **(b) Card:** `SeamlessPaymentRequest` with `Card: new CardDetails(...)` (no `PhoneNumber`). **If both `Card` and `PhoneNumber` are set, `Card` takes priority silently — always set only one.** `CardDetails` fields: `Number` (digits only), `Cvv` (3-4 digits), `ExpiryDate` (format: `"MM/YY"`), `HolderName` (optional). `PhoneNumber`: Zimbabwe mobile format (e.g., `"0771234567"` or `"+263771234567"`). `PaymentResponse.RedirectUrl` is `Uri?` (nullable) — check before dereferencing on seamless payments. | **PCI note:** Never log card details. |
| 9 | Payment Method Codes | Table mapping `PaymentMethodCode` enum values to PesePay codes (USD vs ZiG). Disclaimer: "Call `GetPaymentMethodsAsync` for the full current list; this table covers the most common methods." ZWL is deprecated, replaced by ZiG. Empty cells for methods not available in a currency (e.g., PayGo is ZiG-only). |
| 10 | Checking Payment Status | `CheckPaymentStatusAsync` by reference number + `PollPaymentAsync` by `Uri` (use `new Uri(pollUrlString)` from storage). **Polling defaults:** poll every 10 seconds; if still pending after 30 minutes, treat as failed and investigate. Check PesePay docs for method-specific timeouts. `IsPaid` = `TransactionStatus == "SUCCESS"`. | Note: `PesepayResult<T>` is always `IsSuccess == true` when no exception is thrown — the `IsSuccess` property and `Fail()` factory exist but are not used in the current implementation. Checking `IsSuccess` is harmless but unnecessary; a non-null `Data` means success. |
| 11 | Environment Switching | Change `EnvironmentType.Sandbox` → `EnvironmentType.Production` and swap to production keys. Never commit production keys to source control. |
| 12 | Cancellation & Timeout | All async methods accept `CancellationToken`. The `HttpClient` has a 30-second hard timeout. Pass a shorter `CancellationToken` (e.g., `new CancellationTokenSource(TimeSpan.FromSeconds(15))`) to cancel before the HTTP timeout fires. |
| 13 | Error Handling | **Exception-based model.** All failure paths throw `PesePayException` (wraps `HttpRequestException` for transport errors, thrown directly for config errors). Catch `PesePayException` for all failure cases. **Known limitations:** malformed JSON responses may throw `JsonException`; wrong encryption keys surface as deserialization errors. For production robustness, consider adding a Polly retry policy (library has no built-in retries). |
| 14 | Full API Reference | Link to documentation (initially links to `IPesePayClient.cs` source on GitHub; will be replaced with docfx-generated docs URL once available). |

### What Gets Removed
- "Advanced: Low-Level API" (references non-existent `CreateTransaction`, `CreatePayment`, `MakeSeamlessPaymentAsync`)
- All outdated method names
- Property-assignment patterns that don't compile (`client.ResultUrl = "..."`)
- Verbose model documentation sections (covered by external API reference)
- Standalone "Target Frameworks" section (merged into Installation)
- "Why PesePay?" prose (replaced with feature bullets under tagline)

### What Gets Preserved
- Payment method code table — with dynamic discovery disclaimer
- Error handling — fixed to match actual exception-based model
- DI registration examples
- Dynamic discovery examples (currencies, payment methods)

### API Surface to Document

- `InitiateRedirectPaymentAsync(RedirectPaymentRequest, CancellationToken)` → `PesepayResult<InitiateResponse>`
- `InitiateSeamlessPaymentAsync(SeamlessPaymentRequest, CancellationToken)` → `PesepayResult<PaymentResponse>`
- `CheckPaymentStatusAsync(string referenceNumber, CancellationToken)` → `PesepayResult<PaymentStatus>`
- `PollPaymentAsync(Uri pollUrl, CancellationToken)` → `PesepayResult<PaymentStatus>`
- `GetActiveCurrenciesAsync(CancellationToken)` → `PesepayResult<List<CurrencyInfo>>`
- `GetPaymentMethodsAsync(string currencyCode, CancellationToken)` → `PesepayResult<List<PaymentMethodInfo>>`

Request types: `RedirectPaymentRequest`, `SeamlessPaymentRequest`, `CardDetails`
Response types: `InitiateResponse`, `PaymentResponse`, `PaymentStatus`
Enums: `CurrencyCode`, `EnvironmentType`, `PaymentMethodCode`
Result wrapper: `PesepayResult<T>` — `IsSuccess` is always `true` when no exception is thrown
Exception: `PesePayException`
Constructor: `PesePayClient(string integrationKey, string encryptionKey, EnvironmentType environment, string? resultUrl, string? returnUrl)` — single instance, thread-safe

### Critical Implementation Constraints

1. **URLs from constructor, never from request objects.** No `client.ResultUrl = "..."`.
2. **Error model is exception-only.** Document honestly: catch `PesePayException`, don't check `IsSuccess`. Note that `IsSuccess` is always `true` when no exception is thrown.
3. **Webhooks:** request body is JSON envelope `{"payload": "<base64>"}` — extract and unwrap before decrypting. Show constructor `new AesCbcPayloadCrypto(encryptionKey).Decrypt(payload)`. Document HTTP 200 response, duplicate detection, honest trust model (confidentiality via shared secret, not authentication). Note key secrecy, rotation, and ngrok for local dev.
4. **`resultUrl` must be public HTTPS.** Note ngrok/dev tunnels.
5. **`PaymentResponse.RedirectUrl` (nullable) vs `InitiateResponse.RedirectUrl` (non-nullable)** — distinguished in examples.
6. **`CardDetails` wrapped in `SeamlessPaymentRequest.Card`, not standalone.** Document card field formats (digits only, MM/YY).
7. **Card vs PhoneNumber exclusivity:** Card takes priority. Always set only one.
8. **NuGet.org:** all links absolute, test rendering.
9. **Stale XML doc refs** — fix before docfx generation. Files: `PaymentResponse.cs` (`MakeSeamlessPaymentAsync` → `InitiateSeamlessPaymentAsync`), `InitiateResponse.cs` (`InitiateTransactionAsync` → `InitiateRedirectPaymentAsync`), `PaymentStatus.cs` (`PollTransactionAsync` → `PollPaymentAsync`).

### docfx + GitHub Pages Setup (Deferred Follow-Up)

- Add `docfx.json`, separate GitHub Actions workflow (not part of `ci.yml`)
- Deploy to `gh-pages` branch with `pages: write`, `id-token: write` permissions
- URL: `https://stelele.github.io/pesepay/`
- Update README Section 15 link when live

### Tone
Professional, direct, benefit-oriented. Developer-to-developer.

### NuGet.org Rendering Checklist (Post-Implementation)
- All links absolute (no relative file paths)
- Code blocks fit without horizontal scroll
- Badges render correctly
- Payment method table: empty cells rendered explicitly (use `—` or `-` for unavailable combinations)
- No unsupported markdown features

## Key Decisions
- Webhooks section **before** payment examples — users need to understand callbacks first
- Client setup merged into one section with DI and non-DI sub-sections
- Feature bullets under tagline replace "Why PesePay?" prose section
- `IsSuccess` story documented honestly: always true, harmless to check, not needed
- Card/phone formats explicitly documented (digits only, MM/YY, Zimbabwe mobile format)
- Webhook: JSON envelope extraction, decryption, HTTP 200, duplicate detection, honest trust model (shared-secret confidentiality, not authentication), key secrecy
- Polling defaults provided (10s interval, 30min timeout) as fallback when PesePay docs are unavailable
- `HttpClient` singleton reuse warning for direct construction users
- PCI note: never log card details
- docfx deferred to follow-up

## Success Criteria
- [ ] All code examples compile and use actual public API (verified against `IPesePayClient.cs`)
- [ ] All method names match current API surface (no old names)
- [ ] No property-setter patterns on `PesePayClient`
- [ ] Redirect examples construct client with URLs in constructor
- [ ] Seamless examples show `CardDetails` wrapped in `SeamlessPaymentRequest.Card`
- [ ] Card-vs-PhoneNumber exclusivity and Card priority noted
- [ ] Card field formats documented (digits only, MM/YY)
- [ ] Phone number format documented (Zimbabwe mobile)
- [ ] Webhook: JSON envelope unwrap (`GetProperty("payload").GetString()`), `new AesCbcPayloadCrypto(key).Decrypt(payload)`, HTTP 200 response, duplicate detection, honest trust model (confidentiality, not authentication), key secrecy, ngrok note
- [ ] Webhook: "if no public endpoint, use polling" fallback
- [ ] Error handling: exception-only model honestly documented, `IsSuccess`=always-true noted, known limitations listed
- [ ] `InitiateResponse.RedirectUrl` (non-nullable) vs `PaymentResponse.RedirectUrl` (nullable) distinguished
- [ ] `resultUrl` parameter nullability vs runtime exception contradiction explained
- [ ] `HttpClient` reuse / thread-safety note in direct construction
- [ ] PCI note: never log card details
- [ ] `appsettings.json` example with exact PascalCase keys
- [ ] `EnvironmentType` string binding note
- [ ] Polling defaults (10s interval, 30min timeout)
- [ ] `PollPaymentAsync` shows `new Uri(string)` conversion
- [ ] Idempotency: unique `MerchantReference` per payment
- [ ] Install: ".NET 8.0 SDK or later"
- [ ] Retry note: use Polly (no built-in retries)
- [ ] Payment method table: dynamic discovery disclaimer, ZWL deprecation, empty cells for unavailable combos
- [ ] All links absolute (NuGet.org safe)
- [ ] No stale `<see cref>` references (fix 3 files: PaymentResponse.cs, InitiateResponse.cs, PaymentStatus.cs)
- [ ] Feature bullets under tagline (no standalone "Why PesePay?" section)
- [ ] Client setup merged with DI + non-DI
- [ ] Final: ~1,100-1,500 words, ~25% code density

## Scope Boundaries
- **In scope:** README rewrite (14 sections), fix stale XML doc `<see cref>` references in 3 files (`PaymentResponse.cs`, `InitiateResponse.cs`, `PaymentStatus.cs`)
- **Deferred to follow-up:** docfx config, GitHub Actions workflow for docfx deployment, website theming
- **Out of scope:** Modifying library code (beyond XML doc comments), changing the NuGet package structure, adding features to the library
