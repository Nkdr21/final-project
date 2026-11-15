# Verification record

## Current local checks — 2026-11-15

- Build: 0 warnings, 0 errors. C# tests: 46 passed, 0 failed, 0 skipped (41 before this audit).
- Added five regression cases: zero/negative budgets and short/blank/space-padded review text.
- Core has no EF dependency. Unit tests cover deterministic scores, weight-driven ranking reversal, costs and route outcomes. HousingService tests use actual in-memory SQLite to check persistence and authorization. Comparison tests cover storage through an IJSRuntime test double.
- HTTP smoke: 11 public pages and 3 anonymous authorization redirects passed.
- Chrome: 10 grouped checks passed: negative source budget; source filtering/empty state/avenue filter; anonymous admin/edit redirects; demo budget correction; comparison surviving reload; unavailable routes; preferences opening demo results; four desktop pages; four mobile pages; keyboard skip link.
- Full-page screenshots replace the old images in screenshots/: Home, Catalog, Apartment and Compare, at 1440×1000 and 390×844 viewports. Catalog photos are sourced; Tokyo has no verified photo and shows a fallback. Demo detail/comparison still use illustrations.
- Lighthouse 13.6.0 mobile catalog accessibility: 100/100, no failed automated audits after the logo fix. This is one page, not full accessibility certification. Summary: [accessibility.json](accessibility.json).
- Visual inspection found no horizontal overflow on the four tested pages. Physical phones, Safari/Firefox, full keyboard/screen-reader flows and authenticated browser scenarios remain unverified.
- Student writes and another Owner's edits are rejected in server-service tests; do not describe these as completed signed-in browser tests.

## Repeat the checks

From the repository root:

```sh
dotnet build
dotnet test
dotnet run --project src/Dwellio.Web --launch-profile http
```

Keep the server running and use another terminal:

```sh
python3 scripts/http_smoke.py
npm ci --prefix scripts
node scripts/browser-audit.mjs
```

The optional browser tooling requires Node.js 22.19+ and installed Google Chrome. The application and business logic remain C#/.NET. The browser suite uses a fresh context and does not create accounts or edit listings. Set DWELLIO_URL to test a different server; default is http://localhost:5180. Set DWELLIO_SCREENSHOTS=docs/screenshots only when intentionally replacing screenshots. Without that variable, the suite does not write screenshots.

For Lighthouse (server running):

```sh
node scripts/node_modules/lighthouse/cli/index.js http://localhost:5180/catalog --only-categories=accessibility --chrome-flags="--headless" --output=json --output-path=/tmp/dwellio-accessibility.json
```

## Verified fixes

- Non-positive source rent budget produces an error; empty results suggest filter changes.
- Correcting the demo budget clears its old error.
- Applying preferences opens the matching section automatically.
- Reviews are trimmed before validation, preventing whitespace padding from bypassing minimum length.
- Logo accessible name and keyboard skip target corrected.
- Comparison survives same-tab reload. Blocked storage limits persistence; cross-device synchronization is not implemented.

## CI and remaining limits

Cleanup commit a839084 passed Windows/Linux/macOS in [run 43693225603](https://github.com/Nkdr21/final-project/actions/runs/43693225603). The new audit changes require their own CI run after commit/push. The workflow runs C# build/tests; browser/Lighthouse checks were local, not CI jobs. Hosted server deployment, live routes and verified rental availability remain unverified. GitHub Pages cannot host the Blazor server.
