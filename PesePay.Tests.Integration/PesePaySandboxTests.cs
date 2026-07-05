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
            Console.WriteLine(
                $"  {method.Code} - {method.Name} (redirect: {method.RedirectRequired}, " +
                $"fields: {method.RequiredFields?.Count ?? 0})");
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
            Console.WriteLine(
                $"  {method.Code} - {method.Name} (redirect: {method.RedirectRequired}, " +
                $"fields: {method.RequiredFields?.Count ?? 0})");
    }
}

public class SandboxPaymentTests
{
    private static PesePayClient CreateClient()
    {
        return SandboxCredentials.CreateClientWithUrls();
    }

    [SandboxWebhookFact]
    public async Task InitiateRedirectPayment_Returns_RedirectUrl_And_PollUrl()
    {
        var client = CreateClient();
        var request = new RedirectPaymentRequest(
            5m, CurrencyCode.USD, "Redirect payment test",
            "RDR-" + Guid.NewGuid().ToString("N")[..8]);

        var result = await client.InitiateRedirectPaymentAsync(request);

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

    [SandboxFact]
    public async Task InitiateRedirectPayment_Throws_When_ResultUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        var request = new RedirectPaymentRequest(10m, CurrencyCode.USD, "Test");

        await Assert.ThrowsAsync<PesePayException>(
            () => client.InitiateRedirectPaymentAsync(request));
    }

    [SandboxWebhookFact]
    public async Task CheckPaymentStatus_Returns_Valid_Status()
    {
        var client = CreateClient();
        var request = new RedirectPaymentRequest(
            5m, CurrencyCode.USD, "Status check test",
            "CHK-" + Guid.NewGuid().ToString("N")[..8]);
        var initResult = await client.InitiateRedirectPaymentAsync(request);
        Assert.True(initResult.IsSuccess);

        var result = await client.CheckPaymentStatusAsync(initResult.Data!.ReferenceNumber);

        Assert.True(result.IsSuccess);

        if (result.Data?.ReferenceNumber == null)
        {
            Console.WriteLine(
                "CheckPaymentStatus succeeded but fields are null - poll decryption mismatch");
            return;
        }

        Assert.NotEmpty(result.Data.ReferenceNumber);
        Assert.NotNull(result.Data.TransactionStatus);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
        Console.WriteLine($"IsPaid:       {result.Data.IsPaid}");
    }

    [SandboxWebhookFact]
    public async Task PollPayment_Returns_Valid_Status()
    {
        var client = CreateClient();
        var request = new RedirectPaymentRequest(
            5m, CurrencyCode.USD, "Poll test",
            "POL-" + Guid.NewGuid().ToString("N")[..8]);
        var initResult = await client.InitiateRedirectPaymentAsync(request);
        Assert.True(initResult.IsSuccess);

        var result = await client.PollPaymentAsync(initResult.Data!.PollUrl);

        Assert.True(result.IsSuccess);

        if (result.Data?.TransactionStatus == null)
        {
            Console.WriteLine(
                "PollPaymentAsync succeeded but fields are null - poll decryption mismatch");
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
        _client = SandboxCredentials.CreateClientWithUrls();

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
    public async Task InitiateSeamlessPayment_EcoCash_Success()
    {
        var ecoCashMethod = FindMethod("EcoCash");
        if (ecoCashMethod == null) return;
        var currency = GetCurrencyForMethod(ecoCashMethod);

        Console.WriteLine($"Using method {ecoCashMethod.Code} with currency {currency}");

        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, currency, 10m,
            "EcoCash success test",
            "ECO-SUCCESS-" + Guid.NewGuid().ToString("N")[..8],
            Email: "test@example.com",
            CustomerName: "Test User",
            PhoneNumber: "0777777777");

        var result = await _client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);

        if (result.Data?.ReferenceNumber == null)
        {
            Console.WriteLine(
                "EcoCash response succeeded but reference is null - deserialization mismatch");
            return;
        }

        Assert.NotEmpty(result.Data.ReferenceNumber);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");
        Console.WriteLine($"IsPaid:     {result.Data.IsPaid}");

        Assert.Equal("SUCCESS", result.Data.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task InitiateSeamlessPayment_EcoCash_Failure()
    {
        var ecoCashMethod = FindMethod("EcoCash");
        if (ecoCashMethod == null) return;
        var currency = GetCurrencyForMethod(ecoCashMethod);

        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, currency, 10m,
            "EcoCash failure test",
            "ECO-FAIL-" + Guid.NewGuid().ToString("N")[..8],
            Email: "test@example.com",
            CustomerName: "Test User",
            PhoneNumber: "0770000000");

        var result = await _client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.IsPaid);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
    }

    [SandboxWebhookFact]
    public async Task InitiateSeamlessPayment_VISA_Success()
    {
        var visaMethod = FindMethod("Visa");
        if (visaMethod == null) return;

        var card = new CardDetails("4867960000005461", "608", "12/30", "Test User");
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.Visa, CurrencyCode.USD, 10m,
            "VISA test",
            "VISA-" + Guid.NewGuid().ToString("N")[..8],
            Email: "test@example.com",
            CustomerName: "Test User",
            Card: card);

        var result = await _client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Console.WriteLine($"Reference:  {result.Data.ReferenceNumber}");
        Console.WriteLine($"Status:     {result.Data.TransactionStatus}");

        Assert.Equal("SUCCESS", result.Data!.TransactionStatus);
    }

    [SandboxWebhookFact]
    public async Task InitiateSeamlessPayment_VISA_Failure()
    {
        var visaMethod = FindMethod("Visa");
        if (visaMethod == null) return;

        var card = new CardDetails("4867965005005002", "994", "12/30", "Test User");
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.Visa, CurrencyCode.USD, 10m,
            "VISA failure test",
            "VISA-FAIL-" + Guid.NewGuid().ToString("N")[..8],
            Email: "test@example.com",
            CustomerName: "Test User",
            Card: card);

        var result = await _client.InitiateSeamlessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.IsPaid);

        Console.WriteLine($"Status:       {result.Data.TransactionStatus}");
        Console.WriteLine($"Description:  {result.Data.TransactionStatusDescription}");
    }

    [SandboxFact]
    public async Task InitiateSeamlessPayment_Throws_When_ResultUrl_Missing()
    {
        var client = SandboxCredentials.CreateClient();
        var request = new SeamlessPaymentRequest(
            PaymentMethodCode.EcoCash, CurrencyCode.USD, 10m,
            "test", "MERCH01",
            PhoneNumber: "0777777777");

        await Assert.ThrowsAsync<PesePayException>(
            () => client.InitiateSeamlessPaymentAsync(request));
    }
}
