using PesePay.Domain;

namespace PesePay;

/// <summary>
/// Configuration options for the PesePay client.
/// </summary>
public class PesePayConfiguration
{
    /// <summary>
    /// The Pesepay integration key provided by your Pesepay account.
    /// </summary>
    public string IntegrationKey { get; set; } = string.Empty;

    /// <summary>
    /// The Pesepay encryption key provided by your Pesepay account.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// The PesePay environment to use. Defaults to <see cref="EnvironmentType.Sandbox"/>.
    /// </summary>
    public EnvironmentType Environment { get; set; } = EnvironmentType.Sandbox;

    /// <summary>
    /// The URL PesePay will POST the payment result to (optional, can be set on the client).
    /// </summary>
    public string? ResultUrl { get; set; }

    /// <summary>
    /// The URL the customer will be redirected back to after payment completion (optional).
    /// </summary>
    public string? ReturnUrl { get; set; }
}
