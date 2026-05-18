using FastFashionCatalogSync.Domain.Rollouts;

namespace FastFashionCatalogSync.Application.Rollouts;

public sealed record ScheduleProductRolloutResult(Guid RolloutId, ProductRolloutStatus Status);
