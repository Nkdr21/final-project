# Defence guide

Use the running app and these source files together. This is preparation material, not a claim that either participant has already mastered every topic. All routes below are relative to `http://localhost:5180`.

## Evidence for the 20 criteria

| Criterion | Where to look | What to demonstrate | Why this design / limitation |
|---|---|---|---|
| 1. Code structure | `src/Dwellio.Core`, `src/Dwellio.Web`, `tests/Dwellio.Tests` | Open the Core project file: no EF reference | Calculations can run without the database |
| 2. Reuse | `Components/Shared/ApartmentCard.razor` | Same card on Home, Catalog and Favorites | Parameters provide data; callbacks report user actions |
| 3. Cross-platform | `.github/workflows/dotnet.yml` | Open successful Windows/Linux/macOS jobs | .NET and a browser; real phone QA is separate |
| 4. State | `Services/SearchState.cs`, `wwwroot/comparison.js` | Select two demo homes, open Compare, reload | IDs persist in same-tab sessionStorage; profiles use SQLite |
| 5. Design | `wwwroot/app.css`, screenshots | Desktop/mobile catalog | Blue actions, neutral backgrounds and a card grid keep rent and filters easy to scan |
| 6. UX | `/catalog`, `/preferences` | Search a nonexistent complex, correct an invalid budget | Explain empty results and errors instead of silently changing requirements |
| 7. Responsive layout | CSS media queries | Resize to 390 px and inspect cards/navigation | Browser emulation is not physical-device testing |
| 8. Main workflow | `/catalog?demo=true` | Preferences → matches → apartment reasons → Compare | Sourced rentals and synthetic matching have different data provenance |
| 9. Advanced features | Identity, HousingService, Compare, MapPanel | Show roles, saved data, map and two-way trips | Map/route examples are not live bus predictions |
| 10. Errors | RecommendationEngine.Validate, MarketCatalog, TripCard | Negative budget; unavailable provider; no results | Unknown data stays unknown |
| 11. Efficiency | HousingService.Listings, Catalog.Recalculate | Show one route request per complex and lazy images | No production benchmark has been performed |
| 12. Scalability | HousingService.Listings | Point to Take(500) | 10,000 listings require SQL filtering, pagination and measured indexes |
| 13. Resources | DbContext factory; catalog render batches | Show short-lived contexts and Load more | Rendering more cards does not remove the 500-row retrieval limit |
| 14. Automated tests | `tests/Dwellio.Tests`, `scripts/browser-audit.mjs` | Run dotnet test and the browser audit | C# unit/SQLite tests plus a limited Chrome flow suite |
| 15. QA | `docs/testing.md` | Show actual test and Lighthouse results | One page's automated score does not certify the whole app |
| 16. Documentation | README and docs | Follow setup and architecture links | Keep instructions tied to tested behavior |
| 17. Innovation | RecommendationEngine.Evaluate | Change price vs neighbourhood weights | Lifestyle and return journey matter alongside rent |
| 18. C# / FP | Housing.cs, RecommendationEngine, NeighborhoodService | Find record, with, Func, LINQ and pattern matching | Pure calculations accept external data as arguments |
| 19. Professionalism | Data sources, validation, auth, testing | Explain sources and deployment limits | No claim of verified availability or production readiness |
| 20. Presentation | This file and team-plan.md | Rehearse a 15–20 minute joint demonstration | Slides/rehearsals are not completed by writing this guide |

## Concrete demonstration

