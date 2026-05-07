# Retail Catalog Release Pipeline

Retail Catalog Release Pipeline is a reference implementation for a scheduled, auditable data promotion pipeline between two business systems:

- a merchandising database where product catalog versions are authored and approved
- an operational catalog database consumed by stores, ecommerce, and downstream channels

The solution treats catalog data like a controlled release artifact: preview, validate, schedule, publish, and audit.

## Architecture Narrative

A fast-fashion retailer prepares new product drops in a merchandising platform. Those changes should not become visible to stores and ecommerce immediately, because releases are tied to trading windows, campaign launches, regional availability, and operational readiness.

Administrators can preview the delta between the currently operational catalog and an approved merchandising version, schedule the rollout, and rely on the scheduler to publish the approved version at the selected time. At execution time, the system validates that the approved preview still matches the merchandising/operational state. If the underlying change set has drifted, the release is blocked instead of publishing surprise data.

## Technical Highlights

- Version-aware merchandising catalog snapshots
- Structured diff preview for added, removed, price, category, availability, and detail changes
- Preview fingerprint to bind admin approval to a deterministic change set
- Scheduled release aggregate with explicit lifecycle states
- Time-gated execution so releases publish only when due
- Idempotent release handling so a published release is not executed twice
- Execution guard that blocks rollout if the approved change set has drifted since preview
- Separate merchandising and operational catalog abstractions to model cross-database synchronization
- Background worker for scheduled release sweeps
- API endpoints for preview, scheduling, audit listing, and manual execution

## Solution Layout

- `FastFashionCatalogSync.Domain`: catalog and release domain model
- `FastFashionCatalogSync.Application`: preview, scheduling, and release orchestration use cases
- `FastFashionCatalogSync.Infrastructure`: in-memory merchandising/operational database adapters and release ledger
- `FastFashionCatalogSync.Scheduling`: hosted worker that executes due releases
- `FastFashionCatalogSync.Api`: workflow API
- `FastFashionCatalogSync.Tests`: behavioral tests 

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

## Validation

Run the test suite:

```powershell
dotnet test FastFashionCatalogSync.slnx
```
