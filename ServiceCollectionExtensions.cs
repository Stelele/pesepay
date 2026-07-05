using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PesePay.Domain;

namespace PesePay;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPesePay(this IServiceCollection services, Action<PesePayConfiguration> configure)
    {
        var config = new PesePayConfiguration();
        configure(config);

        var client = new PesePayClient(config.IntegrationKey, config.EncryptionKey, config.Environment, config.ResultUrl, config.ReturnUrl);

        services.AddSingleton<IPesePayClient>(client);
        return services;
    }

    public static IServiceCollection AddPesePay(this IServiceCollection services, IConfiguration configuration)
    {
        var config = new PesePayConfiguration();
        configuration.Bind(config);

        var client = new PesePayClient(config.IntegrationKey, config.EncryptionKey, config.Environment, config.ResultUrl, config.ReturnUrl);

        services.AddSingleton<IPesePayClient>(client);
        return services;
    }
}
