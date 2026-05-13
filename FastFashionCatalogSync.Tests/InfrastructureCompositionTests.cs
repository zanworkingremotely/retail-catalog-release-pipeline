using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Infrastructure.DependencyInjection;
using FastFashionCatalogSync.Infrastructure.Releases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Tests;

public sealed class InfrastructureCompositionTests
{
    [Fact]
    public void Uses_sql_release_ledger_when_release_control_connection_string_is_configured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReleaseControl"] = "Server=localhost,14333;Database=RetailReleaseControlDb;User Id=sa;Password=RetailCatalog!2026;TrustServerCertificate=True"
            })
            .Build();

        var services = new ServiceCollection()
            .AddCatalogReleaseInfrastructure(configuration)
            .BuildServiceProvider();

        var ledger = services.GetRequiredService<ICatalogReleaseLedger>();

        Assert.IsType<SqlCatalogReleaseLedger>(ledger);
    }

    [Fact]
    public void Uses_local_release_ledger_when_release_control_connection_string_is_not_configured()
    {
        var configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection()
            .AddCatalogReleaseInfrastructure(configuration)
            .BuildServiceProvider();

        var ledger = services.GetRequiredService<ICatalogReleaseLedger>();

        Assert.IsType<LocalCatalogReleaseLedger>(ledger);
    }
}
