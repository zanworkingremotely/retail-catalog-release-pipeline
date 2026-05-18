using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Scheduling.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductRolloutScheduling(this IServiceCollection services)
    {
        services.AddHostedService<ProductRolloutWorker>();
        return services;
    }
}
