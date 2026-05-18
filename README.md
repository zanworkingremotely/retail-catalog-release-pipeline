# Retail Product Rollout Pipeline

Retail Product Rollout Pipeline is a backend workflow application for managing scheduled, auditable product rollouts between merchandising and operational retail systems:

- a merchandising database where merchandising versions are authored and approved
- an operational catalog database consumed by stores, ecommerce, and downstream channels

The workflow allows approved product changes to be previewed, validated, scheduled, published, and audited before becoming operationally visible.

## Architecture overview

A fast-fashion retailer prepares new product drops in a merchandising platform. Those changes should not become visible to stores and ecommerce immediately, because rollouts are tied to trading windows, campaign launches, regional availability, and operational readiness.

Administrators can preview the difference between the currently operational catalog and an approved merchandising version, schedule the rollout, and rely on the scheduler to publish the approved version at the selected time. At execution time, the system validates that the approved preview still matches the merchandising/operational state. If the source or operational product data has changed, the rollout is blocked instead of publishing surprise data.

## Key Features

- Version-aware merchandising version snapshots
- Structured diff preview for added, removed, price, category, availability, and detail changes
- Preview fingerprinting to ensure rollouts match the originally approved rollout state
- Scheduled rollout workflow with explicit lifecycle states
- Time-gated execution so rollouts publish only when due
- Idempotent rollout handling so a published rollout is not executed twice
- Rollout validation that blocks rollout if catalog data changes after approval
- SQL-backed rollout tracking and audit history
- Separate merchandising and operational catalog boundaries to simulate cross-system synchronization
- Background worker for processing scheduled rollouts
- API endpoints for preview, scheduling, audit listing, and manual execution

## Project Structure

- `FastFashionCatalogSync.Domain`: merchandising, operational, and rollout domain model
- `FastFashionCatalogSync.Application`: preview, scheduling, and rollout workflow use cases
- `FastFashionCatalogSync.Infrastructure`: local merchandising and operational adapters and SQL Server rollout-control persistence
- `FastFashionCatalogSync.Scheduling`: hosted worker that executes due rollouts
- `FastFashionCatalogSync.Api`: REST API
- `FastFashionCatalogSync.Tests`: workflow and orchestration tests 
- `docs/architecture.md`: logical architecture, data boundaries, and rollout validation
- `docs/adr`: architecture decision records

## Local Setup

The repository includes a local Docker SQL Server setup for simulating the merchandising, operational, and rollout-control databases:

- `RetailMerchandisingDb`
- `RetailOperationalCatalogDb`
- `RetailRolloutControlDb`

Start SQL Server locally:

```powershell
docker compose up -d
```

Create and seed the databases:

```powershell
docker exec -i retail-product-rollout-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "RetailRollout!2026" -C -i /dev/stdin < database/schema.sql
docker exec -i retail-product-rollout-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "RetailRollout!2026" -C -i /dev/stdin < database/seed.sql
```

When `ConnectionStrings:RolloutControl` is configured, the API uses `RetailRolloutControlDb` for scheduled rollout decisions and audit state. The merchandising and operational catalog adapters remain local in this version of the project so the rollout-control boundary can be introduced without changing every persistence concern at once.

## API Workflow

Preview the latest approved merchandising version:

```http
GET /api/product-rollouts/preview/latest
```

Schedule a rollout using the returned fingerprint:

```http
POST /api/product-rollouts/schedule
Content-Type: application/json

{
  "merchandisingVersionId": "SS26-DROP-02",
  "scheduledFor": "2026-05-07T23:00:00Z",
  "requestedBy": "merchandising-admin@retail.com",
  "previewFingerprint": "<fingerprint-from-preview>"
}
```

Inspect rollout audit state:

```http
GET /api/product-rollouts
```

Trigger due rollouts manually for controlled operations or recovery:

```http
POST /api/product-rollouts/execute-due
```

## Running Tests

Run the test suite:

```powershell
dotnet test FastFashionCatalogSync.slnx
```
