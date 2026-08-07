# PesePay for .NET

A .NET library for integrating with the [PesePay](https://www.pesepay.com) payment gateway in Zimbabwe.

Supports EcoCash, InnBucks, Visa, MasterCard, Zimswitch, Omari, and PayGo across USD and ZiG.

## Quick Links

- [GitHub Repository](https://github.com/Stelele/pesepay) — Source code, issues, and contributions
- [NuGet Package](https://www.nuget.org/packages/Stelele.PesePay) — `dotnet add package Stelele.PesePay`

## API Reference

Browse the complete API reference for all public types, methods, and enums using the sidebar navigation.

### Namespaces

- **PesePay** — Main client interface (`IPesePayClient`), implementation (`PesePayClient`), configuration (`PesePayConfiguration`), and DI extensions (`ServiceCollectionExtensions`)
- **PesePay.Domain** — Request/response models, enums (`CurrencyCode`, `EnvironmentType`, `PaymentMethodCode`), result types (`PesepayResult<T>`), and exception (`PesePayException`)
- **PesePay.Crypto** — Encryption interface (`IPayloadCrypto`) and AES-CBC implementation (`AesCbcPayloadCrypto`)

### Key Types

| Type | Description |
|------|-------------|
| `IPesePayClient` | Primary interface — initiate payments, check status, discover currencies and payment methods |
| `PesePayClient` | Default implementation with AES-CBC encrypted HTTP communication |
| `SeamlessPaymentRequest` | Server-to-server payment request (mobile money or card) |
| `RedirectPaymentRequest` | Hosted checkout payment request |
| `CardDetails` | Card information for seamless card payments |
| `PesepayResult<T>` | Generic result wrapper with `IsSuccess`, `Data`, and `ErrorMessage` |
| `PesePayException` | Exception type thrown on all payment errors |
