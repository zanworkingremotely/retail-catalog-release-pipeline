namespace FastFashionCatalogSync.Domain.Catalog;

public sealed record OperationalCatalogItem(
    string Sku,
    string Name,
    string Category,
    decimal Price,
    string Region,
    bool IsAvailable,
    string PublishedVersionId);
