using FastFashionCatalogSync.Application.Releases;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastFashionCatalogSync.Scheduling;

public sealed class CatalogReleaseWorker : BackgroundService
{
    private readonly CatalogReleaseOrchestrator _releaseService;
    private readonly ILogger<CatalogReleaseWorker> _logger;

    public CatalogReleaseWorker(CatalogReleaseOrchestrator releaseService, ILogger<CatalogReleaseWorker> logger)
    {
        _releaseService = releaseService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var result = await _releaseService.ExecuteDueAsync(stoppingToken);
            if (result.Evaluated > 0)
            {
                _logger.LogInformation(
                    "Catalog release sweep evaluated {Evaluated} releases. Published: {Published}, blocked: {Blocked}, failed: {Failed}.",
                    result.Evaluated,
                    result.Published,
                    result.Blocked,
                    result.Failed);
            }
        }
    }
}
