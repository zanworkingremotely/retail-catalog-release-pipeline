using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Rollouts;
using FastFashionCatalogSync.Infrastructure.Catalog;

namespace FastFashionCatalogSync.Infrastructure.Rollouts;

public sealed class LocalProductRolloutLedger : IProductRolloutLedger
{
    private readonly LocalRetailCatalogContext _databases;

    public LocalProductRolloutLedger(LocalRetailCatalogContext databases)
    {
        _databases = databases;
    }

    public Task AddAsync(ProductRollout rollout, CancellationToken cancellationToken)
    {
        _databases.Rollouts.Add(rollout);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ProductRollout>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<ProductRollout>>(_databases.Rollouts.OrderBy(rollout => rollout.ScheduledFor).ToList());

    public Task<IReadOnlyCollection<ProductRollout>> ClaimDueAsync(
        DateTimeOffset now,
        string claimedBy,
        TimeSpan staleClaimAge,
        CancellationToken cancellationToken)
    {
        var rollouts = _databases.Rollouts
            .Where(rollout => rollout.IsDue(now) || rollout.HasStaleClaim(now, staleClaimAge))
            .OrderBy(rollout => rollout.ScheduledFor)
            .ToList();

        foreach (var rollout in rollouts)
        {
            rollout.MarkPublishing(now, claimedBy);
        }

        return Task.FromResult<IReadOnlyCollection<ProductRollout>>(rollouts);
    }

    public Task UpdateAsync(ProductRollout rollout, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
