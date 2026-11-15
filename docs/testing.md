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
npm ci --prefix scripts            # Node.js 22+
node scripts/browser-audit.mjs     # uses Google Chrome; or CHROME_PATH=/usr/bin/chromium
```

Last run 2026-11-15 on Linux, Chromium, desktop 1440×1000 — all passed with no console errors:

- Areas tab shows 6 ranked areas.
- Making "Spa & sauna" a must-have changes results (6 → 0 homes: no mapped sauna near any listing).
- Budget 0 is rejected with a message.
- Two homes in comparison survive a reload.
- Changing the destination to Khan Shatyr survives a reload.
- Commute planner from Comfort Town shows options and the bus load chart.
- Home details render; a missing home and an invalid area link show friendly pages.

Screenshots in `docs/screenshots/` were captured the same day at 1440×1000 and 390×844 (emulated phone).

## Not yet verified

Physical phones, Safari/Firefox, screen readers end to end, signed-in browser flows, load testing.
