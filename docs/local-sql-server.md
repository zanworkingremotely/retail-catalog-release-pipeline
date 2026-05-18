# Local SQL Server

The local environment uses SQL Server Developer Edition in Docker so the repository reflects the intended production shape: separate persistence boundaries for merchandising, operational catalog, and rollout control data.

## Databases

- `RetailMerchandisingDb`: approved merchandising versions from the merchandising system
- `RetailOperationalCatalogDb`: currently published catalog consumed by store and ecommerce channels
- `RetailRolloutControlDb`: scheduled rollout decisions, fingerprints, execution status, and audit trail

## Start SQL Server

```powershell
docker compose up -d
```

The SQL Server container listens on local port `14333`.

## Create Schema

After the container is healthy, run:

```powershell
docker exec -i retail-product-rollout-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "RetailRollout!2026" -C -i /dev/stdin < database/schema.sql
docker exec -i retail-product-rollout-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "RetailRollout!2026" -C -i /dev/stdin < database/seed.sql
```

## Connection Strings

```text
Server=localhost,14333;Database=RetailMerchandisingDb;User Id=sa;Password=RetailRollout!2026;TrustServerCertificate=True
Server=localhost,14333;Database=RetailOperationalCatalogDb;User Id=sa;Password=RetailRollout!2026;TrustServerCertificate=True
Server=localhost,14333;Database=RetailRolloutControlDb;User Id=sa;Password=RetailRollout!2026;TrustServerCertificate=True
```

When `ConnectionStrings:RolloutControl` is configured, the API persists scheduled rollouts to `RetailRolloutControlDb`.

The merchandising and operational catalog adapters are still local in this checkpoint. That keeps the implementation incremental: rollout-control durability is introduced first because scheduled decisions and audit state must survive process restarts.
