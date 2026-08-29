# Backend rules

- Organize API code by feature: Auth, Exercises, Muscles, Equipment, Locations, Alternatives, Favorites, WorkoutTemplates, WorkoutSessions, and Admin.
- Return DTOs, never EF Core entities.
- Return RFC 7807 Problem Details for client-visible errors.
- Paginate collection endpoints and validate all request boundaries.
- Use optimistic concurrency for admin-managed records.
- Require the explicit `Admin` role for every admin endpoint.
- Hash refresh tokens, rotate them on refresh, and verify the allowed origin for refresh and logout.
- Do not automatically retry state-changing requests without an idempotency key.
- An equipment type is usable at a location only when at least one corresponding unit has status `Available`.
