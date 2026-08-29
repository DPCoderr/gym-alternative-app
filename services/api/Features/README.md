# API feature folders

Create implementation files under one of these feature areas:

- `Auth`
- `Exercises`
- `Muscles`
- `Equipment`
- `Locations`
- `Alternatives`
- `Favorites`
- `WorkoutTemplates`
- `WorkoutSessions`
- `Admin`

Keep contracts, endpoints, validation, domain behavior, and persistence configuration close to the owning feature. Shared technical primitives belong outside `Features`; shared business behavior requires an explicit reason.
