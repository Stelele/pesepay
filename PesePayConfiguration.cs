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
