# GymAlternatief

Mobile-first portfolio-webapp voor beginnende sporters die een passend alternatief nodig hebben wanneer fitnessapparatuur bezet, kapot of afwezig is.

Deze eerste foundation bevat de monorepo, lokale Aspire-orchestratie, PostgreSQL, de .NET API, de React-app, testtooling, repositoryregels en architectuurdiagrammen. Catalogus-, account-, workout- en adminfeatures volgen per vertical slice.

## Stack

- `apps/web`: React 19, Vite, TypeScript, React Router, TanStack Query, React Hook Form, Zod, Tailwind CSS en shadcn/ui.
- `services/api`: ASP.NET Core 10, EF Core, Npgsql, Identity, JWT, OpenAPI en Scalar.
- `services/migrations`: aparte productiemigratietool; migrations draaien niet tijdens API-startup.
- `service-defaults`: health checks, service discovery, HTTP resilience en OpenTelemetry.
- `apphost.cs`: Aspire 13.4.6 voor uitsluitend lokale orchestration.
- Tests: xUnit, Vitest, Testing Library en Playwright.

## Lokaal starten

Vereisten: .NET 10 SDK, Node.js, npm, Aspire CLI 13.4 en Docker Desktop.

```powershell
npm install
dotnet restore GymAlternatief.slnx
aspire start --non-interactive
aspire wait gymalternatief --non-interactive
aspire wait api --non-interactive
aspire wait web --non-interactive
```

Gebruik `aspire describe --non-interactive` om het actuele API-endpoint te vinden. Open tijdens
development `/scalar` op dat API-adres voor de interactieve API-documentatie; het gegenereerde
OpenAPI-document staat op `/openapi/v1.json`. Stop de omgeving met:

```powershell
aspire stop --non-interactive
```

## Valideren

```powershell
dotnet build GymAlternatief.slnx
dotnet test GymAlternatief.slnx --no-build
npm run build:web
npm run lint:web
npm run test:web
npx markdownlint-cli2
npm audit --omit=dev
```

## Architectuur

- [System context](docs/architecture/system-context.md)
- [Container- en moduleview](docs/architecture/containers.md)
- [Domeinmodel](docs/architecture/domain-model.md)
- [Cold-startsequence](docs/architecture/cold-start-sequence.md)
- [Alternativescoring](docs/architecture/alternative-scoring.md)

De productietopologie is Cloudflare Pages → Render API → Neon PostgreSQL, met Supabase Storage voor gelicentieerde assets. De Aspire AppHost wordt niet gedeployed.

## Repositoryregels

Begin bij [AGENTS.md](AGENTS.md). De onderliggende regels staan in `docs/rules` en behandelen architectuur, backend, frontend, content/media en testen.

## Gezondheid

- `GET /health/live`: proces leeft; bedoeld voor Render.
- `GET /health/ready`: API en databaseverbinding zijn gereed; bedoeld voor de frontend cold-startflow.

## Bekende beperkingen van deze foundation

- Het domeinmodel en de feature-endpoints zijn nog niet geïmplementeerd.
- De migratietool is geïnitialiseerd maar bevat nog geen migrations of admin-bootstrap.
- De frontend toont alleen statische introductiecontent.
- Productiedeployment en Supabase-uploadflow volgen in een latere fase.

GymAlternatief biedt geen medisch of blessureadvies.
