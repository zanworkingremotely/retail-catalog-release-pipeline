namespace FastFashionCatalogSync.Application.Rollouts;

public sealed record ScheduleProductRolloutRequest(
    string MerchandisingVersionId,
    DateTimeOffset ScheduledFor,
    string RequestedBy,
    string PreviewFingerprint);
