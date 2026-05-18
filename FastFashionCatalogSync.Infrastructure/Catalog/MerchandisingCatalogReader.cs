using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Merchandising;

namespace FastFashionCatalogSync.Infrastructure.Catalog;

public sealed class MerchandisingCatalogReader : IMerchandisingCatalogReader
{
    private readonly LocalRetailCatalogContext _databases;

    public MerchandisingCatalogReader(LocalRetailCatalogContext databases)
    {
        _databases = databases;
    }

    public Task<MerchandisingVersion> GetApprovedVersionAsync(string versionId, CancellationToken cancellationToken)
    {
        var version = _databases.MerchandisingVersions
            .SingleOrDefault(version => StringComparer.OrdinalIgnoreCase.Equals(version.VersionId, versionId));

        return Task.FromResult(version ?? throw new InvalidOperationException($"Approved merchandising version {versionId} was not found."));
    }

    public Task<MerchandisingVersion> GetLatestApprovedVersionAsync(CancellationToken cancellationToken)
    {
        var version = _databases.MerchandisingVersions
            .OrderByDescending(version => version.ApprovedAt)
            .First();

        return Task.FromResult(version);
    }
}
