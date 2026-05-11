using FastFashionCatalogSync.Domain.Catalog;
using FastFashionCatalogSync.Domain.Releases;

namespace FastFashionCatalogSync.Infrastructure.Catalog;

public sealed class LocalRetailCatalogContext
{
    public LocalRetailCatalogContext()
    {
        MerchandisingVersions =
        [
            new CatalogVersion(
                "SS26-DROP-01",
                "Summer Streetwear Drop 01",
                DateTimeOffset.Parse("2026-05-01T08:00:00Z"),
                [
                    new CatalogItemSnapshot("TEE-OVR-001", "Oversized Logo Tee", "Tops", 24.99m, "ZA", true),
                    new CatalogItemSnapshot("DEN-WID-220", "Wide Leg Denim", "Denim", 49.99m, "ZA", true),
                    new CatalogItemSnapshot("DRS-LIN-144", "Linen Blend Dress", "Dresses", 39.99m, "ZA", true)
                ]),
            new CatalogVersion(
                "SS26-DROP-02",
                "Summer Streetwear Drop 02",
                DateTimeOffset.Parse("2026-05-05T08:00:00Z"),
                [
                    new CatalogItemSnapshot("TEE-OVR-001", "Oversized Logo Tee", "Tops", 22.99m, "ZA", true),
                    new CatalogItemSnapshot("DEN-WID-220", "Wide Leg Denim", "Premium Denim", 54.99m, "ZA", true),
                    new CatalogItemSnapshot("JKT-BMB-089", "Cropped Bomber Jacket", "Outerwear", 69.99m, "ZA", true)
                ])
        ];

        OperationalItems =
        [
            new OperationalCatalogItem("TEE-OVR-001", "Oversized Logo Tee", "Tops", 24.99m, "ZA", true, "SS26-DROP-01"),
            new OperationalCatalogItem("DEN-WID-220", "Wide Leg Denim", "Denim", 49.99m, "ZA", true, "SS26-DROP-01"),
            new OperationalCatalogItem("DRS-LIN-144", "Linen Blend Dress", "Dresses", 39.99m, "ZA", true, "SS26-DROP-01")
        ];
    }

    public List<CatalogVersion> MerchandisingVersions { get; }
    public List<OperationalCatalogItem> OperationalItems { get; private set; }
    public List<CatalogRelease> Releases { get; } = [];

    public void PromoteToOperationalCatalog(CatalogVersion version)
    {
        OperationalItems = version.Items
            .Select(item => new OperationalCatalogItem(
                item.Sku,
                item.Name,
                item.Category,
                item.Price,
                item.Region,
                item.IsAvailable,
                version.VersionId))
            .ToList();
    }
}
