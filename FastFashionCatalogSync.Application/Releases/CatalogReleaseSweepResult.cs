namespace FastFashionCatalogSync.Application.Releases;

public sealed record CatalogReleaseSweepResult(int Evaluated, int Published, int Blocked, int Failed);
