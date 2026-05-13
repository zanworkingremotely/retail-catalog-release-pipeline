# ADR 0002: Use SQL-Owned Schema With Runtime Adapters

## Status

Accepted

## Context

The release pipeline needs durable audit and scheduling state. The database schema should be managed as a deployable database asset rather than being implicitly owned by application startup.

Entity Framework migrations are useful in many application teams, but enterprise SQL Server estates often prefer DACPAC-style deployment, reviewed SQL scripts, or database projects so schema changes can move through controlled release pipelines.

## Decision

Use SQL scripts in this repository to define the database shape and keep runtime persistence behind application ports.

`ICatalogReleaseLedger` is the application port for release-control persistence. The SQL Server implementation uses explicit SQL through `Microsoft.Data.SqlClient` and maps to the `RetailReleaseControlDb` schema.

The application does not create or migrate tables at startup.

## Consequences

Schema ownership is clear and can later move into a SQL database project or DACPAC pipeline without changing application use cases.

Runtime data access remains replaceable because the application layer depends on interfaces rather than SQL client details.

The trade-off is that local setup requires running schema scripts before using SQL-backed persistence.
