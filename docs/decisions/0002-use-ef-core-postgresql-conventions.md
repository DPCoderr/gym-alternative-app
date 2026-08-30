# ADR 0002: Use shared EF Core PostgreSQL conventions

## Status

Accepted

## Context

All API features will persist data in PostgreSQL. They need one consistent mapping for UUID
identifiers, UTC timestamps, and optimistic concurrency before domain entities and migrations are
introduced. Local development receives the database connection from Aspire, while production uses
the same named connection string for Neon.

## Decision

Use one `AppDbContext` registered through Aspire's PostgreSQL EF Core client integration with the
connection name `gymalternatief`.

Shared pre-conventions map `Guid` properties to `uuid` and `DateTime` properties to
`timestamp with time zone`. EF Core's normal `Id` naming convention remains responsible for primary
key discovery. Admin-managed entities explicitly configure a `uint` version property with
`IsPostgresConcurrencyToken()`, which Npgsql maps to PostgreSQL's system `xmin` column. Npgsql rejects
non-UTC `DateTime` values written to `timestamp with time zone`.

### What the concurrency version protects

The concurrency version belongs to one database record. It is not the PostgreSQL software version
and is unrelated to Git version control. For example, two admins can open the same equipment record
with version `10`. After the first admin saves, PostgreSQL changes that record's version. The second
admin can no longer save the stale version `10`; EF Core reports a concurrency conflict instead of
silently overwriting the first admin's change.

The API never creates, migrates, or seeds the database during startup. Schema migrations remain an
explicit responsibility of `services/migrations`.

## Consequences

- New feature entities inherit the UUID and timestamp mappings automatically and opt into
  concurrency explicitly in their focused entity configuration.
- Admin update contracts must expose the concurrency value as an opaque token rather than leaking
  PostgreSQL's `xmin` representation.
- Code that persists timestamps must supply UTC values; date-only concepts use `DateOnly` instead.
- Aspire and Neon use the same `ConnectionStrings:gymalternatief` configuration boundary.
