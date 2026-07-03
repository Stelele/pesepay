using PesePay.Domain;

namespace PesePay.Tests.Integration;

public static class SandboxCredentials
{
    public static string IntegrationKey =>
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_INTEGRATION_KEY")!;

    public static string EncryptionKey =>
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_ENCRYPTION_KEY")!;

    public static string ResultUrl =>
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_RESULT_URL")!;

    public static string ReturnUrl =>
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_RETURN_URL")!;

    public static PesePayClient CreateClient()
    {
        return new PesePayClient(IntegrationKey, EncryptionKey, EnvironmentType.Sandbox);
    }

    public static void PrintConfigurationBanner()
    {
        Console.WriteLine("=== PesePay Sandbox Integration Tests ===");
        Console.WriteLine($"Integration Key: {(string.IsNullOrEmpty(IntegrationKey) ? "✗ missing" : "✓ configured")}");
        Console.WriteLine($"Encryption Key:  {(string.IsNullOrEmpty(EncryptionKey) ? "✗ missing" : "✓ configured")}");
        Console.WriteLine($"Result URL:     {(string.IsNullOrEmpty(ResultUrl) ? "✗ missing" : "✓ configured")}");
        Console.WriteLine($"Return URL:     {(string.IsNullOrEmpty(ReturnUrl) ? "✗ missing" : "✓ configured")}");
        Console.WriteLine("Sandbox API:    https://api.test.sandbox.pesepay.com/payments-engine");
        Console.WriteLine("=========================================");
    }
}
