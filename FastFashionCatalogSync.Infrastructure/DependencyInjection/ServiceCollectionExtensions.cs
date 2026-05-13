using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Infrastructure.Catalog;
using FastFashionCatalogSync.Infrastructure.Clocks;
using FastFashionCatalogSync.Infrastructure.Releases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogReleaseInfrastructure(this IServiceCollection services)
    {
        services.AddLocalCatalogReleaseInfrastructure();
        return services;
    }

    public static IServiceCollection AddCatalogReleaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<LocalRetailCatalogContext>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMerchandisingCatalogReader, MerchandisingCatalogReader>();
        services.AddSingleton<OperationalCatalogGateway>();
        services.AddSingleton<IOperationalCatalogReader>(provider => provider.GetRequiredService<OperationalCatalogGateway>());
        services.AddSingleton<IOperationalCatalogPublisher>(provider => provider.GetRequiredService<OperationalCatalogGateway>());

        var releaseControlConnectionString = configuration.GetConnectionString("ReleaseControl");
        if (string.IsNullOrWhiteSpace(releaseControlConnectionString))
        {
            services.AddSingleton<ICatalogReleaseLedger, LocalCatalogReleaseLedger>();
        }
        else
        {
            services.AddSingleton<ICatalogReleaseLedger>(_ => new SqlCatalogReleaseLedger(releaseControlConnectionString));
        }

        return services;
    }

    private static IServiceCollection AddLocalCatalogReleaseInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<LocalRetailCatalogContext>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMerchandisingCatalogReader, MerchandisingCatalogReader>();
        services.AddSingleton<OperationalCatalogGateway>();
        services.AddSingleton<IOperationalCatalogReader>(provider => provider.GetRequiredService<OperationalCatalogGateway>());
        services.AddSingleton<IOperationalCatalogPublisher>(provider => provider.GetRequiredService<OperationalCatalogGateway>());
        services.AddSingleton<ICatalogReleaseLedger, LocalCatalogReleaseLedger>();

        return services;
    }
}
