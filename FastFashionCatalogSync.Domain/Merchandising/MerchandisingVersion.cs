namespace FastFashionCatalogSync.Domain.Merchandising;

public sealed record MerchandisingVersion(
    string VersionId,
    string Label,
    DateTimeOffset ApprovedAt,
    IReadOnlyCollection<MerchandisingItemSnapshot> Items);
