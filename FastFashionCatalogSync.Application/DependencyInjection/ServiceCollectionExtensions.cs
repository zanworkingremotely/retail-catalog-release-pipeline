using FastFashionCatalogSync.Application.Releases;
using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogReleaseApplication(this IServiceCollection services)
    {
        services.AddScoped<CatalogReleasePreviewBuilder>();
        services.AddScoped<CatalogReleaseOrchestrator>();

        return services;
    }
}
