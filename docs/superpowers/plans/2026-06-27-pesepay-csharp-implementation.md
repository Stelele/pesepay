# PesePay C# Library Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the PesePay TypeScript payment gateway library to idiomatic C# as a .NET class library with zero external runtime dependencies.

**Architecture:** Single C# project with 3 namespace folders — Domain (models, enums, result types), Crypto (AES-256-CBC encrypt/decrypt behind IPayloadCrypto), and root (IPesePayClient, PesePayClient, DI extensions). Immutable records for models, interface-based client for testability, static HttpClient internally.

**Tech Stack:** .NET 10.0 (net8.0/9.0 TFM properties ready for multi-targeting when SDKs available), xunit for tests, System.Text.Json, System.Security.Cryptography, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Configuration.Abstractions.

**Note:** Only .NET 10.0 SDK is installed. Multi-target TFMs (net8.0;net9.0) are set in the csproj but `dotnet test` and `dotnet build` will only target net10.0 until additional SDKs are installed.

---

### Task 1: Set Up Project Infrastructure

**Files:**
- Modify: `PesePay.csproj`
- Create: `PesePay.Tests/PesePay.Tests.csproj`
- Create: `PesePay.Tests/GlobalUsings.cs`

- [ ] **Step 1: Update PesePay.csproj for multi-targeting and add DI package references**

Replace the content of `PesePay.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>PesePay</RootNamespace>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="PesePay.Tests" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="9.0.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Restore packages**

Run: `dotnet restore`
Expected: Success.

- [ ] **Step 3: Create test project**

Create directory: `PesePay.Tests/`

Write `PesePay.Tests/PesePay.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\PesePay.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create GlobalUsings.cs for tests**

Write `PesePay.Tests/GlobalUsings.cs`:

```csharp
global using Xunit;
```

- [ ] **Step 5: Verify test project builds**

