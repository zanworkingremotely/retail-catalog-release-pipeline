using FastFashionCatalogSync.Domain.Releases;

namespace FastFashionCatalogSync.Application.Abstractions;

public interface ICatalogReleaseLedger
{
    Task AddAsync(CatalogRelease release, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CatalogRelease>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CatalogRelease>> ListDueAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task UpdateAsync(CatalogRelease release, CancellationToken cancellationToken);
}
