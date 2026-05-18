using FastFashionCatalogSync.Application.Rollouts;
using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductRolloutApplication(this IServiceCollection services)
    {
        services.AddScoped<ProductRolloutPreviewBuilder>();
        services.AddScoped<ProductRolloutWorkflow>();

        return services;
    }
}
