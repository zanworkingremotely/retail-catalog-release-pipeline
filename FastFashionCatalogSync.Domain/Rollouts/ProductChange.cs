namespace FastFashionCatalogSync.Domain.Rollouts;

public sealed record ProductChange(
    ProductChangeType Type,
    string Sku,
    string Region,
    string Field,
    string? CurrentValue,
    string? ProposedValue);
