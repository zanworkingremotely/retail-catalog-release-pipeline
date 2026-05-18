namespace FastFashionCatalogSync.Application.Rollouts;

public sealed record ProductRolloutSweepResult(int Evaluated, int Published, int Blocked, int Failed);
