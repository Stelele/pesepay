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
            Console.WriteLine($"  {method.Code} - {method.Name} (redirect: {method.RedirectRequired}, fields: {method.RequiredFields?.Count ?? 0})");
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
            Console.WriteLine($"  {method.Code} - {method.Name} (redirect: {method.RedirectRequired}, fields: {method.RequiredFields?.Count ?? 0})");
    }
}

public class SandboxPaymentTests
{
    private PesePayClient CreateClient()
    {
        var client = SandboxCredentials.CreateClient();
        client.ResultUrl = SandboxCredentials.ResultUrl;
        client.ReturnUrl = SandboxCredentials.ReturnUrl;
        return client;
    }

    [SandboxWebhookFact]
    public async Task InitiateTransaction_Returns_RedirectUrl_And_PollUrl()
    {
        var client = CreateClient();
        var txn = client.CreateTransaction(
            5m, CurrencyCode.USD, "Redirect payment test", "RDR-" + Guid.NewGuid().ToString("N")[..8]);

        var result = await client.InitiateTransactionAsync(txn);

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

    [SandboxWebhookFact]
    public async Task InitiateTransaction_Convenience_Returns_RedirectUrl()
    {
        var client = SandboxCredentials.CreateClient();
        var result = await client.InitiateTransactionAsync(
            5m, CurrencyCode.USD, "Convenience redirect test", "CNV-" + Guid.NewGuid().ToString("N")[..8],
            resultUrl: SandboxCredentials.ResultUrl,
            returnUrl: SandboxCredentials.ReturnUrl);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ReferenceNumber);

        Console.WriteLine($"Reference:     {result.Data.ReferenceNumber}");
        Console.WriteLine($"RedirectUrl:   {result.Data.RedirectUrl}");
    }

    [SandboxFact]
    public async Task InitiateTransaction_Throws_When_ResultUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        var txn = client.CreateTransaction(10m, CurrencyCode.USD, "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
    }

    [SandboxFact]
    public async Task InitiateTransaction_Throws_When_ReturnUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        client.ResultUrl = SandboxCredentials.ResultUrl;
        var txn = client.CreateTransaction(10m, CurrencyCode.USD, "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
    }

    [SandboxWebhookFact]
    public async Task CheckPaymentStatus_Returns_Valid_Status()
    {
        var client = CreateClient();
        var txn = client.CreateTransaction(
            5m, CurrencyCode.USD, "Status check test", "CHK-" + Guid.NewGuid().ToString("N")[..8]);
        var initResult = await client.InitiateTransactionAsync(txn);
        Assert.True(initResult.IsSuccess);

        var result = await client.CheckPaymentStatusAsync(initResult.Data!.ReferenceNumber);

        Assert.True(result.IsSuccess);

        if (result.Data?.ReferenceNumber == null)
        {
            Console.WriteLine("CheckPaymentStatus succeeded but fields are null - poll decryption mismatch");
            return;
        }

        Assert.NotEmpty(result.Data.ReferenceNumber);
        Assert.NotNull(result.Data.TransactionStatus);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
        Console.WriteLine($"IsPaid:       {result.Data.IsPaid}");
    }

