namespace FastFashionCatalogSync.Domain.Releases;

public enum CatalogReleaseStatus
{
    Scheduled,
    Publishing,
    Published,
    Blocked,
    Failed
}
