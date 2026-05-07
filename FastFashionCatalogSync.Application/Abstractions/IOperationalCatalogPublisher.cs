using FastFashionCatalogSync.Domain.Catalog;

namespace FastFashionCatalogSync.Application.Abstractions;

public interface IOperationalCatalogPublisher
{
    Task PublishVersionAsync(CatalogVersion version, CancellationToken cancellationToken);
}
