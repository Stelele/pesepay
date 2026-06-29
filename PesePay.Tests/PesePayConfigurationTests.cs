using PesePay.Domain;

namespace PesePay.Tests;

public class PesePayConfigurationTests
{
    [Fact]
    public void Default_Environment_Is_Sandbox()
    {
        var config = new PesePayConfiguration
        {
            IntegrationKey = "key",
            EncryptionKey = "enc"
        };

        Assert.Equal(EnvironmentType.Sandbox, config.Environment);
    }

    [Fact]
    public void Properties_Are_Settable()
    {
        var config = new PesePayConfiguration
        {
            IntegrationKey = "int-key",
            EncryptionKey = "enc-key",
            Environment = EnvironmentType.Production,
            ResultUrl = "https://example.com/result",
            ReturnUrl = "https://example.com/return"
        };

        Assert.Equal("int-key", config.IntegrationKey);
        Assert.Equal("enc-key", config.EncryptionKey);
        Assert.Equal(EnvironmentType.Production, config.Environment);
        Assert.Equal("https://example.com/result", config.ResultUrl);
        Assert.Equal("https://example.com/return", config.ReturnUrl);
    }
}
