namespace FastFashionCatalogSync.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
