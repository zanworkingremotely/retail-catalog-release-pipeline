using FastFashionCatalogSync.Domain.Merchandising;

namespace FastFashionCatalogSync.Application.Abstractions;

public interface IMerchandisingCatalogReader
{
    Task<MerchandisingVersion> GetApprovedVersionAsync(string versionId, CancellationToken cancellationToken);
    Task<MerchandisingVersion> GetLatestApprovedVersionAsync(CancellationToken cancellationToken);
}
