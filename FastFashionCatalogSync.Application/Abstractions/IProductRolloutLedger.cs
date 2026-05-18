using FastFashionCatalogSync.Domain.Rollouts;

namespace FastFashionCatalogSync.Application.Abstractions;

public interface IProductRolloutLedger
{
    Task AddAsync(ProductRollout rollout, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductRollout>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductRollout>> ClaimDueAsync(
        DateTimeOffset now,
        string claimedBy,
        TimeSpan staleClaimAge,
        CancellationToken cancellationToken);
    Task UpdateAsync(ProductRollout rollout, CancellationToken cancellationToken);
}
