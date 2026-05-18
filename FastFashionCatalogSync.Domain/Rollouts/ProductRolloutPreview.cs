namespace FastFashionCatalogSync.Domain.Rollouts;

public sealed record ProductRolloutPreview(
    string MerchandisingVersionId,
    string CurrentOperationalVersionId,
    string Fingerprint,
    IReadOnlyCollection<ProductChange> Changes)
{
    public bool HasChanges => Changes.Count > 0;
}
