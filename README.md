# PesePay for .NET

A .NET library for integrating with the PesePay payment gateway. Supports redirect payments, seamless payments, and payment status checking.

## Installation

```shell
dotnet add package PesePay
```

## Getting Started

Import the namespace:

```csharp
using PesePay;
using PesePay.Domain;
```

Create an instance of `PesePayClient` using your Pesepay integration key and encryption key:

```csharp
var client = new PesePayClient("YOUR_INTEGRATION_KEY", "YOUR_ENCRYPTION_KEY", EnvironmentType.Sandbox);
```

Set the return and result URLs:

```csharp
client.ResultUrl = "https://example.com/result";
client.ReturnUrl = "https://example.com/return";
```

> **Note:** PesePay only supports two currencies — `CurrencyCode.Usd` (USD) and `CurrencyCode.Zwl` (ZWL).

---

## Dependency Injection (ASP.NET Core)

Register the client in `Program.cs`:

```csharp
builder.Services.AddPesePay(options =>
{
    options.IntegrationKey = "YOUR_INTEGRATION_KEY";
    options.EncryptionKey = "YOUR_ENCRYPTION_KEY";
    options.Environment = EnvironmentType.Sandbox;
});

// Or bind from appsettings.json
builder.Services.AddPesePay(builder.Configuration.GetSection("PesePay"));
```

Then inject `IPesePayClient` into your controllers or services:

```csharp
public class PaymentController : ControllerBase
{
    private readonly IPesePayClient _pesepay;

    public PaymentController(IPesePayClient pesepay)
    {
        _pesepay = pesepay;
    }
}
```

---

## Making a Seamless Payment

Create a payment object (customer email or phone number must be provided):

```csharp
var payment = client.CreatePayment(
    CurrencyCode.Zwl,
    "ecocash",
    email: "customer@example.com",
    phone: null,
    name: "John Doe");
```

**Optional:** specify additional required fields for the payment method:

```csharp
var fields = new Dictionary<string, string>
{
    { "ecocashNumber", "0771234567" }
};
```

Send the payment:

```csharp
client.ResultUrl = "https://example.com/result";

var result = await client.MakeSeamlessPaymentAsync(
    payment,
    "Invoice #456",     // reason for payment
    500.00m,            // amount
    fields);

if (result.IsSuccess)
{
    var data = result.Data!;
    var referenceNumber = data.ReferenceNumber;
    var pollUrl = data.PollUrl;
    var isPaid = data.IsPaid;

    // Save reference number and/or poll URL to check status later
}
else
{
    var message = result.ErrorMessage;
}
```

> Available payment method codes are dynamically provided by the PesePay API. Use the currency and payment method endpoints to obtain active codes:
> - Currencies: `GET https://api.pesepay.com/api/payments-engine/v1/currencies/active`
> - Methods: `GET https://api.pesepay.com/api/payments-engine/v1/payment-methods/for-currency?currencyCode=ZWL`

---

## Making a Redirect Payment

Create a transaction:

```csharp
var transaction = client.CreateTransaction(
    100.00m,                    // amount
    CurrencyCode.Usd,           // currency
    "Payment for order #789",   // reason
    "ORDER-789");               // optional merchant reference
```

Initiate the transaction to get a redirect URL:

```csharp
client.ResultUrl = "https://example.com/result";
client.ReturnUrl = "https://example.com/return";

var result = await client.InitiateTransactionAsync(transaction);

if (result.IsSuccess)
{
    var data = result.Data!;
    var referenceNumber = data.ReferenceNumber;
    var pollUrl = data.PollUrl;
    var redirectUrl = data.RedirectUrl;

    // Redirect the customer to redirectUrl to complete payment
    return Redirect(data.RedirectUrl.ToString());
}
else
{
    var message = result.ErrorMessage;
}
```

---

## Checking Payment Status

### Method 1: By reference number

```csharp
var result = await client.CheckPaymentStatusAsync("REF123");

if (result.IsSuccess && result.Data!.IsPaid)
{
    // Payment was successful
}
```

### Method 2: By poll URL

```csharp
var result = await client.PollTransactionAsync(new Uri("https://api.pesepay.com/..."));

if (result.IsSuccess && result.Data!.IsPaid)
{
    // Payment was successful
}
```

---

## Environments

| Environment | Base URL | Enum |
|------------|----------|------|
| Sandbox | `https://api.test.sandbox.pesepay.com/payments-engine` | `EnvironmentType.Sandbox` |
| Production | `https://api.pesepay.com/api/payments-engine` | `EnvironmentType.Production` |

Sandbox is the default if no environment is specified.

---

## Error Handling

The library uses a **hybrid error approach**:

- **API-level errors** (invalid payment, bad currency) return `PesepayResult<T>` with `IsSuccess = false` and an `ErrorMessage`.
- **Network/transport errors** (timeout, DNS failure) throw `PesePayException`.
- **Configuration errors** (missing `ResultUrl`) throw `PesePayException`.

```csharp
try
{
    var result = await client.InitiateTransactionAsync(transaction);

    if (!result.IsSuccess)
    {
        // Handle API business error
        Console.WriteLine($"Payment failed: {result.ErrorMessage}");
        return;
    }

    // Use result.Data
}
catch (PesePayException ex)
{
    // Handle transport or configuration error
    Console.WriteLine($"Error: {ex.Message}");
}
```

---

## Cancellation Support

All async methods accept a `CancellationToken`:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

try
{
    var result = await client.InitiateTransactionAsync(transaction, cts.Token);
}
catch (OperationCanceledException)
{
    // Request timed out
}
```

---

## Models

### Amount
```csharp
var amount = new Amount(10.50m, CurrencyCode.Usd);
// amount.Value → 10.50
// amount.Currency → CurrencyCode.Usd
```

### Customer
```csharp
var customer = new Customer("email@example.com", "0771234567", "John Doe");
// At least email OR phone number must be provided
```

### Transaction
```csharp
var txn = new Transaction(
    new Amount(100m, CurrencyCode.Usd),
    "Payment reason",
    "MERCHANT-REF-001");
```

### Payment
```csharp
var payment = new Payment(CurrencyCode.Zwl, "ecocash", customer)
{
    ReasonForPayment = "Invoice #123",
    AmountDetails = new Amount(500m, CurrencyCode.Zwl),
    ReferenceNumber = "REF-456"
};
```

---

## Running Tests

```shell
dotnet test
```

---

## Target Frameworks

`net8.0`, `net9.0`, `net10.0`

## Dependencies

The library ships with **zero third-party runtime dependencies**. Only Microsoft.Extensions.* abstractions are referenced for optional DI support.
