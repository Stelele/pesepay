namespace PesePay.Domain;

/// <summary>
/// PesePay API environment. Determines the base URL for API calls.
/// </summary>
public enum EnvironmentType
{
    /// <summary>Sandbox/test environment (https://api.test.sandbox.pesepay.com/payments-engine).</summary>
    Sandbox,

    /// <summary>Production/live environment (https://api.pesepay.com/api/payments-engine).</summary>
    Production
}
