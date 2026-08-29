# ADR 0001: Use TanStack Router

## Status

Accepted

## Context

The React and Vite application needs client-side routing. The initial foundation
used React Router, while the project now standardizes on TanStack Router.

## Decision

Use TanStack Router with a code-based, type-safe route tree. Keep TanStack Query
as the separate server-state layer. The static homepage is the index route.

Start with code-based routes because the application currently has one route.
Reconsider file-based route generation when the route tree becomes large enough
for generation to reduce maintenance.

## Consequences

- Route paths and navigation APIs receive compile-time validation.
- New screens must be registered in the TanStack route tree.
- React Router is no longer a frontend dependency.
