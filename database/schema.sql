IF DB_ID(N'RetailMerchandisingDb') IS NULL
BEGIN
    CREATE DATABASE RetailMerchandisingDb;
END
GO

IF DB_ID(N'RetailOperationalCatalogDb') IS NULL
BEGIN
    CREATE DATABASE RetailOperationalCatalogDb;
END
GO

IF DB_ID(N'RetailReleaseControlDb') IS NULL
BEGIN
    CREATE DATABASE RetailReleaseControlDb;
END
GO

USE RetailMerchandisingDb;
GO

IF OBJECT_ID(N'dbo.CatalogVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogVersions
    (
        VersionId nvarchar(40) NOT NULL CONSTRAINT PK_CatalogVersions PRIMARY KEY,
        Label nvarchar(160) NOT NULL,
        ApprovedAt datetimeoffset NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.CatalogVersionItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogVersionItems
    (
        VersionId nvarchar(40) NOT NULL,
        Sku nvarchar(40) NOT NULL,
        Region nvarchar(16) NOT NULL,
        Name nvarchar(160) NOT NULL,
        Category nvarchar(80) NOT NULL,
        Price decimal(18, 2) NOT NULL,
        IsAvailable bit NOT NULL,
        CONSTRAINT PK_CatalogVersionItems PRIMARY KEY (VersionId, Sku, Region),
        CONSTRAINT FK_CatalogVersionItems_CatalogVersions FOREIGN KEY (VersionId)
            REFERENCES dbo.CatalogVersions (VersionId)
    );
END
GO

USE RetailOperationalCatalogDb;
GO

IF OBJECT_ID(N'dbo.OperationalCatalogItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperationalCatalogItems
    (
        Sku nvarchar(40) NOT NULL,
        Region nvarchar(16) NOT NULL,
        Name nvarchar(160) NOT NULL,
        Category nvarchar(80) NOT NULL,
        Price decimal(18, 2) NOT NULL,
        IsAvailable bit NOT NULL,
        PublishedVersionId nvarchar(40) NOT NULL,
        PublishedAt datetimeoffset NOT NULL,
        CONSTRAINT PK_OperationalCatalogItems PRIMARY KEY (Sku, Region)
    );
END
GO

USE RetailReleaseControlDb;
GO

IF OBJECT_ID(N'dbo.CatalogReleases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogReleases
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_CatalogReleases PRIMARY KEY,
        MerchandisingVersionId nvarchar(40) NOT NULL,
        PreviewFingerprint char(64) NOT NULL,
        ScheduledFor datetimeoffset NOT NULL,
        RequestedBy nvarchar(256) NOT NULL,
        RequestedAt datetimeoffset NOT NULL,
        Status nvarchar(32) NOT NULL,
        ExecutedAt datetimeoffset NULL,
        ExecutionMessage nvarchar(1000) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CatalogReleases_Status_ScheduledFor')
BEGIN
    CREATE INDEX IX_CatalogReleases_Status_ScheduledFor
        ON dbo.CatalogReleases (Status, ScheduledFor);
END
GO