Run: `dotnet build PesePay.Tests/PesePay.Tests.csproj`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add PesePay.csproj PesePay.Tests/
git commit -m "chore: set up project infrastructure and test project"
```

---

### Task 2: CurrencyCode Enum

**Files:**
- Create: `Domain/CurrencyCode.cs`
- Create: `PesePay.Tests/Domain/CurrencyCodeTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/CurrencyCodeTests.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class CurrencyCodeTests
{
    [Fact]
    public void CurrencyCode_Serializes_To_Correct_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(CurrencyCode.USD, options);

        Assert.Equal("\"usd\"", json);
    }

    [Fact]
    public void CurrencyCode_Zwl_Serializes_To_zwl()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(CurrencyCode.ZWL, options);

        Assert.Equal("\"zwl\"", json);
    }

    [Fact]
    public void CurrencyCode_Deserializes_From_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var result = JsonSerializer.Deserialize<CurrencyCode>("\"zwl\"", options);

        Assert.Equal(CurrencyCode.ZWL, result);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "CurrencyCodeTests"`
Expected: FAIL — "The type or namespace name 'CurrencyCode' does not exist"

- [ ] **Step 2: Implement CurrencyCode enum**

Write `Domain/CurrencyCode.cs`:

```csharp
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PesePay.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CurrencyCode
{
    [EnumMember(Value = "USD")]
    Usd,

    [EnumMember(Value = "ZWL")]
    Zwl
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "CurrencyCodeTests"`
Expected: 3 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/CurrencyCode.cs PesePay.Tests/Domain/CurrencyCodeTests.cs
git commit -m "feat: add CurrencyCode enum with USD and ZWL"
```

---

### Task 3: TransactionType Enum

**Files:**
- Create: `Domain/TransactionType.cs`
- Create: `PesePay.Tests/Domain/TransactionTypeTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/TransactionTypeTests.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class TransactionTypeTests
{
    [Fact]
    public void TransactionType_Serializes_To_basic()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(TransactionType.Basic, options);

        Assert.Equal("\"basic\"", json);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "TransactionTypeTests"`
Expected: FAIL — "The type or namespace name 'TransactionType' does not exist"

- [ ] **Step 2: Implement TransactionType enum**

Write `Domain/TransactionType.cs`:

```csharp
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PesePay.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    [EnumMember(Value = "BASIC")]
    Basic
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "TransactionTypeTests"`
Expected: 1 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/TransactionType.cs PesePay.Tests/Domain/TransactionTypeTests.cs
git commit -m "feat: add TransactionType enum with Basic"
```

---

### Task 4: EnvironmentType Enum

**Files:**
- Create: `Domain/EnvironmentType.cs`
- Create: `PesePay.Tests/Domain/EnvironmentTypeTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/EnvironmentTypeTests.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class EnvironmentTypeTests
{
    [Fact]
    public void EnvironmentType_Has_Sandbox_And_Production()
    {
        var values = Enum.GetValues<EnvironmentType>();
        Assert.Contains(EnvironmentType.Sandbox, values);
        Assert.Contains(EnvironmentType.Production, values);
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void EnvironmentType_Serializes_As_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(EnvironmentType.Sandbox, options);

        Assert.Equal("\"Sandbox\"", json);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "EnvironmentTypeTests"`
Expected: FAIL — "The type or namespace name 'EnvironmentType' does not exist"

- [ ] **Step 2: Implement EnvironmentType enum**

Write `Domain/EnvironmentType.cs`:

```csharp
namespace PesePay.Domain;

public enum EnvironmentType
{
    Sandbox,
    Production
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "EnvironmentTypeTests"`
Expected: 2 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/EnvironmentType.cs PesePay.Tests/Domain/EnvironmentTypeTests.cs
git commit -m "feat: add EnvironmentType enum with Sandbox and Production"
```

---

### Task 5: Amount Record Struct

**Files:**
- Create: `Domain/Amount.cs`
- Create: `PesePay.Tests/Domain/AmountTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/AmountTests.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class AmountTests
{
    [Fact]
    public void Amount_Serializes_With_SnakeCase()
    {
        var amount = new Amount(10.50m, CurrencyCode.USD);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };

        var json = JsonSerializer.Serialize(amount, options);

        Assert.Contains("\"value\":10.50", json);
        Assert.Contains("\"currency\":\"usd\"", json);
    }

    [Fact]
    public void Amount_Equality_Is_Value_Based()
    {
        var a1 = new Amount(10m, CurrencyCode.USD);
        var a2 = new Amount(10m, CurrencyCode.USD);
        var a3 = new Amount(20m, CurrencyCode.USD);

        Assert.Equal(a1, a2);
        Assert.NotEqual(a1, a3);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "AmountTests"`
Expected: FAIL — "The type or namespace name 'Amount' does not exist"

- [ ] **Step 2: Implement Amount record struct**

Write `Domain/Amount.cs`:

```csharp
namespace PesePay.Domain;

public readonly record struct Amount(decimal Value, CurrencyCode Currency);
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "AmountTests"`
Expected: 2 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/Amount.cs PesePay.Tests/Domain/AmountTests.cs
git commit -m "feat: add Amount record struct with value equality"
```

---

### Task 6: Customer Record

**Files:**
- Create: `Domain/Customer.cs`
- Create: `PesePay.Tests/Domain/CustomerTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/CustomerTests.cs`:

```csharp
using System.Text.Json;

namespace PesePay.Domain.Tests;

public class CustomerTests
{
    [Fact]
    public void Customer_Created_With_Email_Only()
    {
        var customer = new Customer("test@example.com", null, null);

        Assert.Equal("test@example.com", customer.Email);
        Assert.Null(customer.PhoneNumber);
        Assert.Null(customer.Name);
    }

    [Fact]
    public void Customer_Serializes_With_SnakeCase()
    {
        var customer = new Customer("a@b.com", "123456", "John");
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(customer, options);

        Assert.Contains("\"email\":\"a@b.com\"", json);
        Assert.Contains("\"phone_number\":\"123456\"", json);
        Assert.Contains("\"name\":\"John\"", json);
    }

    [Fact]
    public void Customer_Null_Properties_Omitted()
    {
        var customer = new Customer("a@b.com", null, null);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(customer, options);

        Assert.DoesNotContain("phone_number", json);
        Assert.DoesNotContain("name", json);
    }

    [Fact]
    public void Customer_Throws_When_Email_And_Phone_Both_Null()
    {
        var ex = Assert.Throws<PesePayException>(() => new Customer(null, null, "John"));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Customer_With_Phone_Only_Is_Valid()
    {
        var customer = new Customer(null, "123456", null);
        Assert.Null(customer.Email);
        Assert.Equal("123456", customer.PhoneNumber);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "CustomerTests"`
Expected: FAIL — "The type or namespace name 'Customer' does not exist"

- [ ] **Step 2: Implement Customer record**

Write `Domain/Customer.cs`:

```csharp
namespace PesePay.Domain;

public record Customer
{
    public string? Email { get; }
    public string? PhoneNumber { get; }
    public string? Name { get; }

    public Customer(string? email, string? phoneNumber, string? name)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phoneNumber))
            throw new PesePayException("Customer details should have an email and/or phone number.");

        Email = email;
        PhoneNumber = phoneNumber;
        Name = name;
    }
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "CustomerTests"`
Expected: 5 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/Customer.cs PesePay.Tests/Domain/CustomerTests.cs
git commit -m "feat: add Customer record with email, phone, name"
```

---

### Task 7: Transaction Record

**Files:**
- Create: `Domain/Transaction.cs`
- Create: `PesePay.Tests/Domain/TransactionTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/TransactionTests.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class TransactionTests
{
    private static JsonSerializerOptions ApiOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void Transaction_Default_Type_Is_Basic()
    {
        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            CurrencyCode.USD,
            "Test payment");

        Assert.Equal(TransactionType.Basic, txn.Type);
    }

    [Fact]
    public void Transaction_Serializes_With_SnakeCase()
    {
        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            CurrencyCode.USD,
            "Test payment",
            "MERCH001");

        var json = JsonSerializer.Serialize(txn, ApiOptions);

        Assert.Contains("\"amount_details\"", json);
        Assert.Contains("\"currency_code\":\"usd\"", json);
        Assert.Contains("\"reason_for_payment\":\"Test payment\"", json);
        Assert.Contains("\"merchant_reference\":\"MERCH001\"", json);
        Assert.Contains("\"transaction_type\":\"basic\"", json);
    }

    [Fact]
    public void Transaction_ResultUrl_And_ReturnUrl_Settable()
    {
        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            CurrencyCode.USD,
            "Test");

        txn.ResultUrl = "https://example.com/result";
        txn.ReturnUrl = "https://example.com/return";

        Assert.Equal("https://example.com/result", txn.ResultUrl);
        Assert.Equal("https://example.com/return", txn.ReturnUrl);
    }

    [Fact]
    public void Transaction_ResultUrl_Serializes_When_Set()
    {
        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            CurrencyCode.USD,
            "Test")
        {
            ResultUrl = "https://example.com/result",
            ReturnUrl = "https://example.com/return"
        };

        var json = JsonSerializer.Serialize(txn, ApiOptions);

        Assert.Contains("\"result_url\":\"https://example.com/result\"", json);
        Assert.Contains("\"return_url\":\"https://example.com/return\"", json);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "TransactionTests"`
Expected: FAIL — "The type or namespace name 'Transaction' does not exist"

- [ ] **Step 2: Implement Transaction record**

Write `Domain/Transaction.cs`:

```csharp
namespace PesePay.Domain;

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
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "TransactionTests"`
Expected: 4 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/Transaction.cs PesePay.Tests/Domain/TransactionTests.cs
git commit -m "feat: add Transaction record with Amount and settable URLs"
```

---

### Task 8: Payment Record

**Files:**
- Create: `Domain/Payment.cs`
- Create: `PesePay.Tests/Domain/PaymentTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/PaymentTests.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class PaymentTests
{
    private static JsonSerializerOptions ApiOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void Payment_Created_With_Required_Fields()
    {
        var customer = new Customer("a@b.com", null, null);
        var payment = new Payment(CurrencyCode.USD, "ecocash", customer);

        Assert.Equal(CurrencyCode.USD, payment.CurrencyCode);
        Assert.Equal("ecocash", payment.PaymentMethodCode);
        Assert.Equal(customer, payment.Customer);
    }

    [Fact]
    public void Payment_Serializes_Correctly()
    {
        var customer = new Customer("a@b.com", "123", "John");
        var payment = new Payment(CurrencyCode.ZWL, "ecocash", customer)
        {
            ReasonForPayment = "Invoice #123",
            AmountDetails = new Amount(500m, CurrencyCode.ZWL),
            ResultUrl = "https://ex.com/result",
            ReturnUrl = "https://ex.com/return",
            RequiredFields = new Dictionary<string, string> { { "field1", "value1" } }
        };

        var json = JsonSerializer.Serialize(payment, ApiOptions);

        Assert.Contains("\"currency_code\":\"zwl\"", json);
        Assert.Contains("\"payment_method_code\":\"ecocash\"", json);
        Assert.Contains("\"reason_for_payment\":\"Invoice #123\"", json);
        Assert.Contains("\"customer\"", json);
        Assert.Contains("\"amount_details\"", json);
        Assert.Contains("\"required_fields\"", json);
    }

    [Fact]
    public void Payment_Optional_Fields_Omitted_When_Null()
    {
        var customer = new Customer("a@b.com", null, null);
        var payment = new Payment(CurrencyCode.USD, "ecocash", customer);

        var json = JsonSerializer.Serialize(payment, ApiOptions);

        Assert.DoesNotContain("reason_for_payment", json);
        Assert.DoesNotContain("amount_details", json);
        Assert.DoesNotContain("result_url", json);
        Assert.DoesNotContain("return_url", json);
        Assert.DoesNotContain("required_fields", json);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PaymentTests"`
Expected: FAIL — "The type or namespace name 'Payment' does not exist"

- [ ] **Step 2: Implement Payment record**

Write `Domain/Payment.cs`:

```csharp
namespace PesePay.Domain;

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

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PaymentTests"`
Expected: 3 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/Payment.cs PesePay.Tests/Domain/PaymentTests.cs
git commit -m "feat: add Payment record with Customer and required fields"
```

---

### Task 9: PesepayResult<T>

**Files:**
- Create: `Domain/PesepayResult.cs`
- Create: `PesePay.Tests/Domain/PesepayResultTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/PesepayResultTests.cs`:

```csharp
namespace PesePay.Domain.Tests;

public class PesepayResultTests
{
    [Fact]
    public void Ok_Creates_Success_Result()
    {
        var result = PesepayResult<string>.Ok("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Data);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Fail_Creates_Failure_Result()
    {
        var result = PesepayResult<string>.Fail("Something went wrong");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal("Something went wrong", result.ErrorMessage);
    }

    [Fact]
    public void Ok_With_ReferenceType()
    {
        var data = new { Name = "test" };
        var result = PesepayResult<object>.Ok(data);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesepayResultTests"`
Expected: FAIL — "The type or namespace name 'PesepayResult<>' does not exist"

- [ ] **Step 2: Implement PesepayResult<T>**

Write `Domain/PesepayResult.cs`:

```csharp
namespace PesePay.Domain;

public class PesepayResult<T>
    where T : notnull
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    private PesepayResult(bool isSuccess, T? data, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static PesepayResult<T> Ok(T data) =>
        new(true, data, null);

    public static PesepayResult<T> Fail(string errorMessage) =>
        new(false, default, errorMessage);
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesepayResultTests"`
Expected: 3 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/PesepayResult.cs PesePay.Tests/Domain/PesepayResultTests.cs
git commit -m "feat: add PesepayResult<T> discriminated result type"
```

---

### Task 10: PesePayException

**Files:**
- Create: `Domain/PesePayException.cs`
- Create: `PesePay.Tests/Domain/PesePayExceptionTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/PesePayExceptionTests.cs`:

```csharp
namespace PesePay.Domain.Tests;

public class PesePayExceptionTests
{
    [Fact]
    public void PesePayException_Stores_Message()
    {
        var ex = new PesePayException("Configuration error");

        Assert.Equal("Configuration error", ex.Message);
    }

    [Fact]
    public void PesePayException_Wraps_Inner_Exception()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new PesePayException("Outer", inner);

        Assert.Equal("Outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesePayExceptionTests"`
Expected: FAIL — "The type or namespace name 'PesePayException' does not exist"

- [ ] **Step 2: Implement PesePayException**

Write `Domain/PesePayException.cs`:

```csharp
namespace PesePay.Domain;

public class PesePayException : Exception
{
    public PesePayException(string message) : base(message) { }
    public PesePayException(string message, Exception inner) : base(message, inner) { }
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesePayExceptionTests"`
Expected: 2 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/PesePayException.cs PesePay.Tests/Domain/PesePayExceptionTests.cs
git commit -m "feat: add PesePayException for transport/validation errors"
```

---

### Task 11: Response Models

**Files:**
- Create: `Domain/InitiateResponse.cs`
- Create: `Domain/PaymentResponse.cs`
- Create: `Domain/PaymentStatus.cs`
- Create: `PesePay.Tests/Domain/ResponseModelTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Domain/ResponseModelTests.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PesePay.Domain.Tests;

public class ResponseModelTests
{
    private static JsonSerializerOptions ApiOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void InitiateResponse_Parses_From_Api_Json()
    {
        var json = """{"reference_number":"REF123","poll_url":"https://api.pesepay.com/poll/REF123","redirect_url":"https://checkout.pesepay.com/pay/REF123"}""";

        var response = JsonSerializer.Deserialize<InitiateResponse>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.Equal("REF123", response.ReferenceNumber);
        Assert.Equal("https://api.pesepay.com/poll/REF123", response.PollUrl.ToString());
        Assert.Equal("https://checkout.pesepay.com/pay/REF123", response.RedirectUrl.ToString());
    }

    [Fact]
    public void PaymentResponse_Parses_From_Api_Json()
    {
        var json = """{"reference_number":"REF456","poll_url":"https://api.pesepay.com/poll/REF456","redirect_url":null,"transaction_status":"SUCCESS"}""";

        var response = JsonSerializer.Deserialize<PaymentResponse>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.Equal("REF456", response.ReferenceNumber);
        Assert.Equal("SUCCESS", response.TransactionStatus);
        Assert.Null(response.RedirectUrl);
        Assert.True(response.IsPaid);
    }

    [Fact]
    public void PaymentStatus_Parses_From_Api_Json_NotPaid()
    {
        var json = """{"reference_number":"REF789","poll_url":"https://api.pesepay.com/poll/REF789","redirect_url":null,"transaction_status":"PENDING"}""";

        var response = JsonSerializer.Deserialize<PaymentStatus>(json, ApiOptions);

        Assert.NotNull(response);
        Assert.False(response.IsPaid);
    }

    [Fact]
    public void IsPaid_False_When_Not_Success()
    {
        var status = new PaymentStatus("R1", new Uri("http://example.com"), null, "FAILED");
        Assert.False(status.IsPaid);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "ResponseModelTests"`
Expected: FAIL — "The type or namespace name 'InitiateResponse' does not exist"

- [ ] **Step 2: Implement response models**

Write `Domain/InitiateResponse.cs`:

```csharp
namespace PesePay.Domain;

public record InitiateResponse(string ReferenceNumber, Uri PollUrl, Uri RedirectUrl);
```

Write `Domain/PaymentResponse.cs`:

```csharp
namespace PesePay.Domain;

public record PaymentResponse(string ReferenceNumber, Uri PollUrl, Uri? RedirectUrl, string TransactionStatus)
{
    public bool IsPaid => TransactionStatus == "SUCCESS";
}
```

Write `Domain/PaymentStatus.cs`:

```csharp
namespace PesePay.Domain;

public record PaymentStatus(string ReferenceNumber, Uri PollUrl, Uri? RedirectUrl, string TransactionStatus)
{
    public bool IsPaid => TransactionStatus == "SUCCESS";
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "ResponseModelTests"`
Expected: 4 passed.

- [ ] **Step 4: Commit**

```bash
git add Domain/InitiateResponse.cs Domain/PaymentResponse.cs Domain/PaymentStatus.cs PesePay.Tests/Domain/ResponseModelTests.cs
git commit -m "feat: add response models for API deserialization"
```

---

### Task 12: IPayloadCrypto + AesCbcPayloadCrypto

**Files:**
- Create: `Crypto/IPayloadCrypto.cs`
- Create: `Crypto/AesCbcPayloadCrypto.cs`
- Create: `PesePay.Tests/Crypto/AesCbcPayloadCryptoTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/Crypto/AesCbcPayloadCryptoTests.cs`:

```csharp
using System.Text;

namespace PesePay.Crypto.Tests;

public class AesCbcPayloadCryptoTests
{
    private const string EncryptionKey = "test-encryption-key-32chars!!!";
    private const string Payload = """{"amount":100,"currency":"USD"}""";

    [Fact]
    public void Encrypt_Produces_Base64_String()
    {
        var crypto = new AesCbcPayloadCrypto(EncryptionKey);

        var encrypted = crypto.Encrypt(Payload);

        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(Payload, encrypted);
    }

    [Fact]
    public void Decrypt_Roundtrip_Returns_Original()
    {
        var crypto = new AesCbcPayloadCrypto(EncryptionKey);

        var encrypted = crypto.Encrypt(Payload);
        var decrypted = crypto.Decrypt(encrypted);

        Assert.Equal(Payload, decrypted);
    }

    [Fact]
    public void Encrypt_Same_Input_Produces_Different_Output()
    {
        var crypto = new AesCbcPayloadCrypto(EncryptionKey);

        var enc1 = crypto.Encrypt(Payload);
        var enc2 = crypto.Encrypt(Payload);

        Assert.NotEqual(enc1, enc2);
    }

    [Fact]
    public void Decrypt_With_Wrong_Key_Throws()
    {
        var crypto1 = new AesCbcPayloadCrypto("key-one-is-exactly-32-chars!!");
        var crypto2 = new AesCbcPayloadCrypto("key-two-is-exactly-32-chars!!");

        var encrypted = crypto1.Encrypt(Payload);

        Assert.ThrowsAny<Exception>(() => crypto2.Decrypt(encrypted));
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "AesCbcPayloadCryptoTests"`
Expected: FAIL — "The type or namespace name 'Crypto' does not exist"

- [ ] **Step 2: Implement IPayloadCrypto interface**

Write `Crypto/IPayloadCrypto.cs`:

```csharp
namespace PesePay.Crypto;

public interface IPayloadCrypto
{
    string Encrypt(string jsonPayload);
    string Decrypt(string base64Payload);
}
```

- [ ] **Step 3: Implement AesCbcPayloadCrypto**

Write `Crypto/AesCbcPayloadCrypto.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

namespace PesePay.Crypto;

public class AesCbcPayloadCrypto : IPayloadCrypto
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesCbcPayloadCrypto(string encryptionKey)
    {
        _key = Encoding.UTF8.GetBytes(encryptionKey);
        _iv = Encoding.UTF8.GetBytes(encryptionKey[..Math.Min(16, encryptionKey.Length)]);
    }

    public string Encrypt(string jsonPayload)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(jsonPayload);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string base64Payload)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(base64Payload);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "AesCbcPayloadCryptoTests"`
Expected: 4 passed.

- [ ] **Step 5: Commit**

```bash
git add Crypto/IPayloadCrypto.cs Crypto/AesCbcPayloadCrypto.cs PesePay.Tests/Crypto/AesCbcPayloadCryptoTests.cs
git commit -m "feat: add AES-256-CBC payload encryption/decryption"
```

---

### Task 13: PesePayConfiguration

**Files:**
- Create: `PesePayConfiguration.cs`
- Create: `PesePay.Tests/PesePayConfigurationTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/PesePayConfigurationTests.cs`:

```csharp
using PesePay.Domain;

namespace PesePay.Tests;

public class PesePayConfigurationTests
{
    [Fact]
    public void Default_Environment_Is_Sandbox()
    {
        var config = new PesePayConfiguration
        {
            IntegrationKey = "key",
            EncryptionKey = "enc"
        };

        Assert.Equal(EnvironmentType.Sandbox, config.Environment);
    }

    [Fact]
    public void Properties_Are_Settable()
    {
        var config = new PesePayConfiguration
        {
            IntegrationKey = "int-key",
            EncryptionKey = "enc-key",
            Environment = EnvironmentType.Production,
            ResultUrl = "https://example.com/result",
            ReturnUrl = "https://example.com/return"
        };

        Assert.Equal("int-key", config.IntegrationKey);
        Assert.Equal("enc-key", config.EncryptionKey);
        Assert.Equal(EnvironmentType.Production, config.Environment);
        Assert.Equal("https://example.com/result", config.ResultUrl);
        Assert.Equal("https://example.com/return", config.ReturnUrl);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesePayConfigurationTests"`
Expected: FAIL — "The type or namespace name 'PesePayConfiguration' does not exist"

- [ ] **Step 2: Implement PesePayConfiguration**

Write `PesePayConfiguration.cs`:

```csharp
using PesePay.Domain;

namespace PesePay;

public class PesePayConfiguration
{
    public string IntegrationKey { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public EnvironmentType Environment { get; set; } = EnvironmentType.Sandbox;
    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesePayConfigurationTests"`
Expected: 2 passed.

- [ ] **Step 4: Commit**

```bash
git add PesePayConfiguration.cs PesePay.Tests/PesePayConfigurationTests.cs
git commit -m "feat: add PesePayConfiguration options class"
```

---

### Task 14: IPesePayClient Interface

**Files:**
- Create: `IPesePayClient.cs`
- Create: `PesePay.Tests/IPesePayClientTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/IPesePayClientTests.cs`:

```csharp
using PesePay.Domain;

namespace PesePay.Tests;

public class IPesePayClientTests
{
    [Fact]
    public void Interface_Defines_All_Expected_Members()
    {
        var type = typeof(IPesePayClient);

        Assert.NotNull(type.GetProperty("ResultUrl"));
        Assert.NotNull(type.GetProperty("ReturnUrl"));

        Assert.NotNull(type.GetMethod("CreateTransaction"));
        Assert.NotNull(type.GetMethod("CreatePayment"));
        Assert.NotNull(type.GetMethod("InitiateTransactionAsync"));
        Assert.NotNull(type.GetMethod("MakeSeamlessPaymentAsync"));
        Assert.NotNull(type.GetMethod("CheckPaymentStatusAsync"));
        Assert.NotNull(type.GetMethod("PollTransactionAsync"));
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "IPesePayClientTests"`
Expected: FAIL — "The type or namespace name 'IPesePayClient' does not exist"

- [ ] **Step 2: Implement IPesePayClient interface**

Write `IPesePayClient.cs`:

```csharp
using PesePay.Domain;

namespace PesePay;

public interface IPesePayClient
{
    string? ResultUrl { get; set; }
    string? ReturnUrl { get; set; }

    Transaction CreateTransaction(decimal amount, CurrencyCode currency, string reason, string? merchantRef = null);
    Payment CreatePayment(CurrencyCode currency, string methodCode, string? email, string? phone, string? name);

    Task<PesepayResult<InitiateResponse>> InitiateTransactionAsync(Transaction transaction, CancellationToken ct = default);
    Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(Payment payment, string reason, decimal amount, Dictionary<string, string>? fields = null, CancellationToken ct = default);
    Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(string referenceNumber, CancellationToken ct = default);
    Task<PesepayResult<PaymentStatus>> PollTransactionAsync(Uri pollUrl, CancellationToken ct = default);
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "IPesePayClientTests"`
Expected: 1 passed.

- [ ] **Step 4: Commit**

```bash
git add IPesePayClient.cs PesePay.Tests/IPesePayClientTests.cs
git commit -m "feat: add IPesePayClient interface"
```

---

### Task 15: PesePayClient — Construction and Factory Methods

**Files:**
- Create: `PesePayClient.cs`
- Create: `PesePay.Tests/PesePayClientFactoryTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/PesePayClientFactoryTests.cs`:

```csharp
using PesePay.Domain;

namespace PesePay.Tests;

public class PesePayClientFactoryTests
{
    private readonly PesePayClient _client = new(
        "integration-key",
        "encryption-key-32chars-long!!",
        EnvironmentType.Sandbox);

    [Fact]
    public void CreateTransaction_Returns_Transaction_With_Correct_Values()
    {
        var txn = _client.CreateTransaction(100m, CurrencyCode.USD, "Payment for order", "ORDER-001");

        Assert.Equal(100m, txn.AmountDetails.Value);
        Assert.Equal(CurrencyCode.USD, txn.AmountDetails.Currency);
        Assert.Equal(CurrencyCode.USD, txn.CurrencyCode);
        Assert.Equal("Payment for order", txn.ReasonForPayment);
        Assert.Equal("ORDER-001", txn.MerchantReference);
        Assert.Equal(TransactionType.Basic, txn.Type);
    }

    [Fact]
    public void CreatePayment_Returns_Payment_With_Correct_Values()
    {
        var payment = _client.CreatePayment(CurrencyCode.ZWL, "ecocash", "a@b.com", "123", "John");

        Assert.Equal(CurrencyCode.ZWL, payment.CurrencyCode);
        Assert.Equal("ecocash", payment.PaymentMethodCode);
        Assert.Equal("a@b.com", payment.Customer.Email);
        Assert.Equal("123", payment.Customer.PhoneNumber);
        Assert.Equal("John", payment.Customer.Name);
    }

    [Fact]
    public void CreatePayment_With_Email_Only()
    {
        var payment = _client.CreatePayment(CurrencyCode.USD, "visa", "a@b.com", null, null);

        Assert.Equal("a@b.com", payment.Customer.Email);
        Assert.Null(payment.Customer.PhoneNumber);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesePayClientFactoryTests"`
Expected: FAIL — "The type or namespace name 'PesePayClient' does not exist"

- [ ] **Step 2: Implement PesePayClient (constructor + factory methods only)**

Write `PesePayClient.cs`:

```csharp
using PesePay.Crypto;
using PesePay.Domain;

namespace PesePay;

public class PesePayClient : IPesePayClient
{
    private readonly string _integrationKey;
    private readonly EnvironmentType _environment;
    private readonly IPayloadCrypto _crypto;
    private readonly HttpClient _httpClient;

    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }

    public PesePayClient(string integrationKey, string encryptionKey, EnvironmentType environment = EnvironmentType.Sandbox)
    {
        _integrationKey = integrationKey;
        _environment = environment;
        _crypto = new AesCbcPayloadCrypto(encryptionKey);

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GetBaseUrl(environment)),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("key", integrationKey);
    }

    internal PesePayClient(IPayloadCrypto crypto, HttpClient httpClient, EnvironmentType environment = EnvironmentType.Sandbox)
    {
        _crypto = crypto;
        _httpClient = httpClient;
        _environment = environment;
        _integrationKey = string.Empty;
    }

    private static string GetBaseUrl(EnvironmentType environment) => environment switch
    {
        EnvironmentType.Production => "https://api.pesepay.com/api/payments-engine",
        EnvironmentType.Sandbox => "https://api.test.sandbox.pesepay.com/payments-engine",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    public Transaction CreateTransaction(decimal amount, CurrencyCode currency, string reason, string? merchantRef = null)
    {
        return new Transaction(
            new Amount(amount, currency),
            currency,
            reason,
            merchantRef);
    }

    public Payment CreatePayment(CurrencyCode currency, string methodCode, string? email, string? phone, string? name)
    {
        var customer = new Customer(email, phone, name);
        return new Payment(currency, methodCode, customer);
    }

    // API methods implemented in next tasks
    public Task<PesepayResult<InitiateResponse>> InitiateTransactionAsync(Transaction transaction, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(Payment payment, string reason, decimal amount, Dictionary<string, string>? fields = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(string referenceNumber, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PesepayResult<PaymentStatus>> PollTransactionAsync(Uri pollUrl, CancellationToken ct = default)
        => throw new NotImplementedException();
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesePayClientFactoryTests"`
Expected: 3 passed.

- [ ] **Step 4: Commit**

```bash
git add PesePayClient.cs PesePay.Tests/PesePayClientFactoryTests.cs
git commit -m "feat: add PesePayClient constructor and factory methods"
```

---

### Task 16: PesePayClient — InitiateTransactionAsync

**Files:**
- Modify: `PesePayClient.cs`
- Create: `PesePay.Tests/PesePayClientApiTests.cs`

- [ ] **Step 1: Write the failing integration test (mocked HTTP)**

Write `PesePay.Tests/PesePayClientApiTests.cs`:

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PesePay.Crypto;
using PesePay.Domain;
using Moq;
using Xunit.Abstractions;

namespace PesePay.Tests;

public class PesePayClientApiTests
{
    private static JsonSerializerOptions ApiOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public async Task InitiateTransactionAsync_Sends_Encrypted_Payload()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("test-key-32-chars-long!!!!!!");
        var expectedResponse = new InitiateResponse("REF001", new Uri("https://poll.example.com/REF001"), new Uri("https://redirect.example.com/REF001"));
        var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
        var encryptedResponse = crypto.Encrypt(responseJson);
        var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

        handler.SetResponse(HttpStatusCode.OK, responsePayload);

        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result",
            ReturnUrl = "https://example.com/return"
        };

        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            CurrencyCode.USD,
            "Test payment");

        var result = await client.InitiateTransactionAsync(txn);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("REF001", result.Data.ReferenceNumber);
        Assert.Equal("https://poll.example.com/REF001", result.Data.PollUrl.ToString());
        Assert.Equal("https://redirect.example.com/REF001", result.Data.RedirectUrl.ToString());
    }

    [Fact]
    public async Task InitiateTransactionAsync_Throws_When_ResultUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("test-key-32-chars-long!!!!!!");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            CurrencyCode.USD,
            "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
    }

    [Fact]
    public async Task InitiateTransactionAsync_Throws_When_ReturnUrl_Missing()
    {
        var handler = new FakeHttpMessageHandler();
        var crypto = new AesCbcPayloadCrypto("test-key-32-chars-long!!!!!!");
        var httpClient = new HttpClient(handler);
        var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
        {
            ResultUrl = "https://example.com/result"
        };

        var txn = new Transaction(
            new Amount(10m, CurrencyCode.USD),
            CurrencyCode.USD,
            "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
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
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesePayClientApiTests"`
Expected: FAIL — "PesepayException" from missing ResultUrl test passes, but the success test fails (NotImplementedException).

- [ ] **Step 2: Implement InitiateTransactionAsync**

Replace the placeholder `InitiateTransactionAsync` in `PesePayClient.cs` with:

```csharp
public async Task<PesepayResult<InitiateResponse>> InitiateTransactionAsync(Transaction transaction, CancellationToken ct = default)
{
    if (string.IsNullOrEmpty(ResultUrl))
        throw new PesePayException("Result URL has not been specified.");

    if (string.IsNullOrEmpty(ReturnUrl))
        throw new PesePayException("Return URL has not been specified.");

    transaction.ResultUrl = ResultUrl;
    transaction.ReturnUrl = ReturnUrl;

    var payload = _crypto.Encrypt(JsonSerializer.Serialize(transaction, _jsonOptions));

    try
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { payload }, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/v1/payments/initiate", content, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var raw = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var decrypted = _crypto.Decrypt(raw.GetProperty("payload").GetString()!);
        var result = JsonSerializer.Deserialize<InitiateResponse>(decrypted, _jsonOptions)!;

        return PesepayResult<InitiateResponse>.Ok(result);
    }
    catch (HttpRequestException ex)
    {
        throw new PesePayException(ex.Message, ex);
    }
}
```

Also add the `_jsonOptions` field to the class body:

```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

And add the required `using System.Text.Json.Serialization;` to the top of `PesePayClient.cs`.

- [ ] **Step 3: Run the tests**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "PesePayClientApiTests"`
Expected: 3 passed (successful call, missing ResultUrl, missing ReturnUrl).

- [ ] **Step 4: Commit**

```bash
git add PesePayClient.cs PesePay.Tests/PesePayClientApiTests.cs
git commit -m "feat: implement InitiateTransactionAsync with encryption and URL validation"
```

---

### Task 17: PesePayClient — MakeSeamlessPaymentAsync

**Files:**
- Modify: `PesePayClient.cs`
- Modify: `PesePay.Tests/PesePayClientApiTests.cs`

- [ ] **Step 1: Write the failing test**

Append to `PesePay.Tests/PesePayClientApiTests.cs`:

```csharp
[Fact]
public async Task MakeSeamlessPaymentAsync_Sends_Encrypted_Payload()
{
    var handler = new FakeHttpMessageHandler();
    var crypto = new AesCbcPayloadCrypto("test-key-32-chars-long!!!!!!");
    var expectedResponse = new PaymentResponse("REF002", new Uri("https://poll.example.com/REF002"), null, "SUCCESS");
    var responseJson = JsonSerializer.Serialize(expectedResponse, ApiOptions);
    var encryptedResponse = crypto.Encrypt(responseJson);
    var responsePayload = JsonSerializer.Serialize(new { payload = encryptedResponse });

    handler.SetResponse(HttpStatusCode.OK, responsePayload);

    var httpClient = new HttpClient(handler);
    var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox)
    {
        ResultUrl = "https://example.com/result"
    };

    var payment = new Payment(CurrencyCode.ZWL, "ecocash", new Customer("a@b.com", null, null));

    var result = await client.MakeSeamlessPaymentAsync(payment, "Invoice #456", 500m, new Dictionary<string, string> { { "field1", "val1" } });

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
    var crypto = new AesCbcPayloadCrypto("test-key-32-chars-long!!!!!!");
    var httpClient = new HttpClient(handler);
    var client = new PesePayClient(crypto, httpClient, EnvironmentType.Sandbox);

    var payment = new Payment(CurrencyCode.USD, "visa", new Customer("a@b.com", null, null));

    await Assert.ThrowsAsync<PesePayException>(() =>
        client.MakeSeamlessPaymentAsync(payment, "test", 10m));
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "MakeSeamlessPaymentAsync"`
Expected: FAIL — NotImplementedException for the success test.

- [ ] **Step 2: Implement MakeSeamlessPaymentAsync**

Replace the placeholder `MakeSeamlessPaymentAsync` in `PesePayClient.cs` with:

```csharp
public async Task<PesepayResult<PaymentResponse>> MakeSeamlessPaymentAsync(Payment payment, string reason, decimal amount, Dictionary<string, string>? fields = null, CancellationToken ct = default)
{
    if (string.IsNullOrEmpty(ResultUrl))
        throw new PesePayException("Result URL has not been specified.");

    payment.ResultUrl = ResultUrl;
    payment.ReturnUrl = ReturnUrl;
    payment.ReasonForPayment = reason;
    payment.AmountDetails = new Amount(amount, payment.CurrencyCode);
    if (fields != null)
        payment.RequiredFields = fields;

    var payload = _crypto.Encrypt(JsonSerializer.Serialize(payment, _jsonOptions));

    try
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { payload }, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/v2/payments/make-payment", content, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var raw = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var decrypted = _crypto.Decrypt(raw.GetProperty("payload").GetString()!);
        var result = JsonSerializer.Deserialize<PaymentResponse>(decrypted, _jsonOptions)!;

        return PesepayResult<PaymentResponse>.Ok(result);
    }
    catch (HttpRequestException ex)
    {
        throw new PesePayException(ex.Message, ex);
    }
}
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "MakeSeamlessPaymentAsync"`
Expected: 2 passed.

- [ ] **Step 4: Commit**

```bash
git add PesePayClient.cs PesePay.Tests/PesePayClientApiTests.cs
git commit -m "feat: implement MakeSeamlessPaymentAsync"
```

---

### Task 18: PesePayClient — CheckPaymentStatusAsync + PollTransactionAsync

**Files:**
- Modify: `PesePayClient.cs`
- Modify: `PesePay.Tests/PesePayClientApiTests.cs`

- [ ] **Step 1: Write the failing tests**

Append to `PesePay.Tests/PesePayClientApiTests.cs`:

```csharp
[Fact]
public async Task CheckPaymentStatusAsync_Returns_PaymentStatus()
{
    var handler = new FakeHttpMessageHandler();
    var crypto = new AesCbcPayloadCrypto("test-key-32-chars-long!!!!!!");
    var expectedStatus = new PaymentStatus("REF003", new Uri("https://poll.example.com/REF003"), null, "SUCCESS");
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
    var crypto = new AesCbcPayloadCrypto("test-key-32-chars-long!!!!!!");
    var expectedStatus = new PaymentStatus("REF004", new Uri("https://poll.example.com/REF004"), null, "PENDING");
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
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "CheckPaymentStatusAsync|PollTransactionAsync"`
Expected: FAIL — NotImplementedException.

- [ ] **Step 2: Implement CheckPaymentStatusAsync and PollTransactionAsync**

Replace the placeholder methods in `PesePayClient.cs`:

```csharp
public async Task<PesepayResult<PaymentStatus>> CheckPaymentStatusAsync(string referenceNumber, CancellationToken ct = default)
{
    var url = $"/v1/payments/check-payment?referenceNumber={Uri.EscapeDataString(referenceNumber)}";
    return await PollTransactionAsync(new Uri(url, UriKind.Relative), ct);
}

public async Task<PesepayResult<PaymentStatus>> PollTransactionAsync(Uri pollUrl, CancellationToken ct = default)
{
    try
    {
        HttpResponseMessage response;
        if (pollUrl.IsAbsoluteUri)
        {
            using var pollingClient = new HttpClient();
            pollingClient.DefaultRequestHeaders.Add("key", _integrationKey);
            response = await pollingClient.GetAsync(pollUrl, ct);
        }
        else
        {
            response = await _httpClient.GetAsync(pollUrl, ct);
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var raw = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var decrypted = _crypto.Decrypt(raw.GetProperty("payload").GetString()!);
        var result = JsonSerializer.Deserialize<PaymentStatus>(decrypted, _jsonOptions)!;

        return PesepayResult<PaymentStatus>.Ok(result);
    }
    catch (HttpRequestException ex)
    {
        throw new PesePayException(ex.Message, ex);
    }
}
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "CheckPaymentStatusAsync|PollTransactionAsync"`
Expected: 2 passed.

- [ ] **Step 4: Commit**

```bash
git add PesePayClient.cs PesePay.Tests/PesePayClientApiTests.cs
git commit -m "feat: implement CheckPaymentStatusAsync and PollTransactionAsync"
```

---

### Task 19: ServiceCollectionExtensions (DI Registration)

**Files:**
- Create: `ServiceCollectionExtensions.cs`
- Create: `PesePay.Tests/ServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

Write `PesePay.Tests/ServiceCollectionExtensionsTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PesePay.Domain;

namespace PesePay.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPesePay_With_Delegate_Registers_IPesePayClient()
    {
        var services = new ServiceCollection();

        services.AddPesePay(options =>
        {
            options.IntegrationKey = "int-key";
            options.EncryptionKey = "test-encryption-key-32chars!!";
            options.Environment = EnvironmentType.Sandbox;
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IPesePayClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddPesePay_With_IConfiguration_Registers_IPesePayClient()
    {
        var configData = new Dictionary<string, string?>
        {
            { "PesePay:IntegrationKey", "cfg-key" },
            { "PesePay:EncryptionKey", "test-encryption-key-32chars!!" },
            { "PesePay:Environment", "Production" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddPesePay(configuration.GetSection("PesePay"));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IPesePayClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddPesePay_Registers_As_Singleton()
    {
        var services = new ServiceCollection();

        services.AddPesePay(options =>
        {
            options.IntegrationKey = "key";
            options.EncryptionKey = "test-encryption-key-32chars!!";
        });

        var provider = services.BuildServiceProvider();
        var client1 = provider.GetRequiredService<IPesePayClient>();
        var client2 = provider.GetRequiredService<IPesePayClient>();

        Assert.Same(client1, client2);
    }
}
```

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "ServiceCollectionExtensionsTests"`
Expected: FAIL — "AddPesePay does not exist" or "The type or namespace name 'ServiceCollectionExtensions' does not exist"

- [ ] **Step 2: Implement ServiceCollectionExtensions**

Write `ServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PesePay.Domain;

namespace PesePay;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPesePay(this IServiceCollection services, Action<PesePayConfiguration> configure)
    {
        var config = new PesePayConfiguration();
        configure(config);

        var client = new PesePayClient(config.IntegrationKey, config.EncryptionKey, config.Environment)
        {
            ResultUrl = config.ResultUrl,
            ReturnUrl = config.ReturnUrl
        };

        services.AddSingleton<IPesePayClient>(client);
        return services;
    }

    public static IServiceCollection AddPesePay(this IServiceCollection services, IConfiguration configuration)
    {
        var config = new PesePayConfiguration();
        configuration.Bind(config);

        var client = new PesePayClient(config.IntegrationKey, config.EncryptionKey, config.Environment)
        {
            ResultUrl = config.ResultUrl,
            ReturnUrl = config.ReturnUrl
        };

        services.AddSingleton<IPesePayClient>(client);
        return services;
    }
}
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test PesePay.Tests/PesePay.Tests.csproj --filter "ServiceCollectionExtensionsTests"`
Expected: 3 passed.

- [ ] **Step 4: Commit**

```bash
git add ServiceCollectionExtensions.cs PesePay.Tests/ServiceCollectionExtensionsTests.cs
git commit -m "feat: add DI registration extensions for AddPesePay"
```

---

### Task 20: Clean Up and Finalize Existing PesePay.cs

**Files:**
- Modify: `PesePay.cs`

- [ ] **Step 1: Remove the skeleton PesePay class**

The existing `PesePay.cs` contains a skeleton class that is superseded by `PesePayClient.cs`. Replace the content of `PesePay.cs` so it only contains a namespace declaration and forward:

Write `PesePay.cs`:

```csharp
// PesePay library — see PesePayClient for the main entry point.

namespace PesePay;
```

- [ ] **Step 2: Verify the full test suite passes**

Run: `dotnet test`
Expected: All tests pass (should be around 30 tests).

- [ ] **Step 3: Commit**

```bash
git add PesePay.cs
git commit -m "chore: replace skeleton PesePay.cs with namespace stub"
```

---

### Task 21: Final Verification

- [ ] **Step 1: Run all tests**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 2: Build in Release mode**

Run: `dotnet build -c Release`
Expected: Build succeeded with zero warnings.

- [ ] **Step 3: Verify zero NuGet runtime dependencies**

Run: `dotnet list PesePay.csproj package`
Expected: Only `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Configuration.Abstractions` (DI tooling, not runtime dependencies for direct construction).

- [ ] **Step 4: Commit final verification**

```bash
git add -A
git commit -m "chore: final verification — all tests pass, release build clean"
```
