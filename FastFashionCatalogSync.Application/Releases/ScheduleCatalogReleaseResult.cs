using FastFashionCatalogSync.Domain.Releases;

namespace FastFashionCatalogSync.Application.Releases;

public sealed record ScheduleCatalogReleaseResult(Guid ReleaseId, CatalogReleaseStatus Status);