1. `/catalog`: enter -1 in Maximum monthly rent. Show the validation message, then restore 500000. Search `no-such-complex`; show the empty state and its suggestions.
2. Use the Mangilik El filter. Open a source link and explain its date, deposit, archived label and any price conflict. Do not claim that all six examples are currently available.
3. `/preferences`: set Price to 5, Neighbourhood and Commute to 0. Apply. The demo section opens automatically. Note the first homes; change Neighbourhood to 5 and Price to 0. The exact order depends on other filters, but the C# ranking test proves a controlled reversal.
4. Open a demo apartment. Explain reasons, missing data, monthly costs and the separate deposit. Add two homes to comparison and reload the comparison page: the selection remains in this tab.
5. In preferences choose “Demo: provider unavailable”. Apply and open a demo apartment: no trip minutes are fabricated.
6. In a signed-out browser open `/admin` and `/owner/edit/1`: both redirect to sign-in. For a signed-in Student, show the role gate. Explain the service-level tests preventing Student writes and another Owner's edits. An unexecuted signed-in browser scenario must not be presented as tested.
7. Show the desktop/mobile screenshots. Use Shift+Tab to reach “Skip to content”, then Enter; the main area receives focus. This is a keyboard sample, not a complete manual accessibility audit.

## C# questions

- **Why Core?** Core contains immutable model values and calculations; its project has no EF reference. Web performs database/identity effects. This allows deterministic tests without a web server.
- **Why record / with?** `Preferences` is a value-oriented record. `p with { Budget = 300000 }` produces a new preference value without modifying p. ImmutableArray is used for collection values in Core.
- **Where is LINQ evaluated?** HousingService composes EF queries that SQLite executes (including the 500-row cap). RecommendationEngine, NeighborhoodService and MarketCatalog filter and rank in memory.
- **Where is pattern matching?** Validate uses relational patterns; ChooseTrip switches on priority and Crowding; Evaluate distinguishes RoutesFound, RoutesMissing and RoutesUnavailable.
- **What is pure?** Evaluate and CalculateCost depend on their arguments and do not read a database, HTTP or the clock. HousingService.SaveListing is not pure: it persists data and records UTC time.
- **How are passwords stored?** ASP.NET Core Identity handles password hashing. No plaintext password belongs in source control. Email confirmation is disabled in this teaching version.
- **Why SQLite / EnsureCreated?** Local startup is simple. The housing schema uses EnsureCreated; Identity uses migrations. Housing schema evolution needs migrations before deployment.
- **Uploads?** The form checks a 5 MB limit and JPEG/PNG signatures, and generates a GUID filename. These checks do not replace full decoding, malware scanning, quotas or orphan cleanup.
- **AI assistance?** Explain truthfully which implementation help was used and what you personally reviewed, changed and tested. Commit history and working knowledge support your contribution; do not invent authorship.

## Score calculation by hand

For valid eligible homes, `score = Round(100 × (price × Pw + lifestyle × Lw + travel × Tw) / (Pw + Lw + Tw))`.
`price = Clamp(1 - rent / budget, 0, 1)`; `lifestyle = matched wanted amenities / wanted amenities` (zero if none wanted); `travel = Clamp(1 - (outbound + return) / (2 × maximum minutes), 0, 1)` (zero when data is missing). Required amenities and trip limits can exclude a home before scoring. All-zero weights are rejected.

Hypothetical explanation, not real Astana travel data: budget 300000, maximum travel 50 minutes, two wanted amenities, weights 2/2/1.

| Home | Rent | Amenities matched | Out / return | Price | Lifestyle | Travel | Rounded score |
|---|---:|---:|---|---:|---:|---:|---:|
| A | 100000 | 0/2 | 30/30 | 0.6667 | 0 | 0.4 | 35 |
| B | 180000 | 2/2 | 20/20 | 0.4 | 1 | 0.6 | 68 |
| C | 150000 | 1/2 | 10/10 | 0.5 | 0.5 | 0.8 | 56 |

With price-only weights the order is A, C, B. With lifestyle-only weights it is B, C, A. See `Priorities_change_the_ranking` for an automated controlled example.

## Rehearsal

Allow 2 minutes for the problem, 5 for the workflow, 4 for C# architecture, 3 for authorization/tests/limits, and the remainder for roles and questions. Each participant should answer five questions, then locate the implementation and check their explanation. Both must understand the shared architecture. Do not claim that presentation preparation is complete until you can demonstrate it yourselves.
