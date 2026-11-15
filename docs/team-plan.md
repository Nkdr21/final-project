# Team responsibilities

Participants: Anuarbek Nurkadyr and Daulet Ulgaisyn (group IT-2502), as listed in README. Repository accounts: `Nkdr21` and `Surrare`.
This is a responsibility plan, not proof that each task has been completed.
Actual contributions are evidenced by changes, commits and Pull Requests.

| Participant | Area | Defense topics |
|---|---|---|
| Nkdr21 | Matching, preferences, costs, route scenarios and integration | C# models, pure functions, LINQ, ranking and cost calculations |
| Surrare | Complexes, amenity search/filtering, reviews and related tests | Filtering, grouping, review validation and unit tests |
| Both | Shared models/services, application checks and presentation | Architecture, state management, database and limitations |

Coordinate changes to `HousingService.cs` and shared CSS. Work on separate branches, run `dotnet build` and `dotnet test`, then open a Pull Request for the other participant to review. Both participants must understand the whole application for the defense.

## Modules after the 2026-11-15 rewrite

The code now centres on these areas. The participant column above is the agreed split; confirm the new owners with your partner before the defence.

| Module | Files | Defence topics |
|---|---|---|
| Geo, places and area analysis | `Core/Geo`, `Core/Places` | Pure functions, folds, spatial index |
| Transit and seat model | `Core/Transit` | Fold over stops, function composition, tactics |
| Home matching and planner state | `Core/Matching`, `Web/Services/PlannerState.cs`, `HomeFinder.cs` | Records with `with`, cascading state, validation |
| Open-data services | `Web/Services/OpenData` | HTTP failover, caching, rate limiting |
| UI components and pages | `Web/Components` | Reusable generic components, parameters and callbacks |
| Tests and checks | `tests/Dwellio.Tests`, `scripts/` | xUnit, fakes for HTTP and storage, browser and smoke checks |
