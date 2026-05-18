using FastFashionCatalogSync.Domain.Merchandising;

namespace FastFashionCatalogSync.Application.Abstractions;

public interface IOperationalCatalogPublisher
{
    Task PublishVersionAsync(MerchandisingVersion version, CancellationToken cancellationToken);
}
