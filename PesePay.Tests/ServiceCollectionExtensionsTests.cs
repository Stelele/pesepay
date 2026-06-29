using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PesePay.Domain;

namespace PesePay.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPesePay_With_Delegate_Registers_IPesePayClient()
    {
        var services = new ServiceCollection();

        services.AddPesePay(options =>
        {
            options.IntegrationKey = "int-key";
            options.EncryptionKey = "0123456789abcdef0123456789abcdef";
            options.Environment = EnvironmentType.Sandbox;
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IPesePayClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddPesePay_With_IConfiguration_Registers_IPesePayClient()
    {
        var configData = new Dictionary<string, string?>
        {
            { "PesePay:IntegrationKey", "cfg-key" },
            { "PesePay:EncryptionKey", "0123456789abcdef0123456789abcdef" },
            { "PesePay:Environment", "Production" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddPesePay(configuration.GetSection("PesePay"));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IPesePayClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddPesePay_Registers_As_Singleton()
    {
        var services = new ServiceCollection();

        services.AddPesePay(options =>
        {
            options.IntegrationKey = "key";
            options.EncryptionKey = "0123456789abcdef0123456789abcdef";
        });

        var provider = services.BuildServiceProvider();
        var client1 = provider.GetRequiredService<IPesePayClient>();
        var client2 = provider.GetRequiredService<IPesePayClient>();

        Assert.Same(client1, client2);
    }
}
