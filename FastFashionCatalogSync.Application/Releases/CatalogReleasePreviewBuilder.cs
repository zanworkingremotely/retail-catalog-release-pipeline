using System.Security.Cryptography;
using System.Text;
using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Catalog;
using FastFashionCatalogSync.Domain.Releases;

namespace FastFashionCatalogSync.Application.Releases;

public sealed class CatalogReleasePreviewBuilder
{
    private readonly IMerchandisingCatalogReader _merchandisingCatalog;
    private readonly IOperationalCatalogReader _operationalCatalog;

    public CatalogReleasePreviewBuilder(IMerchandisingCatalogReader merchandisingCatalog, IOperationalCatalogReader operationalCatalog)
    {
        _merchandisingCatalog = merchandisingCatalog;
        _operationalCatalog = operationalCatalog;
    }

    public async Task<CatalogReleasePreview> PreviewLatestApprovedAsync(CancellationToken cancellationToken)
    {
        var version = await _merchandisingCatalog.GetLatestApprovedVersionAsync(cancellationToken);
        return await PreviewVersionAsync(version.VersionId, cancellationToken);
    }

    public async Task<CatalogReleasePreview> PreviewVersionAsync(string merchandisingVersionId, CancellationToken cancellationToken)
    {
        var merchandisingVersion = await _merchandisingCatalog.GetApprovedVersionAsync(merchandisingVersionId, cancellationToken);
        var operationalItems = await _operationalCatalog.GetOperationalItemsAsync(cancellationToken);
        var operationalVersion = await _operationalCatalog.GetCurrentOperationalVersionIdAsync(cancellationToken);
        var changes = BuildChanges(merchandisingVersion.Items, operationalItems);
        var fingerprint = BuildFingerprint(merchandisingVersion.VersionId, operationalVersion, changes);

        return new CatalogReleasePreview(merchandisingVersion.VersionId, operationalVersion, fingerprint, changes);
    }

    private static IReadOnlyCollection<CatalogChange> BuildChanges(
        IReadOnlyCollection<CatalogItemSnapshot> merchandisingItems,
        IReadOnlyCollection<OperationalCatalogItem> operationalItems)
    {
        var merchandisingByKey = merchandisingItems.ToDictionary(item => Key(item.Sku, item.Region));
        var operationalByKey = operationalItems.ToDictionary(item => Key(item.Sku, item.Region));
        var changes = new List<CatalogChange>();

        foreach (var merchandisingItem in merchandisingItems.OrderBy(item => item.Sku).ThenBy(item => item.Region))
        {
            if (!operationalByKey.TryGetValue(Key(merchandisingItem.Sku, merchandisingItem.Region), out var operationalItem))
            {
                changes.Add(new CatalogChange(CatalogChangeType.Added, merchandisingItem.Sku, merchandisingItem.Region, "item", null, merchandisingItem.Name));
                continue;
            }

            AddIfChanged(changes, CatalogChangeType.DetailsChanged, merchandisingItem.Sku, merchandisingItem.Region, "name", operationalItem.Name, merchandisingItem.Name);
            AddIfChanged(changes, CatalogChangeType.CategoryChanged, merchandisingItem.Sku, merchandisingItem.Region, "category", operationalItem.Category, merchandisingItem.Category);
            AddIfChanged(changes, CatalogChangeType.PriceChanged, merchandisingItem.Sku, merchandisingItem.Region, "price", operationalItem.Price.ToString("0.00"), merchandisingItem.Price.ToString("0.00"));
            AddIfChanged(changes, CatalogChangeType.AvailabilityChanged, merchandisingItem.Sku, merchandisingItem.Region, "availability", operationalItem.IsAvailable.ToString(), merchandisingItem.IsAvailable.ToString());
        }

        foreach (var operationalItem in operationalItems.OrderBy(item => item.Sku).ThenBy(item => item.Region))
        {
            if (!merchandisingByKey.ContainsKey(Key(operationalItem.Sku, operationalItem.Region)))
            {
                changes.Add(new CatalogChange(CatalogChangeType.Removed, operationalItem.Sku, operationalItem.Region, "item", operationalItem.Name, null));
            }
        }

        return changes;
    }

    private static void AddIfChanged(
        ICollection<CatalogChange> changes,
        CatalogChangeType type,
        string sku,
        string region,
        string field,
        string currentValue,
        string proposedValue)
    {
        if (!StringComparer.Ordinal.Equals(currentValue, proposedValue))
        {
            changes.Add(new CatalogChange(type, sku, region, field, currentValue, proposedValue));
        }
    }

    private static string BuildFingerprint(string merchandisingVersionId, string operationalVersionId, IEnumerable<CatalogChange> changes)
    {
        var canonical = new StringBuilder()
            .Append(merchandisingVersionId)
            .Append('|')
            .Append(operationalVersionId);

        foreach (var change in changes.OrderBy(change => change.Sku).ThenBy(change => change.Region).ThenBy(change => change.Field))
        {
            canonical
                .Append('|')
                .Append(change.Type)
                .Append(':')
                .Append(change.Sku)
                .Append(':')
                .Append(change.Region)
                .Append(':')
                .Append(change.Field)
                .Append(':')
                .Append(change.CurrentValue)
                .Append("->")
                .Append(change.ProposedValue);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static string Key(string sku, string region) => $"{sku}::{region}";
}
