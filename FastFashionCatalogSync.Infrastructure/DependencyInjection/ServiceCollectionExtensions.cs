using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Infrastructure.Catalog;
using FastFashionCatalogSync.Infrastructure.Clocks;
using FastFashionCatalogSync.Infrastructure.Releases;
using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogReleaseInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryRetailCatalogContext>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMerchandisingCatalogReader, MerchandisingCatalogReader>();
        services.AddSingleton<OperationalCatalogGateway>();
        services.AddSingleton<IOperationalCatalogReader>(provider => provider.GetRequiredService<OperationalCatalogGateway>());
        services.AddSingleton<IOperationalCatalogPublisher>(provider => provider.GetRequiredService<OperationalCatalogGateway>());
        services.AddSingleton<ICatalogReleaseLedger, InMemoryCatalogReleaseLedger>();

        return services;
    }
}
