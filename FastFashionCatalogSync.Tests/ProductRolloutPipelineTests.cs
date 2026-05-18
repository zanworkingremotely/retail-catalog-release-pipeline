using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Application.Rollouts;
using FastFashionCatalogSync.Domain.Rollouts;
using FastFashionCatalogSync.Infrastructure.Catalog;
using FastFashionCatalogSync.Infrastructure.Rollouts;

namespace FastFashionCatalogSync.Tests;

public sealed class ProductRolloutPipelineTests
{
    private static readonly TimeSpan StaleClaimAge = TimeSpan.FromMinutes(10);

    [Fact]
    public async Task Preview_latest_approved_catalog_returns_deterministic_business_diff()
    {
        var services = CreateServices();

        var preview = await services.DiffService.PreviewLatestApprovedAsync(CancellationToken.None);

        Assert.Equal("SS26-DROP-02", preview.MerchandisingVersionId);
        Assert.Equal("SS26-DROP-01", preview.CurrentOperationalVersionId);
        Assert.NotEmpty(preview.Fingerprint);
        Assert.Contains(preview.Changes, change => change.Type == ProductChangeType.PriceChanged && change.Sku == "TEE-OVR-001");
        Assert.Contains(preview.Changes, change => change.Type == ProductChangeType.CategoryChanged && change.Sku == "DEN-WID-220");
        Assert.Contains(preview.Changes, change => change.Type == ProductChangeType.Added && change.Sku == "JKT-BMB-089");
        Assert.Contains(preview.Changes, change => change.Type == ProductChangeType.Removed && change.Sku == "DRS-LIN-144");
    }

    [Fact]
    public async Task Scheduled_rollout_publishes_only_when_due_and_only_once()
    {
        var services = CreateServices();
        var preview = await services.DiffService.PreviewLatestApprovedAsync(CancellationToken.None);
        var scheduledFor = services.Clock.UtcNow.AddMinutes(30);

        await services.RolloutWorkflow.ScheduleAsync(
            new ScheduleProductRolloutRequest(
                preview.MerchandisingVersionId,
                scheduledFor,
                "merchandising-admin@retail.com",
                preview.Fingerprint),
            CancellationToken.None);

        var early = await services.RolloutWorkflow.ExecuteDueAsync(CancellationToken.None);
        Assert.Equal(0, early.Evaluated);
        Assert.Equal("SS26-DROP-01", await services.LiveGateway.GetCurrentOperationalVersionIdAsync(CancellationToken.None));

        services.Clock.UtcNow = scheduledFor.AddSeconds(1);

        var due = await services.RolloutWorkflow.ExecuteDueAsync(CancellationToken.None);
        Assert.Equal(new ProductRolloutSweepResult(1, 1, 0, 0), due);
        Assert.Equal("SS26-DROP-02", await services.LiveGateway.GetCurrentOperationalVersionIdAsync(CancellationToken.None));

        var secondPass = await services.RolloutWorkflow.ExecuteDueAsync(CancellationToken.None);
        Assert.Equal(0, secondPass.Evaluated);

        var rollout = Assert.Single(await services.RolloutStore.ListAsync(CancellationToken.None));
        Assert.Equal(ProductRolloutStatus.Published, rollout.Status);
    }

    [Fact]
    public async Task Scheduled_rollout_blocks_when_the_approved_preview_changes_before_execution()
    {
        var services = CreateServices();
        var preview = await services.DiffService.PreviewLatestApprovedAsync(CancellationToken.None);
        var scheduledFor = services.Clock.UtcNow.AddMinutes(15);

        await services.RolloutWorkflow.ScheduleAsync(
            new ScheduleProductRolloutRequest(
                preview.MerchandisingVersionId,
                scheduledFor,
                "merchandising-admin@retail.com",
                preview.Fingerprint),
            CancellationToken.None);

        services.Database.PromoteToOperationalCatalog(await services.SourceReader.GetApprovedVersionAsync("SS26-DROP-02", CancellationToken.None));
        services.Clock.UtcNow = scheduledFor.AddSeconds(1);

        var result = await services.RolloutWorkflow.ExecuteDueAsync(CancellationToken.None);

        Assert.Equal(new ProductRolloutSweepResult(1, 0, 1, 0), result);
        var rollout = Assert.Single(await services.RolloutStore.ListAsync(CancellationToken.None));
        Assert.Equal(ProductRolloutStatus.Blocked, rollout.Status);
    }

