# Frontend rules

- Build mobile-first with semantic HTML and a WCAG 2.2 AA baseline.
- Use React Router for routes, TanStack Query for server state, React Hook Form plus Zod for forms, and Tailwind plus shadcn/ui for UI primitives.
- Never replace the search input value with a suggestion unless the user explicitly selects it.
- Start suggestions after two characters, debounce 300 ms, and show at most eight.
- Cancel stale search requests and never let an older response overwrite the newest query.
- During API warm-up, retain only the newest debounced query and execute it once readiness succeeds.
- Never show an empty result state until the API has returned successfully.
- Keep images dimensioned to prevent layout shift and use lazy loading away from the initial viewport.
- Put `Bekijk alternatieven` directly below `Bekijk uitvoering op YouTube ↗` on exercise details.
