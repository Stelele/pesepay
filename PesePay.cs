using PesePay.Domain;

namespace PesePay;

public class PesePay(
    string integrationKey,
    string encryptionKey, 
    EnvironmentType environment = EnvironmentType.Sandbox)
{
    public string IntegrationKey { get; } = integrationKey; 
    public string EncryptionKey { get; } = encryptionKey;
    public EnvironmentType Environment { get; } = environment; 
    
    public string? ResultUrl { get; set; }
    public string? ReturnUrl { get; set; }

}
