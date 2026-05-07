using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Scheduling.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogReleaseScheduling(this IServiceCollection services)
    {
        services.AddHostedService<CatalogReleaseWorker>();
        return services;
    }
}
