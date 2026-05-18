using FastFashionCatalogSync.Application.Rollouts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastFashionCatalogSync.Scheduling;

public sealed class ProductRolloutWorker : BackgroundService
{
    private readonly ProductRolloutWorkflow _rolloutWorkflow;
    private readonly ILogger<ProductRolloutWorker> _logger;

    public ProductRolloutWorker(ProductRolloutWorkflow rolloutWorkflow, ILogger<ProductRolloutWorker> logger)
    {
        _rolloutWorkflow = rolloutWorkflow;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var result = await _rolloutWorkflow.ExecuteDueAsync(stoppingToken);
            if (result.Evaluated > 0)
            {
                _logger.LogInformation(
                    "Product rollout sweep evaluated {Evaluated} rollouts. Published: {Published}, blocked: {Blocked}, failed: {Failed}.",
                    result.Evaluated,
                    result.Published,
                    result.Blocked,
                    result.Failed);
            }
        }
    }
}
