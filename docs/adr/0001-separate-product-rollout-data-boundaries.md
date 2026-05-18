# ADR 0001: Separate Catalog Rollout Data Boundaries

## Status

Accepted

## Context

Product changes are authored and approved in a merchandising system, but consumed by operational channels such as stores and ecommerce. Administrators need to preview approved changes, schedule rollout, and rely on the system to publish at the selected time.

If the solution used a single database boundary, it would hide the integration risk that exists in the real business process. The architecture needs to make source, destination, and rollout-control ownership explicit.

## Decision

Use three logical databases:

- `RetailMerchandisingDb` for approved merchandising versions
- `RetailOperationalCatalogDb` for the currently published operational catalog
- `RetailRolloutControlDb` for rollout decisions, preview fingerprints, status, and audit trail

Local development uses Docker SQL Server to keep this boundary visible without requiring external infrastructure.

## Consequences

The rollout pipeline can validate and promote data across clear system boundaries. It also creates a natural place for audit, recovery, retry, and operational reporting.

The trade-off is additional setup and more integration code than a single-database CRUD application. That complexity is intentional because it reflects the business requirement: controlled promotion between systems.
