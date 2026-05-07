namespace FastFashionCatalogSync.Domain.Releases;

public sealed record CatalogChange(
    CatalogChangeType Type,
    string Sku,
    string Region,
    string Field,
    string? CurrentValue,
    string? ProposedValue);
