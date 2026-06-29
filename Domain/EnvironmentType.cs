namespace PesePay.Domain;

/// <summary>
/// PesePay API environment. Determines the base URL for API calls.
/// </summary>
public enum EnvironmentType
{
    /// <summary>Sandbox/test environment.</summary>
    Sandbox,

    /// <summary>Production/live environment.</summary>
    Production
}
