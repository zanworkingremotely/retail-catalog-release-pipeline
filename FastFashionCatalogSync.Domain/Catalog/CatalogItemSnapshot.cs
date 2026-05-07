namespace FastFashionCatalogSync.Domain.Catalog;

public sealed record CatalogItemSnapshot(
    string Sku,
    string Name,
    string Category,
    decimal Price,
    string Region,
    bool IsAvailable);