    [Fact]
    public async Task Due_rollout_claim_is_not_returned_to_another_worker()
    {
        var services = CreateServices();
        var preview = await services.DiffService.PreviewLatestApprovedAsync(CancellationToken.None);
        var scheduledFor = services.Clock.UtcNow.AddMinutes(15);

        await services.RolloutWorkflow.ScheduleAsync(
            new ScheduleProductRolloutRequest(
                preview.MerchandisingVersionId,
                scheduledFor,
                "merchandising-admin@retail.com",
                preview.Fingerprint),
            CancellationToken.None);

        services.Clock.UtcNow = scheduledFor.AddSeconds(1);

        var firstClaim = await services.RolloutStore.ClaimDueAsync(
            services.Clock.UtcNow,
            "worker-a",
            StaleClaimAge,
            CancellationToken.None);

        var secondClaim = await services.RolloutStore.ClaimDueAsync(
            services.Clock.UtcNow,
            "worker-b",
            StaleClaimAge,
            CancellationToken.None);

        var rollout = Assert.Single(firstClaim);
        Assert.Equal(ProductRolloutStatus.Publishing, rollout.Status);
        Assert.Equal("worker-a", rollout.ClaimedBy);
        Assert.Empty(secondClaim);
    }

    [Fact]
    public async Task Stale_publishing_rollout_can_be_claimed_by_another_worker()
    {
        var services = CreateServices();
        var preview = await services.DiffService.PreviewLatestApprovedAsync(CancellationToken.None);
        var scheduledFor = services.Clock.UtcNow.AddMinutes(15);

        await services.RolloutWorkflow.ScheduleAsync(
            new ScheduleProductRolloutRequest(
                preview.MerchandisingVersionId,
                scheduledFor,
                "merchandising-admin@retail.com",
                preview.Fingerprint),
            CancellationToken.None);

        services.Clock.UtcNow = scheduledFor.AddSeconds(1);

        var firstClaim = await services.RolloutStore.ClaimDueAsync(
            services.Clock.UtcNow,
            "worker-a",
            StaleClaimAge,
            CancellationToken.None);

        services.Clock.UtcNow = services.Clock.UtcNow.Add(StaleClaimAge).AddSeconds(1);

        var reclaimed = await services.RolloutStore.ClaimDueAsync(
            services.Clock.UtcNow,
            "worker-b",
            StaleClaimAge,
            CancellationToken.None);

        Assert.Single(firstClaim);
        var rollout = Assert.Single(reclaimed);
        Assert.Equal(ProductRolloutStatus.Publishing, rollout.Status);
        Assert.Equal("worker-b", rollout.ClaimedBy);
        Assert.Equal(services.Clock.UtcNow, rollout.ClaimedAt);
    }

    private static TestServices CreateServices()
    {
        var database = new LocalRetailCatalogContext();
        var clock = new TestClock { UtcNow = DateTimeOffset.Parse("2026-05-07T08:00:00Z") };
        var sourceReader = new MerchandisingCatalogReader(database);
        var liveGateway = new OperationalCatalogGateway(database);
        var rolloutLedger = new LocalProductRolloutLedger(database);
        var previewBuilder = new ProductRolloutPreviewBuilder(sourceReader, liveGateway);
        var rolloutWorkflow = new ProductRolloutWorkflow(previewBuilder, sourceReader, liveGateway, rolloutLedger, clock);

        return new TestServices(database, clock, sourceReader, liveGateway, rolloutLedger, previewBuilder, rolloutWorkflow);
    }

    private sealed record TestServices(
        LocalRetailCatalogContext Database,
        TestClock Clock,
        MerchandisingCatalogReader SourceReader,
        OperationalCatalogGateway LiveGateway,
        LocalProductRolloutLedger RolloutStore,
        ProductRolloutPreviewBuilder DiffService,
        ProductRolloutWorkflow RolloutWorkflow);

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
