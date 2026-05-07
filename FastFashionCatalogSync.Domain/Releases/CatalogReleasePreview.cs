namespace FastFashionCatalogSync.Domain.Releases;

public sealed record CatalogReleasePreview(
    string MerchandisingVersionId,
    string CurrentOperationalVersionId,
    string Fingerprint,
    IReadOnlyCollection<CatalogChange> Changes)
{
    public bool HasChanges => Changes.Count > 0;
}
