using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Application.Releases;
using FastFashionCatalogSync.Domain.Releases;
using FastFashionCatalogSync.Infrastructure.Catalog;
using FastFashionCatalogSync.Infrastructure.Releases;

namespace FastFashionCatalogSync.Tests;

public sealed class CatalogReleasePipelineTests
{
    [Fact]
    public async Task Preview_latest_approved_catalog_returns_deterministic_business_diff()
    {
        var services = CreateServices();

        var preview = await services.DiffService.PreviewLatestApprovedAsync(CancellationToken.None);

        Assert.Equal("SS26-DROP-02", preview.MerchandisingVersionId);
        Assert.Equal("SS26-DROP-01", preview.CurrentOperationalVersionId);
        Assert.NotEmpty(preview.Fingerprint);
        Assert.Contains(preview.Changes, change => change.Type == CatalogChangeType.PriceChanged && change.Sku == "TEE-OVR-001");
        Assert.Contains(preview.Changes, change => change.Type == CatalogChangeType.CategoryChanged && change.Sku == "DEN-WID-220");
        Assert.Contains(preview.Changes, change => change.Type == CatalogChangeType.Added && change.Sku == "JKT-BMB-089");
        Assert.Contains(preview.Changes, change => change.Type == CatalogChangeType.Removed && change.Sku == "DRS-LIN-144");
    }

    [Fact]
    public async Task Scheduled_release_publishes_only_when_due_and_only_once()
    {
        var services = CreateServices();
        var preview = await services.DiffService.PreviewLatestApprovedAsync(CancellationToken.None);
        var scheduledFor = services.Clock.UtcNow.AddMinutes(30);

        await services.ReleaseService.ScheduleAsync(
            new ScheduleCatalogReleaseRequest(
                preview.MerchandisingVersionId,
                scheduledFor,
                "catalog-admin@retail.com",
                preview.Fingerprint),
            CancellationToken.None);

        var early = await services.ReleaseService.ExecuteDueAsync(CancellationToken.None);
        Assert.Equal(0, early.Evaluated);
        Assert.Equal("SS26-DROP-01", await services.LiveGateway.GetCurrentOperationalVersionIdAsync(CancellationToken.None));

        services.Clock.UtcNow = scheduledFor.AddSeconds(1);

        var due = await services.ReleaseService.ExecuteDueAsync(CancellationToken.None);
        Assert.Equal(new CatalogReleaseSweepResult(1, 1, 0, 0), due);
        Assert.Equal("SS26-DROP-02", await services.LiveGateway.GetCurrentOperationalVersionIdAsync(CancellationToken.None));

        var secondPass = await services.ReleaseService.ExecuteDueAsync(CancellationToken.None);
        Assert.Equal(0, secondPass.Evaluated);

        var release = Assert.Single(await services.ReleaseStore.ListAsync(CancellationToken.None));
        Assert.Equal(CatalogReleaseStatus.Published, release.Status);
    }

    [Fact]
    public async Task Scheduled_release_blocks_when_the_approved_preview_changes_before_execution()
    {
        var services = CreateServices();
        var preview = await services.DiffService.PreviewLatestApprovedAsync(CancellationToken.None);
        var scheduledFor = services.Clock.UtcNow.AddMinutes(15);

        await services.ReleaseService.ScheduleAsync(
            new ScheduleCatalogReleaseRequest(
                preview.MerchandisingVersionId,
                scheduledFor,
                "catalog-admin@retail.com",
                preview.Fingerprint),
            CancellationToken.None);

        services.Database.PromoteToOperationalCatalog(await services.SourceReader.GetApprovedVersionAsync("SS26-DROP-02", CancellationToken.None));
        services.Clock.UtcNow = scheduledFor.AddSeconds(1);

        var result = await services.ReleaseService.ExecuteDueAsync(CancellationToken.None);

        Assert.Equal(new CatalogReleaseSweepResult(1, 0, 1, 0), result);
        var release = Assert.Single(await services.ReleaseStore.ListAsync(CancellationToken.None));
        Assert.Equal(CatalogReleaseStatus.Blocked, release.Status);
    }

    private static TestServices CreateServices()
    {
        var database = new LocalRetailCatalogContext();
        var clock = new TestClock { UtcNow = DateTimeOffset.Parse("2026-05-07T08:00:00Z") };
        var sourceReader = new MerchandisingCatalogReader(database);
        var liveGateway = new OperationalCatalogGateway(database);
        var releaseLedger = new LocalCatalogReleaseLedger(database);
        var previewBuilder = new CatalogReleasePreviewBuilder(sourceReader, liveGateway);
        var releaseService = new CatalogReleaseOrchestrator(previewBuilder, sourceReader, liveGateway, releaseLedger, clock);

        return new TestServices(database, clock, sourceReader, liveGateway, releaseLedger, previewBuilder, releaseService);
    }

    private sealed record TestServices(
        LocalRetailCatalogContext Database,
        TestClock Clock,
        MerchandisingCatalogReader SourceReader,
        OperationalCatalogGateway LiveGateway,
        LocalCatalogReleaseLedger ReleaseStore,
        CatalogReleasePreviewBuilder DiffService,
        CatalogReleaseOrchestrator ReleaseService);

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
