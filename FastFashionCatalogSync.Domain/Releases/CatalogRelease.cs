namespace FastFashionCatalogSync.Domain.Releases;

public sealed class CatalogRelease
{
    public CatalogRelease(
        Guid id,
        string merchandisingVersionId,
        string previewFingerprint,
        DateTimeOffset scheduledFor,
        string requestedBy,
        DateTimeOffset requestedAt)
    {
        Id = id;
        MerchandisingVersionId = merchandisingVersionId;
        PreviewFingerprint = previewFingerprint;
        ScheduledFor = scheduledFor;
        RequestedBy = requestedBy;
        RequestedAt = requestedAt;
        Status = CatalogReleaseStatus.Scheduled;
    }

    public Guid Id { get; }
    public string MerchandisingVersionId { get; }
    public string PreviewFingerprint { get; }
    public DateTimeOffset ScheduledFor { get; }
    public string RequestedBy { get; }
    public DateTimeOffset RequestedAt { get; }
    public CatalogReleaseStatus Status { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public string? ExecutionMessage { get; private set; }

    public static CatalogRelease Restore(
        Guid id,
        string merchandisingVersionId,
        string previewFingerprint,
        DateTimeOffset scheduledFor,
        string requestedBy,
        DateTimeOffset requestedAt,
        CatalogReleaseStatus status,
        DateTimeOffset? executedAt,
        string? executionMessage)
    {
        return new CatalogRelease(id, merchandisingVersionId, previewFingerprint, scheduledFor, requestedBy, requestedAt)
        {
            Status = status,
            ExecutedAt = executedAt,
            ExecutionMessage = executionMessage
        };
    }

    public bool IsDue(DateTimeOffset now) =>
        Status == CatalogReleaseStatus.Scheduled && ScheduledFor <= now;

    public void MarkPublishing()
    {
        if (Status != CatalogReleaseStatus.Scheduled)
        {
            throw new InvalidOperationException($"Release {Id} cannot publish from {Status}.");
        }

        Status = CatalogReleaseStatus.Publishing;
    }

    public void MarkPublished(DateTimeOffset executedAt, string message)
    {
        Status = CatalogReleaseStatus.Published;
        ExecutedAt = executedAt;
        ExecutionMessage = message;
    }

    public void MarkBlocked(DateTimeOffset executedAt, string reason)
    {
        Status = CatalogReleaseStatus.Blocked;
        ExecutedAt = executedAt;
        ExecutionMessage = reason;
    }

    public void MarkFailed(DateTimeOffset executedAt, string reason)
    {
        Status = CatalogReleaseStatus.Failed;
        ExecutedAt = executedAt;
        ExecutionMessage = reason;
    }
}
