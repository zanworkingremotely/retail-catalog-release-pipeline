namespace FastFashionCatalogSync.Domain.Rollouts;

public enum ProductRolloutStatus
{
    Scheduled,
    Publishing,
    Published,
    Blocked,
    Failed
}
