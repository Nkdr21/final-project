# Verification status

Local platform: Apple Silicon macOS, .NET SDK 10.0.401. Verification updated 2026-11-15.

- Before and after repository cleanup: `dotnet build` passed with zero warnings/errors; `dotnet test --no-build` passed 38/38, with zero failures and skips.
- 38 unit/integration tests passed after neighbourhood implementation. Uses xUnit and actual in-memory SQLite.
- HTTP smoke script passed 10 public pages and 3 anonymous access redirects.
- Chrome browser check on 2026-11-15: home → catalog → select two apartments → comparison → apartment detail. Comparison displayed both selected apartments. Captured four pages at 1440×1000 and 390×844 in `screenshots/`. This is a limited browser check, not a repeatable E2E suite or physical-device certification. Keyboard, account and full cross-browser flows remain unverified.
- [GitHub Actions run 43634904148](https://github.com/Nkdr21/final-project/actions/runs/43634904148) succeeded on Windows, Linux and macOS before this cleanup. The current uncommitted changes have not run on CI yet.
- Coverage collection is supported; no high-coverage percentage is claimed for template Identity/UI code.

## Manual acceptance checklist

1. Open localhost:5180 on desktop and at phone width. Inspect header, cards, forms and comparison; no horizontal overflow.
2. Change budget/amenity preferences; see result order change. Test zero results and invalid weights.
3. Add three homes; fourth comparison choice should be refused. Remove a home and add another.
4. Register Student; save home and preferences; log out/in and check persistence.
5. Register Owner; add listing with valid PNG/JPEG; edit it; mark unavailable. Attempt another owner's URL.
6. Review a complex: text under 10 characters or scores outside 1–5 must fail. A second review updates the first.
7. Login Admin using locally configured demo account; hide/restore a listing and review. Student access must fail.
8. Choose unavailable route-provider scenario; catalog remains usable and missing-data warning is shown.
9. Disable internet: main catalog remains usable; external map/fonts may not load.
10. Repeat key flows in Chrome and Safari/Firefox. Record actual browser/device and outcome before claiming cross-browser QA.

## Automated scope

RecommendationTests covers hard constraints, missing data, independent return limits, weights, deterministic calculations, costs, route outcomes and comparison limits.
HousingServiceTests covers account isolation, ownership, forbidden student writes, database persistence, moderation and review uniqueness/rating bounds.
NeighborhoodTests covers case-insensitive search, conjunctive filters, inclusive distance boundaries and inside/nearby grouping.

The HTTP smoke script verifies server rendering and redirect protection; it cannot verify interactive circuit behaviour or image layout.
