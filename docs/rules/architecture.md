# Architecture rules

- Keep `apps/web`, `services/api`, `services/migrations`, `service-defaults`, and the root `apphost.cs` independently understandable.
- Aspire owns local dependency discovery, startup order, health, and observability. Production targets Cloudflare Pages, Render, Neon, and Supabase Storage directly.
- PostgreSQL is the only relational database. Model multiple gym locations even while the pilot exposes one.
- Use environment variables for deployment configuration. Local defaults may be supplied by Aspire.
- All timestamps are UTC and persistent identifiers are UUIDs.
- Record meaningful architecture changes in `docs/decisions/` before implementation.
- Keep schema changes backwards-compatible and independently deployable from the API.
