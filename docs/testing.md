# Testing

## Automated (.NET)

```sh
dotnet test                                   # 98 tests
dotnet test --collect:"XPlat Code Coverage"
```

| Suite | What it proves |
|---|---|
| `GeoTests` | Haversine against a known distance, walking minutes, offsets, Astana bounds |
| `PlaceCatalogTests`, `OsmDataTests` | Tag classification is unique; Overpass parsing (nodes, way centres, irrelevant tags, error pages); snapshot round trip; query covers every tag; bundled snapshot covers Astana |
| `PlaceIndexTests` | Grid index equals brute force on real data; radius and ordering; immutable `ReplaceArea` |
| `AreaAnalysisTests` | Nearest place, missing must-haves, lifestyle decay, no needs = 0 |
| `SeatModelTests` | Activity from dwell; boarding split; seat chance monotonic and bounded; the fold keeps loads physical; real route 46 peaks mid-route |
| `TransitNetworkTests` | Bundled GTFS summary: 3 routes × 2 directions, nearest-hour fallback |
| `JourneyPlannerTests` | Walk vs bus; correct stops; "walk back to an emptier stop" appears with a real trade-off; ties and dominated options hidden; estimate fallback; explicit no-commute; Comfort Town → Khan Shatyr uses bus 46 |
| `HomeMatcherTests` | Exclusions with reasons; must-have vs want; unknown commute never positive; weights change ranking; costs; validation messages; sanitising tampered storage |
| `AreaSearchTests` | Grid bounds; areas sorted, distinct, satisfy must-haves and commute limit |
| `PlannerStateTests` | Plan and comparison survive reloads; invalid updates rejected; tampered/broken storage repaired; blocked storage keeps working and says so |
| `OverpassClientTests` | Busy mirror skipped; all mirrors failing returns null; offline mode makes no requests |
| `HomeFinderIntegrationTests` | Real seed + bundled data, offline: all homes ranked, AITU homes walk, Comfort Town takes bus 46 |
| `HousingServiceTests` | Real in-memory SQLite: authorization, favourites isolation, owner edits, moderation, reviews, corrupted profile JSON |

## HTTP smoke test

```sh
python3 scripts/http_smoke.py http://localhost:5180
```

Checks every page answers with the app shell, assets (Leaflet, map module) are served and `/owner`, `/admin`, `/favorites` redirect anonymous users to sign-in.

## Browser audit

```sh
npm ci --prefix scripts                              # Node.js 22+
node scripts/browser-audit.mjs                       # Google Chrome
CHROME_PATH=/usr/bin/chromium node scripts/browser-audit.mjs
DWELLIO_BROWSER=firefox node scripts/browser-audit.mjs    # after: npx playwright install firefox
DWELLIO_MOBILE=1 node scripts/browser-audit.mjs      # 390 px phone profile
```

The audit waits for Blazor to connect before every navigation and lets each plan change settle before the next step (the planner recalculates after each change).

Checks (same in every browser):

- Areas tab shows ranked areas.
- Making "Spa & sauna" a must-have changes results (6 → 0 homes: no mapped sauna near any listing).
- Budget 0 is rejected with a message.
- Two homes in comparison survive a reload.
- Destination change survives a reload.
- Commute planner from Comfort Town shows options and the bus load chart.
- Home details render; a missing home and an invalid area link show friendly pages.
- No console errors.

| Browser / profile | Result (2026-11-15, Linux) |
|---|---|
| Chromium, desktop 1440×1000 | Passed, no console errors |
| Chromium, phone 390×844 (touch, mobile emulation) | Passed, no console errors |
| Firefox (Playwright build 1572), desktop | Passed, no console errors |
| WebKit / Safari | **Not run:** Playwright's WebKit could not be installed on this Ubuntu 24.04 machine |
| Physical iPhone / Android | **Not run** |

## Accessibility (Lighthouse)

Results are in [accessibility.json](accessibility.json): the planner page (`/plan`), accessibility category, Lighthouse 13.6.0.

| Mode | Score | Failing automated audits |
|---|---|---|
| Mobile | 1.0 | none |
| Desktop | 1.0 | none |

Found and fixed in this run: muted text below contrast requirements, score badges with white text on light backgrounds, links inside paragraphs without underline, and need chips whose accessible names did not contain their visible text. Scope: automated checks on one page; manual screen-reader testing has not been done.

## Continuous integration

`.github/workflows/dotnet.yml` runs:

1. **test** on Ubuntu, Windows and macOS: build, then `dotnet test` with coverage.
2. **browser-checks** on Ubuntu (after `test`): starts the app, runs the HTTP smoke test, then the browser audit in Chrome and in the phone profile.

The workflow was extended on 2026-11-15; check the Actions tab for the first green run of the new job before presenting.

## Not yet verified

Safari/WebKit, physical phones, screen readers end to end, signed-in browser flows, load testing.
