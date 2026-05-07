namespace FastFashionCatalogSync.Domain.Catalog;

public sealed record CatalogVersion(
    string VersionId,
    string Label,
    DateTimeOffset ApprovedAt,
    IReadOnlyCollection<CatalogItemSnapshot> Items);
