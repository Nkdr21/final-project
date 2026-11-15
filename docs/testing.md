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

## Update: comparison persistence and housing interface — 2026-11-15

- `dotnet build --no-restore` and `dotnet test --no-build`: 41 passed, zero failed/skipped; build zero warnings/errors.
- Three `ComparisonStorageTests`: restoration into a new circuit and removal, positive/unique/max-three IDs, write failure fallback.
- Chrome: selected two catalog apartments, opened comparison, fully reloaded page; both remained. Removed one and reloaded; only the other remained.
- Captured updated home, catalog, comparison and apartment pages at desktop 1440×1000 and mobile 390×844. Mobile is viewport emulation, not a physical-device test.
- HTTP smoke: 10 public routes and 3 anonymous redirects passed against the updated application on port 5182.
- Added skip-to-content link, visible keyboard focus and reduced-motion CSS; these changes do not establish full accessibility compliance.
- No repeatable browser E2E suite has been added. Account flows, multiple browsers, load tests and deployment remain to be verified.

## Sourced rental references — 2026-11-15

- Added `/market` with six sourced Astana rental examples, name/address and rent filters, sorting, source links and explicit snapshot limitations.
- Build: zero warnings/errors; existing 41 unit/integration tests passed. These tests do not validate external listing availability or market-page interactions.
- HTTP smoke: 11 public routes including `/market` and 3 anonymous redirects passed on port 5183.
- Browser access was unavailable for this update; market filter interactions and new layout are not visually verified. Existing screenshots show the preceding interface version; the home headline and navigation have since changed.

- Mangilik El extension: three examples added (Expo New Life-2, Promenade Expo, Dream City), avenue filter and archived-price label. Build and 41 tests passed; interactive avenue-filter check not performed.

## Integrated catalog and English documentation — 2026-11-15

- Main `/catalog` now displays six sourced examples with five source photo URLs and one explicit missing-photo fallback. `/market` redirects to `/catalog`; separate navigation was removed. The demo matching workflow remains expandable below the real cards.
- All five source image endpoints returned HTTP 200 with image/jpeg in the previous verification. The final HTTP HTML check confirmed five remote image tags, no separate market navigation and fallback text. This does not certify visual browser rendering.
- Fixed a Razor string-parameter binding discovered by the HTTP check before delivery. Build: zero warnings/errors; tests: 41/41 passed. Final HTTP smoke: 11 routes and 3 redirects passed on port 5184.
- README, architecture, data sources and team plan are in English. Existing screenshots are outdated and explicitly labeled in README.
- Browser automation was unavailable; photo rendering, image-error interaction and responsive layout still require a browser check.

## Repository cleanup — 2026-11-15

Removed 42 unused Bootstrap distribution variants and the unused hero illustration; retained bootstrap.min.css and its source map. Removed 50 unused old-layout CSS selectors while retaining the preference action styles. No C# logic changed. Build completed with zero warnings/errors; all 41 tests passed. Visual regression testing was not performed for this cleanup. README launch URL now matches the configured HTTP profile (port 5180).
