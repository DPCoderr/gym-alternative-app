# Testing rules

- End every delivery phase with a green build, relevant automated tests, and a manually verifiable vertical slice.
- Use xUnit for backend tests, Testcontainers with real PostgreSQL for integration tests, Vitest and Testing Library for components, and Playwright for end-to-end flows.
- Test alternatives scoring, overrides, equipment availability, authorization, token rotation, validation, pagination, and Problem Details.
- Assert that a session-only substitution does not alter its workout template.
- Assert cold-start timing states and that only the latest queued search runs after readiness.
- Use Aspire-discovered endpoints for end-to-end tests; do not hard-code local ports.