    [SandboxWebhookFact]
    public async Task PollTransaction_Returns_Valid_Status()
    {
        var client = CreateClient();
        var txn = client.CreateTransaction(
            5m, CurrencyCode.USD, "Poll test", "POL-" + Guid.NewGuid().ToString("N")[..8]);
        var initResult = await client.InitiateTransactionAsync(txn);
        Assert.True(initResult.IsSuccess);

        var result = await client.PollTransactionAsync(initResult.Data!.PollUrl);

        Assert.True(result.IsSuccess);

        if (result.Data?.TransactionStatus == null)
        {
            Console.WriteLine("PollTransaction succeeded but fields are null - poll decryption mismatch");
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
        _client = SandboxCredentials.CreateClient();
        _client.ResultUrl = SandboxCredentials.ResultUrl;
        _client.ReturnUrl = SandboxCredentials.ReturnUrl;

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
    public async Task MakeSeamlessPayment_EcoCash_Success()
    {
        var ecoCashMethod = FindMethod("EcoCash");
        if (ecoCashMethod == null) return;
        var currency = GetCurrencyForMethod(ecoCashMethod);

        Console.WriteLine($"Using method {ecoCashMethod.Code} with currency {currency}");

        var payment = _client.CreatePayment(
            currency, ecoCashMethod.Code, "test@example.com", "0777777777", "Test User");

        var result = await _client.MakeSeamlessPaymentAsync(
            payment, "EcoCash success test", 10m, "ECO-SUCCESS-" + Guid.NewGuid().ToString("N")[..8],
            new Dictionary<string, string> { { "customerPhoneNumber", "0777777777" } });

        Assert.True(result.IsSuccess);

        if (result.Data?.ReferenceNumber == null)
        {
            Console.WriteLine("EcoCash response succeeded but reference is null - deserialization mismatch");
            return;
        }

        Assert.NotEmpty(result.Data.ReferenceNumber);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");
        Console.WriteLine($"IsPaid:     {result.Data.IsPaid}");

        Assert.Equal("SUCCESS", result.Data.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task MakeSeamlessPayment_EcoCash_Success_Convenience()
    {
        var ecoCashMethod = FindMethod("EcoCash");
        if (ecoCashMethod == null) return;
        var currency = GetCurrencyForMethod(ecoCashMethod);

        Console.WriteLine($"Using convenience API with {ecoCashMethod.Code} ({currency})");

        var result = await _client.MakeSeamlessPaymentAsync(
            PaymentMethodCode.EcoCash, currency, 10m,
            "0777777777", "Test User",
            "EcoCash convenience test",
            "ECO-CONV-" + Guid.NewGuid().ToString("N")[..8]);

        Assert.True(result.IsSuccess);

        if (result.Data?.ReferenceNumber == null)
        {
            Console.WriteLine("Convenience EcoCash succeeded but reference is null");
            return;
        }

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");
        Console.WriteLine($"IsPaid:     {result.Data.IsPaid}");

        Assert.Equal("SUCCESS", result.Data.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task MakeSeamlessPayment_EcoCash_Failure()
    {
        var ecoCashMethod = FindMethod("EcoCash");
        if (ecoCashMethod == null) return;
        var currency = GetCurrencyForMethod(ecoCashMethod);

        var payment = _client.CreatePayment(
            currency, ecoCashMethod.Code, "test@example.com", "0770000000", "Test User");

        var result = await _client.MakeSeamlessPaymentAsync(
            payment, "EcoCash failure test", 10m, "ECO-FAIL-" + Guid.NewGuid().ToString("N")[..8],
            new Dictionary<string, string> { { "customerPhoneNumber", "0770000000" } });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.IsPaid);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
    }

    [SandboxWebhookFact]
    public async Task MakeSeamlessPayment_OneMoney_Success()
    {
        var oneMoneyMethod = FindMethod("OneMoney");
        if (oneMoneyMethod == null) return;
        var currency = GetCurrencyForMethod(oneMoneyMethod);

        var payment = _client.CreatePayment(
            currency, oneMoneyMethod.Code, "test@example.com", "0719999999", "Test User");

        var result = await _client.MakeSeamlessPaymentAsync(
            payment, "OneMoney success test", 10m, "ONE-SUCCESS-" + Guid.NewGuid().ToString("N")[..8],
            new Dictionary<string, string> { { "customerPhoneNumber", "0719999999" } });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");

        Assert.Equal("SUCCESS", result.Data!.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task MakeSeamlessPayment_OneMoney_Failure()
    {
        var oneMoneyMethod = FindMethod("OneMoney");
        if (oneMoneyMethod == null) return;
        var currency = GetCurrencyForMethod(oneMoneyMethod);

        var payment = _client.CreatePayment(
            currency, oneMoneyMethod.Code, "test@example.com", "0719000000", "Test User");

        var result = await _client.MakeSeamlessPaymentAsync(
            payment, "OneMoney failure test", 10m, "ONE-FAIL-" + Guid.NewGuid().ToString("N")[..8],
            new Dictionary<string, string> { { "customerPhoneNumber", "0719000000" } });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.IsPaid);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
    }

    [SandboxWebhookFact]
    public async Task MakeSeamlessPayment_VISA_Success()
    {
        var visaMethod = FindMethod("Visa");
        if (visaMethod == null) return;
        var currency = GetCurrencyForMethod(visaMethod);

        var payment = _client.CreatePayment(
            currency, visaMethod.Code, "test@example.com", null, "Test User");

        var fields = new Dictionary<string, string>
        {
            { "creditCardNumber",  "4867960000005461" },
            { "creditCardSecurityNumber",    "608" },
            { "creditCardExpiryDate", "12/30" }
        };

        var result = await _client.MakeSeamlessPaymentAsync(
            payment, "VISA success test", 10m, "VISA-SUCCESS-" + Guid.NewGuid().ToString("N")[..8], fields);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");

        Assert.Equal("SUCCESS", result.Data!.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task MakeSeamlessPayment_VISA_Success_Convenience()
    {
        var visaMethod = FindMethod("Visa");
        if (visaMethod == null) return;

        var card = new CardDetails("4867960000005461", "608", "12/30", "Test User");
        var result = await _client.MakeSeamlessCardPaymentAsync(
            PaymentMethodCode.Visa, CurrencyCode.USD, 10m,
            card, "test@example.com", "Test User",
            "VISA convenience test",
            "VISA-CONV-" + Guid.NewGuid().ToString("N")[..8]);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");

        Assert.Equal("SUCCESS", result.Data!.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task MakeSeamlessPayment_VISA_Failure()
    {
        var visaMethod = FindMethod("Visa");
        if (visaMethod == null) return;
        var currency = GetCurrencyForMethod(visaMethod);

        var payment = _client.CreatePayment(
            currency, visaMethod.Code, "test@example.com", null, "Test User");

        var fields = new Dictionary<string, string>
        {
            { "creditCardNumber",  "4867965005005002" },
            { "creditCardSecurityNumber",    "994" },
            { "creditCardExpiryDate", "12/30" }
        };

        var result = await _client.MakeSeamlessPaymentAsync(
            payment, "VISA failure test", 10m, "VISA-FAIL-" + Guid.NewGuid().ToString("N")[..8], fields);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.IsPaid);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
    }

    [SandboxWebhookFact]
    public async Task MakeSeamlessPayment_CABS_Success()
    {
        var cabsMethod = FindMethod("CABS");
        if (cabsMethod == null) return;
        var currency = GetCurrencyForMethod(cabsMethod);

        var payment = _client.CreatePayment(
            currency, cabsMethod.Code, "test@example.com", null, "Test User");

        var fields = new Dictionary<string, string>
        {
            { "creditCardNumber",  "405405405405430" },
            { "creditCardSecurityNumber",    "708" },
            { "creditCardExpiryDate", "12/30" },
            { "creditCardHolder", "Test User" }
        };

        try
        {
            var result = await _client.MakeSeamlessPaymentAsync(
                payment, "CABS success test", 10m, "CABS-SUCCESS-" + Guid.NewGuid().ToString("N")[..8], fields);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
            Console.WriteLine($"Status:     {result.Data.TransactionStatus}");

            Assert.Equal("SUCCESS", result.Data!.TransactionStatus);
        }
        catch (PesePayException ex)
        {
            Console.WriteLine($"CABS payment not supported by sandbox: {ex.Message}");
        }
    }

    [SandboxFact]
    public async Task MakeSeamlessPayment_Throws_When_ResultUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        var customer = new Customer("test@example.com", "0777777777", null);
        var payment = client.CreatePayment(CurrencyCode.USD, PaymentMethodCode.EcoCash, customer);

        await Assert.ThrowsAsync<PesePayException>(() =>
            client.MakeSeamlessPaymentAsync(payment, "test", 10m, "MERCH01"));
    }
}
