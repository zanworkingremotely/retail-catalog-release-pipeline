using FastFashionCatalogSync.Domain.Catalog;

namespace FastFashionCatalogSync.Application.Abstractions;

public interface IOperationalCatalogReader
{
    Task<IReadOnlyCollection<OperationalCatalogItem>> GetOperationalItemsAsync(CancellationToken cancellationToken);
    Task<string> GetCurrentOperationalVersionIdAsync(CancellationToken cancellationToken);
}
