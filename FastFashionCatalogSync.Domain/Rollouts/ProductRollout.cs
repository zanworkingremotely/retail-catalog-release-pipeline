namespace FastFashionCatalogSync.Domain.Rollouts;

public sealed class ProductRollout
{
    public ProductRollout(
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
        Status = ProductRolloutStatus.Scheduled;
    }

    public Guid Id { get; }
    public string MerchandisingVersionId { get; }
    public string PreviewFingerprint { get; }
    public DateTimeOffset ScheduledFor { get; }
    public string RequestedBy { get; }
    public DateTimeOffset RequestedAt { get; }
    public ProductRolloutStatus Status { get; private set; }
    public DateTimeOffset? ClaimedAt { get; private set; }
    public string? ClaimedBy { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public string? ExecutionMessage { get; private set; }

    public static ProductRollout Restore(
        Guid id,
        string merchandisingVersionId,
        string previewFingerprint,
        DateTimeOffset scheduledFor,
        string requestedBy,
        DateTimeOffset requestedAt,
        ProductRolloutStatus status,
        DateTimeOffset? claimedAt,
        string? claimedBy,
        DateTimeOffset? executedAt,
        string? executionMessage)
    {
        return new ProductRollout(id, merchandisingVersionId, previewFingerprint, scheduledFor, requestedBy, requestedAt)
        {
            Status = status,
            ClaimedAt = claimedAt,
            ClaimedBy = claimedBy,
            ExecutedAt = executedAt,
            ExecutionMessage = executionMessage
        };
    }

    public bool IsDue(DateTimeOffset now) =>
        Status == ProductRolloutStatus.Scheduled && ScheduledFor <= now;

    public bool HasStaleClaim(DateTimeOffset now, TimeSpan staleClaimAge) =>
        Status == ProductRolloutStatus.Publishing &&
        ClaimedAt is not null &&
        ClaimedAt.Value <= now.Subtract(staleClaimAge);

    public void MarkPublishing(DateTimeOffset claimedAt, string claimedBy)
    {
        if (Status is not ProductRolloutStatus.Scheduled and not ProductRolloutStatus.Publishing)
        {
            throw new InvalidOperationException($"Rollout {Id} cannot publish from {Status}.");
        }

        Status = ProductRolloutStatus.Publishing;
        ClaimedAt = claimedAt;
        ClaimedBy = claimedBy;
    }

    public void MarkPublished(DateTimeOffset executedAt, string message)
    {
        Status = ProductRolloutStatus.Published;
        ExecutedAt = executedAt;
        ExecutionMessage = message;
    }

    public void MarkBlocked(DateTimeOffset executedAt, string reason)
    {
        Status = ProductRolloutStatus.Blocked;
        ExecutedAt = executedAt;
        ExecutionMessage = reason;
    }

    public void MarkFailed(DateTimeOffset executedAt, string reason)
    {
        Status = ProductRolloutStatus.Failed;
        ExecutedAt = executedAt;
        ExecutionMessage = reason;
    }
}
