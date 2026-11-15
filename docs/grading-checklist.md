# Grading evidence

The rubric has 20 criteria at 5% each. This records what is implemented and where; it is not an awarded score.

| Criterion | Evidence | Limitation |
|---|---|---|
| Code structure | Core (pure, no EF/HTTP) / Web (effects) / Tests; folders by domain: Geo, Places, Transit, Matching | — |
| Component reuse | `LiveMap`, `PlaceSearch`, `NeedPicker`, generic `Segmented<TValue>`, templated generic `BarChart<TItem>`, `HomeCard`, `AreaCard`, `JourneyOptions`, `NeedReport`, `SeatMeter` — parameters + `EventCallback` | — |
| Cross-platform | .NET 10 + browser; CI on Windows, Linux, macOS; Leaflet bundled locally | Physical devices not tested |
| State management | `PlannerState` container with pure `Update(Func<Preferences,Preferences>)`, `Changed` event, cascading `Preferences` via `PlannerScope`, localStorage/sessionStorage/profile persistence | Comparison is per tab |
| UI design | New design system (`app.css`): tokens, cards, chips, segmented controls, maps, charts | — |
| UX | One-page planner with instant results, first-visit hint, explanations ("why it doesn't fit"), empty/error states, keyboard combobox | No user study |
| Responsiveness | Mobile-first CSS, bottom tab bar on phones, collapsible plan panel, sticky panels on desktop | Emulated only |
| Core functionality | Destination search → needs → homes/areas → seat-aware commute → compare/save; owner CRUD; reviews; moderation | — |
| Advanced features | Live OpenStreetMap (Overpass) with mirror failover and SQLite cache; geocoding; GPS-based seat model; journey tactics; area search; roles | Seat chance is a model |
| Error handling | Typed outcomes (`CommutePlan` cases), validation + sanitising, safe URL parsing, `ErrorBoundary`, offline fallbacks | — |
| Efficiency | Spatial grid index, per-complex memoisation in `IMemoryCache`, background computation, cancellation of stale work, lazy images, no prerender double work | No formal benchmark |
| Scalability | Immutable shared index (lock-free reads), cache keyed by plan, tile-based live refresh shared between users | SQLite, single server |
| Resource utilization | One Overpass request per ~1 km tile per 72 h shared by all users; Nominatim rate gate; `IHttpClientFactory` | — |
| Automated testing | 98 xUnit tests (unit, SQLite integration, fake HTTP, fake browser storage, real data) + HTTP smoke + browser audit in CI | Signed-in browser flows not automated |
| Quality assurance | Browser audit passes in Chromium (desktop and phone), Firefox; Lighthouse accessibility 1.0 on the planner (mobile and desktop); CI browser job | Manual cross-browser QA; WebKit not run |
| Documentation | README, architecture, data model, data sources, calculations, testing, presentation material, defence guide, user guide in README | — |
| Innovation | Seat chance from GPS dwell times; "walk back to an emptier stop" and "leave earlier" tactics; best-area search | — |
| Creative C# / FP | Records + `with`, `ImmutableArray`, folds (`Aggregate`) for bus load and area selection, function composition (`Combine`), pattern matching on plan cases, `FrozenDictionary`, `ILookup` index, generic/templated components | — |
| Professionalism | Data provenance on every card/map, licences attributed, no keys or secrets, polite API use | Not production-deployed |
| Presentation | `presentation.md` (aim, goals, technology comparison, UML, architecture, methodology); `defense.md` demo script | Slides and rehearsal are yours |

## Functional C# lecture mapping

Reference supplied by the students: `L6_1_merged (1).pdf`, 1,537 pages (physical PDF page numbers).

| Topic | Lecture pages | Dwellio evidence |
|---|---|---|
| Pure functions | 3, 6 | `HomeMatcher.Evaluate`, `SeatModel.Profile`, `JourneyPlanner.Plan`, `AreaAnalysis.Analyse` — results depend only on arguments |
| FP with OOP | 9 | Pure Core used by ordinary ASP.NET services (`HomeFinder`, `PlaceService`) |
| Records / immutable updates | 38–47 | `Preferences` updated with `with` in every input handler; `PlaceIndex.ReplaceArea` returns a new index |
| Higher-order functions | 48–95 | `PlannerState.Update(Func<Preferences, Preferences>)`; `JourneyPlanner.Combine` returns a scoring function; `BarChart` takes `Func<TItem,double>` |
| LINQ | 53, 95 | Query syntax in `PlaceIndex.Near`, `AreaSearch.Grid`, `RouteOptions` |
| Folds | — | `SeatModel.Profile` (load along the route), `AreaSearch.Best` (greedy selection), tactic filtering in `JourneyPlanner.Options` |
| Discriminated alternatives | 96 onward | `CommutePlan` = `TransitPlan` / `EstimatedBusPlan` / `WalkPlan` / `NoCommute` |
| Pattern matching | 41–42 | `switch` on plan cases (`CommuteSummary`, `HomeMatcher.CommuteScore`), relational patterns in validation and labels |
| Composition | 248 onward | Validate → analyse → plan → evaluate → rank pipeline |
| Testing effects at the edge | 1458–1460, 1528–1529 | Core tested directly; HTTP and browser storage replaced by fakes in tests |
