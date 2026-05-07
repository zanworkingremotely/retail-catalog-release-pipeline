namespace FastFashionCatalogSync.Application.Releases;

public sealed record ScheduleCatalogReleaseRequest(
    string MerchandisingVersionId,
    DateTimeOffset ScheduledFor,
    string RequestedBy,
    string PreviewFingerprint);
