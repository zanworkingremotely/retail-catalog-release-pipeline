# Local SQL Server

The local environment uses SQL Server Developer Edition in Docker so the repository reflects the intended production shape: separate persistence boundaries for merchandising, operational catalog, and release control data.

## Databases

- `RetailMerchandisingDb`: approved catalog versions from the merchandising system
- `RetailOperationalCatalogDb`: currently published catalog consumed by store and ecommerce channels
- `RetailReleaseControlDb`: scheduled release decisions, fingerprints, execution status, and audit trail

## Start SQL Server

```powershell
docker compose up -d
```

The SQL Server container listens on local port `14333`.

## Create Schema

After the container is healthy, run:

```powershell
docker exec -i retail-catalog-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "RetailCatalog!2026" -C -i /dev/stdin < database/schema.sql
docker exec -i retail-catalog-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "RetailCatalog!2026" -C -i /dev/stdin < database/seed.sql
```

## Connection Strings

```text
Server=localhost,14333;Database=RetailMerchandisingDb;User Id=sa;Password=RetailCatalog!2026;TrustServerCertificate=True
Server=localhost,14333;Database=RetailOperationalCatalogDb;User Id=sa;Password=RetailCatalog!2026;TrustServerCertificate=True
Server=localhost,14333;Database=RetailReleaseControlDb;User Id=sa;Password=RetailCatalog!2026;TrustServerCertificate=True
```

When `ConnectionStrings:ReleaseControl` is configured, the API persists scheduled releases to `RetailReleaseControlDb`.

The merchandising and operational catalog adapters are still local in this checkpoint. That keeps the implementation incremental: release-control durability is introduced first because scheduled decisions and audit state must survive process restarts.
