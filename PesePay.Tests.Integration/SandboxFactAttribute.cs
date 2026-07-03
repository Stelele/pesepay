namespace PesePay.Tests.Integration;

public class SandboxFactAttribute : FactAttribute
{
    public SandboxFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_INTEGRATION_KEY")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_ENCRYPTION_KEY")))
        {
            Skip = "Sandbox credentials not configured. Set PESEPAY_SANDBOX_INTEGRATION_KEY and PESEPAY_SANDBOX_ENCRYPTION_KEY env vars.";
        }
    }
}

public class SandboxWebhookFactAttribute : FactAttribute
{
    public SandboxWebhookFactAttribute() : this(
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_INTEGRATION_KEY"),
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_ENCRYPTION_KEY"),
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_RESULT_URL"),
        Environment.GetEnvironmentVariable("PESEPAY_SANDBOX_RETURN_URL"))
    { }

    internal SandboxWebhookFactAttribute(
        string? integrationKey,
        string? encryptionKey,
        string? resultUrl,
        string? returnUrl)
    {
        if (string.IsNullOrEmpty(integrationKey) || string.IsNullOrEmpty(encryptionKey))
        {
            Skip = "Sandbox credentials not configured.";
        }
        else if (string.IsNullOrEmpty(resultUrl) || string.IsNullOrEmpty(returnUrl))
        {
            Skip = "Webhook URLs not configured. Set PESEPAY_SANDBOX_RESULT_URL and PESEPAY_SANDBOX_RETURN_URL env vars (e.g. webhook.site URLs).";
        }
    }
}
