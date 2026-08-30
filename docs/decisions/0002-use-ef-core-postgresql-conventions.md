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

Persistent entities opt into shared marker interfaces for a UUID primary key, UTC audit timestamps,
and an opaque PostgreSQL concurrency token. The concurrency token is a `uint` row version that Npgsql
maps to PostgreSQL's system `xmin` column. All `DateTime` and `DateTimeOffset` properties map to
`timestamp with time zone`; writes reject values that are not UTC.

The API never creates, migrates, or seeds the database during startup. Schema migrations remain an
explicit responsibility of `services/migrations`.

## Consequences

- New feature entities reuse the same key, timestamp, and concurrency behavior without duplicating
  provider configuration.
- Admin update contracts must expose the concurrency value as an opaque token rather than leaking
  PostgreSQL's `xmin` representation.
- Code that persists timestamps must supply UTC values; date-only concepts use `DateOnly` instead.
- Aspire and Neon use the same `ConnectionStrings:gymalternatief` configuration boundary.
