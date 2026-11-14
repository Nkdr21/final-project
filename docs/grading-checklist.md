# Grading evidence and remaining work

The supplied final rubric has 20 criteria at 5% each. This checklist records implementation, not an awarded score. An A/100 is not guaranteed.

| Criterion | Evidence | Remaining verification / limitation |
|---|---|---|
| Code structure | Core/Web/Tests separation | Team must explain responsibilities |
| Component reuse | ApartmentCard, TripCard, MapPanel, AmenityGroup | Review component parameters |
| Cross-platform | .NET + browser; CI matrix configured | Only local macOS execution verified; CI awaits push |
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
| Documentation | README, architecture, data model, sources, tests, handoff | Keep updated after friend changes |
| Innovation | Lifestyle plus two-direction commute comparison | Demonstrate priority changes clearly |
| Creative C# / FP | Records, pure functions, Func predicates, LINQ, typed outcomes | No unnecessary library abstractions |
| Professionalism | No tracked secrets, accessible labels, clear demo provenance | Not production-ready; email/uploads need hardening |
| Presentation | Demo outline and lecture mapping | Actual slides and rehearsals are later stages |
