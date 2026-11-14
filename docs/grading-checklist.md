# Grading evidence and remaining work

The supplied final rubric has 20 criteria at 5% each. This checklist records implementation, not an awarded score. An A/100 is not guaranteed.

| Criterion | Evidence | Remaining verification / limitation |
|---|---|---|
| Code structure | Core/Web/Tests separation | Team must explain responsibilities |
| Component reuse | ApartmentCard, TripCard, MapPanel, AmenityGroup | Review component parameters |
| Cross-platform | .NET + browser; CI matrix configured | Windows, Linux and macOS jobs passed in [run 43634904148](https://github.com/Nkdr21/final-project/actions/runs/43634904148); device/browser QA remains separate |
| State management | Scoped SearchState; persisted profiles/favourites | Compare selection resets with new circuit |
| UI design | Coherent responsive English UI and original illustrations | Visual browser QA outstanding |
| UX | Empty/error states; explanations; reversible actions | Complete hands-on user testing |
| Responsiveness | CSS breakpoints for desktop/tablet/mobile | Actual device/browser checks outstanding |
| Core functionality | Catalog, preferences, detail, compare, auth, owner CRUD, reviews | Interactive browser flows need verification |
| Advanced features | Role authorization; personalized scoring; uploads | Live routing not integrated |
| Error handling | Typed route outcomes, validation and unknown labels | More failure/load testing needed |
| Efficiency | Batched catalog fetch, lazy images, no repeated route fetch per listing in catalog | No performance benchmark yet |
| Scalability | Services/contracts and DB indexes | SQLite/small dataset; no load-tested scalability claim |
| Resource utilization | Short-lived contexts, render batches | Catalog capped at 500; production pagination needed |
| Automated testing | Core unit tests and real SQLite integration tests | Browser E2E suite not yet implemented |
| Quality assurance | Build/tests/HTTP smoke checks | Cross-browser and mobile QA not completed |
| Documentation | README, architecture, data model, sources and tests | Keep updated after friend changes |
| Innovation | Lifestyle plus two-direction commute comparison | Demonstrate priority changes clearly |
| Creative C# / FP | Records, pure functions, Func predicates, LINQ, typed outcomes | No unnecessary library abstractions |
| Professionalism | No tracked secrets, accessible labels, clear demo provenance | Not production-ready; email/uploads need hardening |
| Presentation | Demo outline and lecture mapping | Actual slides and rehearsals are later stages |

## Functional C# lecture mapping

Reference supplied by the students: `L6_1_merged (1).pdf`, 1,537 pages. The following page numbers are physical PDF pages, not slide numbers.

| Topic | Lecture location | Dwellio evidence | Explanation |
|---|---|---|---|
| Pure functions | 3, 6 | RecommendationEngine, NeighborhoodService | Results depend on explicit arguments, without database calls |
| FP with OOP | 9 | Core functions plus HousingService | Functional calculations inside an ordinary C# application |
| Records / immutable updates | 38–47 | Preferences and UI `with` updates | New preference value rather than hidden mutation |
| Higher-order functions | 48–95 | FitsBudget returns Func; LINQ Where accepts it | Pass a rule as data |
| LINQ / declarative transformations | 53, 95 | Filter, Select, OrderBy, All, Any | Describe the selected result |
| Discriminated alternatives | 96 onward | RoutesFound / RoutesMissing / RoutesUnavailable | Different outcomes carry different information |
| Pattern matching | 41–42 | RouteResult and Crowding switches | Handle a specific case explicitly |
| Functional composition | 248 onward | Validate → filter → evaluate → sort | Small steps form a pipeline; no claim to implement a full monad library |
| Testing external boundaries | 1458–1460, 1528–1529 | Route interface; in-memory SQLite tests | Core is deterministic; effects are tested separately |

Not every advanced lecture concept is forced into the project. Currying libraries, recursion and memoization are not required for this small dataset. Explain why each used technique helps rather than reciting terminology.
