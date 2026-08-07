# PesePay for .NET API Reference

Welcome to the PesePay for .NET API reference documentation.

## Namespaces

- **PesePay** — Main client interface (`IPesePayClient`), implementation (`PesePayClient`), configuration (`PesePayConfiguration`), and DI extensions (`ServiceCollectionExtensions`)
- **PesePay.Domain** — Request/response models, enums, and result types
- **PesePay.Crypto** — Encryption interface (`IPayloadCrypto`) and AES-CBC implementation (`AesCbcPayloadCrypto`)

## Key Types

### Client Interface
- `IPesePayClient` — The primary interface with methods for initiating payments, checking status, and discovering currencies/payment methods
- `PesePayClient` — Default implementation using AES-CBC encrypted HTTP communication
- `PesePayConfiguration` — Configuration class for ASP.NET Core DI registration
- `ServiceCollectionExtensions` — Extension methods for registering `IPesePayClient` with the DI container

### Payment Requests
- `RedirectPaymentRequest` — Request for hosted checkout (PesePay hosts the payment page)
- `SeamlessPaymentRequest` — Request for server-to-server payments (you build the checkout UI)
- `CardDetails` — Card information for seamless card payments

### Responses
- `InitiateResponse` — Response from redirect payment initiation
- `PaymentResponse` — Response from seamless payment initiation
- `PaymentStatus` — Payment status check result

### Enums
- `CurrencyCode` — Supported currencies (USD, ZiG, ZWL)
- `EnvironmentType` — Sandbox or Production
- `PaymentMethodCode` — Payment methods (EcoCash, InnBucks, Visa, MasterCard, Zimswitch, Omari, PayGo)

### Result & Error
- `PesepayResult<T>` — Generic result wrapper with `IsSuccess`, `Data`, and `ErrorMessage`
- `PesePayException` — Exception type thrown on all payment errors

### Cryptography
- `IPayloadCrypto` — Encryption interface
- `AesCbcPayloadCrypto` — AES-CBC implementation using the encryption key from your PesePay account
