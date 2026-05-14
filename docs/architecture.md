# Architecture

## Business Capability

The release pipeline controls when approved merchandising catalog changes become visible in the operational retail catalog used by stores and ecommerce channels.

The core business outcome is controlled promotion:

1. Preview the difference between the approved merchandising version and the current operational catalog.
2. Bind approval to the previewed change set with a preview fingerprint.
3. Schedule the release for a business-approved time.
4. Execute due releases through a background worker.
5. Record release status for audit and operational support.

## Logical Components

```mermaid
flowchart LR
    Admin["Catalog Administrator"]
    Api["Release API"]
    App["Release Orchestrator"]
    Preview["Preview Builder"]
    Worker["Scheduled Worker"]
    MerchDb[("RetailMerchandisingDb")]
    OpsDb[("RetailOperationalCatalogDb")]
    ControlDb[("RetailReleaseControlDb")]

    Admin --> Api
    Api --> Preview
    Api --> App
    Worker --> App
    Preview --> MerchDb
    Preview --> OpsDb
    App --> ControlDb
    App --> MerchDb
    App --> OpsDb
```

## Data Boundaries

`RetailMerchandisingDb` owns approved catalog versions. It represents the source-of-truth system where product drops are prepared and approved.

`RetailOperationalCatalogDb` owns the catalog currently visible to stores and ecommerce channels. This database should change only through controlled release execution.

`RetailReleaseControlDb` owns scheduled release decisions, preview fingerprints, execution timestamps, status, and failure messages. The application uses a SQL-backed release ledger for this boundary when the `ReleaseControl` connection string is configured.

## Execution Guard

The preview fingerprint protects against drift between approval and execution. If the merchandising version or operational catalog changes after preview, the scheduled release is blocked instead of publishing an unapproved change set.

## Current Implementation Stage

The application contracts already model the target boundaries. The release-control boundary now has a SQL Server implementation. The merchandising and operational catalog adapters remain local so the business workflow can evolve in focused increments.

The repository includes Docker SQL Server, schema scripts, and seed data for local development.

## Worker Concurrency

Due releases are claimed by the release ledger before catalog data is published. The SQL Server ledger atomically moves eligible rows from `Scheduled` to `Publishing` with update locks and `READPAST`, so multiple worker instances can sweep for due releases without publishing the same scheduled release twice.

A production deployment should still add stale-claim recovery for releases that remain in `Publishing` after a worker crash.
