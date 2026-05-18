using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Catalog;
using FastFashionCatalogSync.Domain.Merchandising;

namespace FastFashionCatalogSync.Infrastructure.Catalog;

public sealed class OperationalCatalogGateway : IOperationalCatalogReader, IOperationalCatalogPublisher
{
    private readonly LocalRetailCatalogContext _databases;

    public OperationalCatalogGateway(LocalRetailCatalogContext databases)
    {
        _databases = databases;
    }

    public Task<IReadOnlyCollection<OperationalCatalogItem>> GetOperationalItemsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<OperationalCatalogItem>>(_databases.OperationalItems.ToList());

    public Task<string> GetCurrentOperationalVersionIdAsync(CancellationToken cancellationToken)
    {
        var versionId = _databases.OperationalItems
            .Select(item => item.PublishedVersionId)
            .Distinct(StringComparer.Ordinal)
            .SingleOrDefault() ?? "EMPTY";

        return Task.FromResult(versionId);
    }

    public Task PublishVersionAsync(MerchandisingVersion version, CancellationToken cancellationToken)
    {
        _databases.PromoteToOperationalCatalog(version);
        return Task.CompletedTask;
    }
}
