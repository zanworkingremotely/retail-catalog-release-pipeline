using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Releases;
using FastFashionCatalogSync.Infrastructure.Catalog;

namespace FastFashionCatalogSync.Infrastructure.Releases;

public sealed class LocalCatalogReleaseLedger : ICatalogReleaseLedger
{
    private readonly LocalRetailCatalogContext _databases;

    public LocalCatalogReleaseLedger(LocalRetailCatalogContext databases)
    {
        _databases = databases;
    }

    public Task AddAsync(CatalogRelease release, CancellationToken cancellationToken)
    {
        _databases.Releases.Add(release);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<CatalogRelease>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<CatalogRelease>>(_databases.Releases.OrderBy(release => release.ScheduledFor).ToList());

    public Task<IReadOnlyCollection<CatalogRelease>> ClaimDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var releases = _databases.Releases
            .Where(release => release.IsDue(now))
            .OrderBy(release => release.ScheduledFor)
            .ToList();

        foreach (var release in releases)
        {
            release.MarkPublishing();
        }

        return Task.FromResult<IReadOnlyCollection<CatalogRelease>>(releases);
    }

    public Task UpdateAsync(CatalogRelease release, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
