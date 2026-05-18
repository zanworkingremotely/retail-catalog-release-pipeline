using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Infrastructure.Catalog;
using FastFashionCatalogSync.Infrastructure.Clocks;
using FastFashionCatalogSync.Infrastructure.Rollouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductRolloutInfrastructure(this IServiceCollection services)
    {
        services.AddLocalProductRolloutInfrastructure();
        return services;
    }

    public static IServiceCollection AddProductRolloutInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<LocalRetailCatalogContext>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMerchandisingCatalogReader, MerchandisingCatalogReader>();
        services.AddSingleton<OperationalCatalogGateway>();
        services.AddSingleton<IOperationalCatalogReader>(provider => provider.GetRequiredService<OperationalCatalogGateway>());
        services.AddSingleton<IOperationalCatalogPublisher>(provider => provider.GetRequiredService<OperationalCatalogGateway>());

        var rolloutControlConnectionString = configuration.GetConnectionString("RolloutControl");
        if (string.IsNullOrWhiteSpace(rolloutControlConnectionString))
        {
            services.AddSingleton<IProductRolloutLedger, LocalProductRolloutLedger>();
        }
        else
        {
            services.AddSingleton<IProductRolloutLedger>(_ => new SqlProductRolloutLedger(rolloutControlConnectionString));
        }

        return services;
    }

    private static IServiceCollection AddLocalProductRolloutInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<LocalRetailCatalogContext>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMerchandisingCatalogReader, MerchandisingCatalogReader>();
        services.AddSingleton<OperationalCatalogGateway>();
        services.AddSingleton<IOperationalCatalogReader>(provider => provider.GetRequiredService<OperationalCatalogGateway>());
        services.AddSingleton<IOperationalCatalogPublisher>(provider => provider.GetRequiredService<OperationalCatalogGateway>());
        services.AddSingleton<IProductRolloutLedger, LocalProductRolloutLedger>();

        return services;
    }
}
