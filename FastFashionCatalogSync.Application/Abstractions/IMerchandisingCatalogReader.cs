using FastFashionCatalogSync.Domain.Catalog;

namespace FastFashionCatalogSync.Application.Abstractions;

public interface IMerchandisingCatalogReader
{
    Task<CatalogVersion> GetApprovedVersionAsync(string versionId, CancellationToken cancellationToken);
    Task<CatalogVersion> GetLatestApprovedVersionAsync(CancellationToken cancellationToken);
}
