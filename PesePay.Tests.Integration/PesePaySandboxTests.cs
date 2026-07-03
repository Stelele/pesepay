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
    public async Task GetPaymentMethods_ZWL_Returns_Mobile_Money_Methods()
    {
        var client = SandboxCredentials.CreateClient();
        var result = await client.GetPaymentMethodsAsync("ZWL");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Payment methods for ZWL ({result.Data!.Count}):");
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
            5m, CurrencyCode.ZWL, "Redirect payment test", "RDR-" + Guid.NewGuid().ToString("N")[..8]);

        var result = await client.InitiateTransactionAsync(txn);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ReferenceNumber);
        Assert.NotNull(result.Data.RedirectUrl);
        Assert.NotNull(result.Data.PollUrl);
        Assert.NotNull(result.Data.InternalReference);
        Assert.NotNull(result.Data.TransactionStatus);

        Console.WriteLine($"Reference:   {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:      {result.Data.TransactionStatus}");
        Console.WriteLine($"Redirect URL: {result.Data.RedirectUrl}");
        Console.WriteLine($"Poll URL:     {result.Data.PollUrl}");
    }

    [SandboxFact]
    public async Task InitiateTransaction_Throws_When_ResultUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        var txn = client.CreateTransaction(10m, CurrencyCode.ZWL, "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
    }

    [SandboxFact]
    public async Task InitiateTransaction_Throws_When_ReturnUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        client.ResultUrl = SandboxCredentials.ResultUrl;
        var txn = client.CreateTransaction(10m, CurrencyCode.ZWL, "Test");

        await Assert.ThrowsAsync<PesePayException>(() => client.InitiateTransactionAsync(txn));
    }

    [SandboxWebhookFact]
    public async Task CheckPaymentStatus_Returns_Valid_Status()
    {
        var client = CreateClient();
        var txn = client.CreateTransaction(
            5m, CurrencyCode.ZWL, "Status check test", "CHK-" + Guid.NewGuid().ToString("N")[..8]);
        var initResult = await client.InitiateTransactionAsync(txn);
        Assert.True(initResult.IsSuccess);

        var result = await client.CheckPaymentStatusAsync(initResult.Data!.ReferenceNumber);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.ReferenceNumber);
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
            5m, CurrencyCode.ZWL, "Poll test", "POL-" + Guid.NewGuid().ToString("N")[..8]);
        var initResult = await client.InitiateTransactionAsync(txn);
        Assert.True(initResult.IsSuccess);

        var result = await client.PollTransactionAsync(initResult.Data!.PollUrl);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.TransactionStatus);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"IsPaid:       {result.Data.IsPaid}");
    }
}

public class SandboxSeamlessPaymentTests : IAsyncLifetime
{
    private PesePayClient _client = null!;
    private List<PaymentMethodInfo> _zwlMethods = new();
    private List<PaymentMethodInfo> _usdMethods = new();

    public async Task InitializeAsync()
    {
        _client = SandboxCredentials.CreateClient();
        _client.ResultUrl = SandboxCredentials.ResultUrl;
        _client.ReturnUrl = SandboxCredentials.ReturnUrl;

        var zwlResult = await _client.GetPaymentMethodsAsync("ZWL");
        if (zwlResult.IsSuccess) _zwlMethods = zwlResult.Data!;

        var usdResult = await _client.GetPaymentMethodsAsync("USD");
        if (usdResult.IsSuccess) _usdMethods = usdResult.Data!;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private PaymentMethodInfo? FindMethod(string nameKeyword)
    {
        return _zwlMethods?.FirstOrDefault(m =>
            m.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase) ||
            m.Description.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase) ||
            m.Code.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase))
            ??
            _usdMethods?.FirstOrDefault(m =>
                m.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase) ||
                m.Code.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase));
    }

    private CurrencyCode GetCurrencyForMethod(PaymentMethodInfo method)
    {
        if (_zwlMethods?.Contains(method) == true) return CurrencyCode.ZWL;
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
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ReferenceNumber);

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
            { "cardNumber",  "4867960000005461" },
            { "cvv",         "608" },
            { "expiryMonth", "12" },
            { "expiryYear",  "2030" }
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
    public async Task MakeSeamlessPayment_VISA_Failure()
    {
        var visaMethod = FindMethod("Visa");
        if (visaMethod == null) return;
        var currency = GetCurrencyForMethod(visaMethod);

        var payment = _client.CreatePayment(
            currency, visaMethod.Code, "test@example.com", null, "Test User");

        var fields = new Dictionary<string, string>
        {
            { "cardNumber",  "4867965005005002" },
            { "cvv",         "994" },
            { "expiryMonth", "12" },
            { "expiryYear",  "2030" }
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
            { "cardNumber",  "405405405405430" },
            { "cvv",         "708" },
            { "expiryMonth", "12" },
            { "expiryYear",  "2030" }
        };

        var result = await _client.MakeSeamlessPaymentAsync(
            payment, "CABS success test", 10m, "CABS-SUCCESS-" + Guid.NewGuid().ToString("N")[..8], fields);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");

        Assert.Equal("SUCCESS", result.Data!.TransactionStatus);
    }

    [SandboxFact]
    public async Task MakeSeamlessPayment_Throws_When_ResultUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        var payment = client.CreatePayment(CurrencyCode.ZWL, "PZW211", "test@example.com", "0777777777", null);

        await Assert.ThrowsAsync<PesePayException>(() =>
            client.MakeSeamlessPaymentAsync(payment, "test", 10m, "MERCH01"));
    }
}
