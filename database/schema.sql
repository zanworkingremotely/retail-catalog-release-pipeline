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

IF DB_ID(N'RetailRolloutControlDb') IS NULL
BEGIN
    CREATE DATABASE RetailRolloutControlDb;
END
GO

USE RetailMerchandisingDb;
GO

IF OBJECT_ID(N'dbo.MerchandisingVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MerchandisingVersions
    (
        VersionId nvarchar(40) NOT NULL CONSTRAINT PK_MerchandisingVersions PRIMARY KEY,
        Label nvarchar(160) NOT NULL,
        ApprovedAt datetimeoffset NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.MerchandisingVersionItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MerchandisingVersionItems
    (
        VersionId nvarchar(40) NOT NULL,
        Sku nvarchar(40) NOT NULL,
        Region nvarchar(16) NOT NULL,
        Name nvarchar(160) NOT NULL,
        Category nvarchar(80) NOT NULL,
        Price decimal(18, 2) NOT NULL,
        IsAvailable bit NOT NULL,
        CONSTRAINT PK_MerchandisingVersionItems PRIMARY KEY (VersionId, Sku, Region),
        CONSTRAINT FK_MerchandisingVersionItems_MerchandisingVersions FOREIGN KEY (VersionId)
            REFERENCES dbo.MerchandisingVersions (VersionId)
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

USE RetailRolloutControlDb;
GO

IF OBJECT_ID(N'dbo.ProductRollouts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductRollouts
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ProductRollouts PRIMARY KEY,
        MerchandisingVersionId nvarchar(40) NOT NULL,
        PreviewFingerprint char(64) NOT NULL,
        ScheduledFor datetimeoffset NOT NULL,
        RequestedBy nvarchar(256) NOT NULL,
        RequestedAt datetimeoffset NOT NULL,
        Status nvarchar(32) NOT NULL,
        ClaimedAt datetimeoffset NULL,
        ClaimedBy nvarchar(128) NULL,
        ExecutedAt datetimeoffset NULL,
        ExecutionMessage nvarchar(1000) NULL
    );
END
GO

IF COL_LENGTH(N'dbo.ProductRollouts', N'ClaimedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ProductRollouts
        ADD ClaimedAt datetimeoffset NULL;
END
GO

IF COL_LENGTH(N'dbo.ProductRollouts', N'ClaimedBy') IS NULL
BEGIN
    ALTER TABLE dbo.ProductRollouts
        ADD ClaimedBy nvarchar(128) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductRollouts_Status_ScheduledFor')
BEGIN
    CREATE INDEX IX_ProductRollouts_Status_ScheduledFor
        ON dbo.ProductRollouts (Status, ScheduledFor);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductRollouts_Status_ClaimedAt')
BEGIN
    CREATE INDEX IX_ProductRollouts_Status_ClaimedAt
        ON dbo.ProductRollouts (Status, ClaimedAt);
END
GO
