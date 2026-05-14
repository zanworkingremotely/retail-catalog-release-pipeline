# Retail Catalog Release Pipeline

Retail Catalog Release Pipeline is a backend workflow application for managing scheduled and auditable catalog promotions between merchandising and operational retail systems:

- a merchandising database where product catalog versions are authored and approved
- an operational catalog database consumed by stores, ecommerce, and downstream channels

The workflow allows approved catalog changes to be previewed, validated, scheduled, published, and audited before becoming operationally visible.

## Architecture overview

A fast-fashion retailer prepares new product drops in a merchandising platform. Those changes should not become visible to stores and ecommerce immediately, because releases are tied to trading windows, campaign launches, regional availability, and operational readiness.

Administrators can preview the difference between the currently operational catalog and an approved merchandising version, schedule the rollout, and rely on the scheduler to publish the approved version at the selected time. At execution time, the system validates that the approved preview still matches the merchandising/operational state. If the underlying catalog state has changed, the release is blocked instead of publishing surprise data.

## Key Features

- Version-aware merchandising catalog snapshots
- Structured diff preview for added, removed, price, category, availability, and detail changes
- Preview fingerprinting to ensure releases match the originally approved catalog state
- Scheduled release workflow with explicit lifecycle states
- Time-gated execution so releases publish only when due
- Idempotent release handling so a published release is not executed twice
- Release validation that blocks rollout if catalog data changes after approval
- SQL-backed release tracking and audit history
- Separate merchandising and operational catalog boundaries to simulate cross-system synchronization
- Background worker for processing scheduled releases
- API endpoints for preview, scheduling, audit listing, and manual execution

## Project Structure

- `FastFashionCatalogSync.Domain`: catalog and release domain model
- `FastFashionCatalogSync.Application`: preview, scheduling, and release orchestration use cases
- `FastFashionCatalogSync.Infrastructure`: local catalog adapters and SQL Server release-control persistence
- `FastFashionCatalogSync.Scheduling`: hosted worker that executes due releases
- `FastFashionCatalogSync.Api`: REST API
- `FastFashionCatalogSync.Tests`: workflow and orchestration tests 
- `docs/architecture.md`: logical architecture, data boundaries, and release validation
- `docs/adr`: architecture decision records

## Local Setup

The repository includes a local Docker SQL Server setup for simulating the merchandising, operational, and release-control databases:

- `RetailMerchandisingDb`
- `RetailOperationalCatalogDb`
- `RetailReleaseControlDb`

Start SQL Server locally:

```powershell
docker compose up -d
```

Create and seed the databases:

```powershell
docker exec -i retail-catalog-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "RetailCatalog!2026" -C -i /dev/stdin < database/schema.sql
docker exec -i retail-catalog-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "RetailCatalog!2026" -C -i /dev/stdin < database/seed.sql
```

When `ConnectionStrings:ReleaseControl` is configured, the API uses `RetailReleaseControlDb` for scheduled release decisions and audit state. The merchandising and operational catalog adapters remain local in this version of the project so the release-control boundary can be introduced without changing every persistence concern at once.

## API Workflow

Preview the latest approved merchandising catalog:

```http
GET /api/catalog-releases/preview/latest
```

Schedule a rollout using the returned fingerprint:

```http
POST /api/catalog-releases/schedule
Content-Type: application/json

{
  "merchandisingVersionId": "SS26-DROP-02",
  "scheduledFor": "2026-05-07T23:00:00Z",
  "requestedBy": "catalog-admin@retail.com",
  "previewFingerprint": "<fingerprint-from-preview>"
}
```

Inspect release audit state:

```http
GET /api/catalog-releases
```

Trigger due releases manually for controlled operations or recovery:

```http
POST /api/catalog-releases/execute-due
```

## Running Tests

Run the test suite:

```powershell
dotnet test FastFashionCatalogSync.slnx
```
