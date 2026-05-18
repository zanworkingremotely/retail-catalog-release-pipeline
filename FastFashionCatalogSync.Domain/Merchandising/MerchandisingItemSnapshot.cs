namespace FastFashionCatalogSync.Domain.Merchandising;

public sealed record MerchandisingItemSnapshot(
    string Sku,
    string Name,
    string Category,
    decimal Price,
    string Region,
    bool IsAvailable);
