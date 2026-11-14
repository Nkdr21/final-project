# Architecture

Dwellio uses a functional core surrounded by database and UI adapters. Core calculations never read the clock, fetch HTTP data or change database records. This allows deterministic tests and clear explanations during the defence.

```mermaid
flowchart LR
    UI[Blazor pages and shared components] --> State[Scoped SearchState]
    UI --> Service[HousingService]
    Service --> Auth[Identity and server role checks]
    Service --> DB[(SQLite housing data)]
    UI --> Route[IRouteProvider]
    Route --> Demo[Explicit demo provider]
    UI --> Core[Pure recommendation and cost functions]
    Core --> Model[Immutable records]
```

## Responsibilities

- `Dwellio.Core/Models/Housing.cs`: values and explicit route outcomes.
- `RecommendationEngine`: validation, mandatory filters, route selection, scoring and cost calculation.
- `NeighborhoodService`: reusable pure amenity filtering/grouping.
- `DemoRouteProvider`: deterministic teaching scenarios behind an interface; replace with a researched real provider later.
- `HousingService`: authorization, validation and persistence. Never trusts a user ID supplied by a browser form.
- `SearchState`: per-circuit preferences and comparison IDs. No cross-user singleton state.
- `IDbContextFactory`: short-lived contexts per database operation, rather than a long-lived tracked context per circuit.
- Identity: framework password hashing, cookies, antiforgery and lockout on login failures.

## Tradeoffs

SQLite and a single application process keep setup understandable. This is not a claim of production scalability. Catalog loads at most 500 rows, computes in memory and displays batches of nine; larger datasets need server pagination and query optimization. Current maps are embedded OpenStreetMap views, not a routing service. Uploads are limited to 5 MB and basic JPEG/PNG signature checks, with randomized filenames; production needs image decoding/re-encoding, storage quotas and orphan cleanup.
