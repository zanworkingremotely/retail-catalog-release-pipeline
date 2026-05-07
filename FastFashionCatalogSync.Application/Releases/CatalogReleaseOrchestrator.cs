using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Releases;

namespace FastFashionCatalogSync.Application.Releases;

public sealed class CatalogReleaseOrchestrator
{
    private readonly CatalogReleasePreviewBuilder _previewBuilder;
    private readonly IMerchandisingCatalogReader _merchandisingCatalog;
    private readonly IOperationalCatalogPublisher _operationalCatalog;
    private readonly ICatalogReleaseLedger _releaseLedger;
    private readonly IClock _clock;

    public CatalogReleaseOrchestrator(
        CatalogReleasePreviewBuilder previewBuilder,
        IMerchandisingCatalogReader merchandisingCatalog,
        IOperationalCatalogPublisher operationalCatalog,
        ICatalogReleaseLedger releaseLedger,
        IClock clock)
    {
        _previewBuilder = previewBuilder;
        _merchandisingCatalog = merchandisingCatalog;
        _operationalCatalog = operationalCatalog;
        _releaseLedger = releaseLedger;
        _clock = clock;
    }

    public async Task<ScheduleCatalogReleaseResult> ScheduleAsync(
        ScheduleCatalogReleaseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ScheduledFor <= _clock.UtcNow)
        {
            throw new InvalidOperationException("Catalog releases must be scheduled for a future time.");
        }

        var preview = await _previewBuilder.PreviewVersionAsync(request.MerchandisingVersionId, cancellationToken);
        if (!preview.HasChanges)
        {
            throw new InvalidOperationException("The selected catalog version has no publishable changes.");
        }

        if (!StringComparer.Ordinal.Equals(preview.Fingerprint, request.PreviewFingerprint))
        {
            throw new InvalidOperationException("The preview fingerprint no longer matches the approved change set.");
        }

        var release = new CatalogRelease(
            Guid.NewGuid(),
            request.MerchandisingVersionId,
            request.PreviewFingerprint,
            request.ScheduledFor,
            request.RequestedBy,
            _clock.UtcNow);

        await _releaseLedger.AddAsync(release, cancellationToken);

        return new ScheduleCatalogReleaseResult(release.Id, release.Status);
    }

    public Task<IReadOnlyCollection<CatalogRelease>> ListAsync(CancellationToken cancellationToken) =>
        _releaseLedger.ListAsync(cancellationToken);

    public async Task<CatalogReleaseSweepResult> ExecuteDueAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var due = await _releaseLedger.ListDueAsync(now, cancellationToken);
        var published = 0;
        var blocked = 0;
        var failed = 0;

        foreach (var release in due)
        {
            try
            {
                release.MarkPublishing();
                var preview = await _previewBuilder.PreviewVersionAsync(release.MerchandisingVersionId, cancellationToken);
                if (!StringComparer.Ordinal.Equals(preview.Fingerprint, release.PreviewFingerprint))
                {
                    release.MarkBlocked(now, "Approved preview fingerprint changed before execution.");
                    blocked++;
                }
                else
                {
                    var version = await _merchandisingCatalog.GetApprovedVersionAsync(release.MerchandisingVersionId, cancellationToken);
                    await _operationalCatalog.PublishVersionAsync(version, cancellationToken);
                    release.MarkPublished(now, $"Published {preview.Changes.Count} catalog changes.");
                    published++;
                }
            }
            catch (Exception ex)
            {
                release.MarkFailed(now, ex.Message);
                failed++;
            }
            finally
            {
                await _releaseLedger.UpdateAsync(release, cancellationToken);
            }
        }

        return new CatalogReleaseSweepResult(due.Count, published, blocked, failed);
    }
}
