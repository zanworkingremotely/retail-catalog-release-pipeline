USE RetailMerchandisingDb;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CatalogVersions WHERE VersionId = N'SS26-DROP-01')
BEGIN
    INSERT INTO dbo.CatalogVersions (VersionId, Label, ApprovedAt)
    VALUES
        (N'SS26-DROP-01', N'Summer Streetwear Drop 01', '2026-05-01T08:00:00+00:00'),
        (N'SS26-DROP-02', N'Summer Streetwear Drop 02', '2026-05-05T08:00:00+00:00');

    INSERT INTO dbo.CatalogVersionItems (VersionId, Sku, Region, Name, Category, Price, IsAvailable)
    VALUES
        (N'SS26-DROP-01', N'TEE-OVR-001', N'ZA', N'Oversized Logo Tee', N'Tops', 24.99, 1),
        (N'SS26-DROP-01', N'DEN-WID-220', N'ZA', N'Wide Leg Denim', N'Denim', 49.99, 1),
        (N'SS26-DROP-01', N'DRS-LIN-144', N'ZA', N'Linen Blend Dress', N'Dresses', 39.99, 1),
        (N'SS26-DROP-02', N'TEE-OVR-001', N'ZA', N'Oversized Logo Tee', N'Tops', 22.99, 1),
        (N'SS26-DROP-02', N'DEN-WID-220', N'ZA', N'Wide Leg Denim', N'Premium Denim', 54.99, 1),
        (N'SS26-DROP-02', N'JKT-BMB-089', N'ZA', N'Cropped Bomber Jacket', N'Outerwear', 69.99, 1);
END
GO

USE RetailOperationalCatalogDb;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.OperationalCatalogItems WHERE PublishedVersionId = N'SS26-DROP-01')
BEGIN
    INSERT INTO dbo.OperationalCatalogItems (Sku, Region, Name, Category, Price, IsAvailable, PublishedVersionId, PublishedAt)
    VALUES
        (N'TEE-OVR-001', N'ZA', N'Oversized Logo Tee', N'Tops', 24.99, 1, N'SS26-DROP-01', '2026-05-01T09:00:00+00:00'),
        (N'DEN-WID-220', N'ZA', N'Wide Leg Denim', N'Denim', 49.99, 1, N'SS26-DROP-01', '2026-05-01T09:00:00+00:00'),
        (N'DRS-LIN-144', N'ZA', N'Linen Blend Dress', N'Dresses', 39.99, 1, N'SS26-DROP-01', '2026-05-01T09:00:00+00:00');
END
GO
