# GymAlternatief repository rules

Read the relevant file in `docs/rules/` before changing that area.

- UI copy is Dutch; code, database identifiers, routes, and API contracts are English.
- Keep the backend a feature-based modular monolith. Do not introduce MediatR or split services without an accepted architecture decision.
- Aspire is local orchestration only. Start the AppHost with `aspire start --non-interactive`; never deploy the AppHost.
- Do not run database migrations during API startup. Production migrations run through `services/migrations` before deployment.
- Exercise catalog data always comes from the API and PostgreSQL. Do not add a static JSON fallback catalog.
- Never commit secrets, production connection strings, access tokens, or storage credentials.
- Preserve planned and performed exercises separately when a workout substitution occurs.
- Run the smallest relevant tests, then the complete build before handing off a phase.

Rules:

- [Architecture](docs/rules/architecture.md)
- [Backend](docs/rules/backend.md)
- [Frontend](docs/rules/frontend.md)
- [Content and media](docs/rules/content-and-media.md)
- [Testing](docs/rules/testing.md)
