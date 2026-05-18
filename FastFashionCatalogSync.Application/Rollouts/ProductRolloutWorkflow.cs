using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Rollouts;

namespace FastFashionCatalogSync.Application.Rollouts;

public sealed class ProductRolloutWorkflow
{
    private static readonly TimeSpan StaleClaimAge = TimeSpan.FromMinutes(10);

    private readonly ProductRolloutPreviewBuilder _previewBuilder;
    private readonly IMerchandisingCatalogReader _merchandisingCatalog;
    private readonly IOperationalCatalogPublisher _operationalCatalog;
    private readonly IProductRolloutLedger _rolloutLedger;
    private readonly IClock _clock;
    private readonly string _claimOwner;

    public ProductRolloutWorkflow(
        ProductRolloutPreviewBuilder previewBuilder,
        IMerchandisingCatalogReader merchandisingCatalog,
        IOperationalCatalogPublisher operationalCatalog,
        IProductRolloutLedger rolloutLedger,
        IClock clock)
    {
        _previewBuilder = previewBuilder;
        _merchandisingCatalog = merchandisingCatalog;
        _operationalCatalog = operationalCatalog;
        _rolloutLedger = rolloutLedger;
        _clock = clock;
        _claimOwner = $"{Environment.MachineName}:{Environment.ProcessId}";
    }

    public async Task<ScheduleProductRolloutResult> ScheduleAsync(
        ScheduleProductRolloutRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ScheduledFor <= _clock.UtcNow)
        {
            throw new InvalidOperationException("Product rollouts must be scheduled for a future time.");
        }

        var preview = await _previewBuilder.PreviewVersionAsync(request.MerchandisingVersionId, cancellationToken);
        if (!preview.HasChanges)
        {
            throw new InvalidOperationException("The selected merchandising version has no publishable changes.");
        }

        if (!StringComparer.Ordinal.Equals(preview.Fingerprint, request.PreviewFingerprint))
        {
            throw new InvalidOperationException("The preview fingerprint no longer matches the approved change set.");
        }

        var rollout = new ProductRollout(
            Guid.NewGuid(),
            request.MerchandisingVersionId,
            request.PreviewFingerprint,
            request.ScheduledFor,
            request.RequestedBy,
            _clock.UtcNow);

        await _rolloutLedger.AddAsync(rollout, cancellationToken);

        return new ScheduleProductRolloutResult(rollout.Id, rollout.Status);
    }

    public Task<IReadOnlyCollection<ProductRollout>> ListAsync(CancellationToken cancellationToken) =>
        _rolloutLedger.ListAsync(cancellationToken);

    public async Task<ProductRolloutSweepResult> ExecuteDueAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var due = await _rolloutLedger.ClaimDueAsync(now, _claimOwner, StaleClaimAge, cancellationToken);
        var published = 0;
        var blocked = 0;
        var failed = 0;

        foreach (var rollout in due)
        {
            try
            {
                var preview = await _previewBuilder.PreviewVersionAsync(rollout.MerchandisingVersionId, cancellationToken);
                if (!StringComparer.Ordinal.Equals(preview.Fingerprint, rollout.PreviewFingerprint))
                {
                    rollout.MarkBlocked(now, "Approved preview fingerprint changed before execution.");
                    blocked++;
                }
                else
                {
                    var version = await _merchandisingCatalog.GetApprovedVersionAsync(rollout.MerchandisingVersionId, cancellationToken);
                    await _operationalCatalog.PublishVersionAsync(version, cancellationToken);
                    rollout.MarkPublished(now, $"Published {preview.Changes.Count} product changes.");
                    published++;
                }
            }
            catch (Exception ex)
            {
                rollout.MarkFailed(now, ex.Message);
                failed++;
            }
            finally
            {
                await _rolloutLedger.UpdateAsync(rollout, cancellationToken);
            }
        }

        return new ProductRolloutSweepResult(due.Count, published, blocked, failed);
    }
}
