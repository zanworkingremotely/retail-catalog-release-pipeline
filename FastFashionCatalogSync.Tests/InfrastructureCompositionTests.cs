using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Infrastructure.DependencyInjection;
using FastFashionCatalogSync.Infrastructure.Rollouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastFashionCatalogSync.Tests;

public sealed class InfrastructureCompositionTests
{
    [Fact]
    public void Uses_sql_rollout_ledger_when_rollout_control_connection_string_is_configured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RolloutControl"] = "Server=localhost,14333;Database=RetailRolloutControlDb;User Id=sa;Password=RetailRollout!2026;TrustServerCertificate=True"
            })
            .Build();

        var services = new ServiceCollection()
            .AddProductRolloutInfrastructure(configuration)
            .BuildServiceProvider();

        var ledger = services.GetRequiredService<IProductRolloutLedger>();

        Assert.IsType<SqlProductRolloutLedger>(ledger);
    }

    [Fact]
    public void Uses_local_rollout_ledger_when_rollout_control_connection_string_is_not_configured()
    {
        var configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection()
            .AddProductRolloutInfrastructure(configuration)
            .BuildServiceProvider();

        var ledger = services.GetRequiredService<IProductRolloutLedger>();

        Assert.IsType<LocalProductRolloutLedger>(ledger);
    }
}
