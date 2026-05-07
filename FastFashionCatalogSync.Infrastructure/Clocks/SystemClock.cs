using FastFashionCatalogSync.Application.Abstractions;

namespace FastFashionCatalogSync.Infrastructure.Clocks;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
