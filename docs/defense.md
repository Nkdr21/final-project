# Defence guide

Run the app (`dotnet run --project src/Dwellio.Web --launch-profile http`) and open `http://localhost:5180`. Have internet on for the live map; everything also works offline from the bundled snapshot.

## 10-minute demonstration

1. **Home `/`** — the problem: moving to a new city, you want life to be easy around your university or job. Type "Nazarbayev" in the search, pick it.
2. **Planner `/plan`** — results update instantly:
   - Tap **Gym** twice → must-have. Homes without a gym within the walk limit move to "don't fit — see why".
   - Move the walking slider; change **On the bus I prefer → A seat**.
   - Point at the map label: "Live OpenStreetMap" vs "snapshot" (explain the three data layers).
3. **Areas tab** — best neighbourhoods even without a listing; click an area to see its nearest gym, café and bus stop on the map. Tap the map anywhere to open an area report.
4. **Home details** (Comfort Town studio) — everyday places with real names and walking minutes; the bus plan; open "How full is bus 46…" to show the load chart; explain the cost box and the score parts.
5. **Commute `/commute`** — choose Comfort Town or a point near Khan Shatyr. Compare tactics ("walk back to an earlier stop", "leave earlier"). Use the **route load explorer** slider: 08:00 vs 13:00 vs 18:00.
6. **Compare** two homes; reload — still there. Show `/about` for sources and limits.
7. Robustness: open `/area?lat=abc` (friendly page), set rent to 0 (validation message), turn Wi-Fi off and reload (snapshot still works).

## Where to point in the code

| Question | File |
|---|---|
| Where are decisions made? | `src/Dwellio.Core` — no EF, no HTTP |
| How is the seat chance computed? | `Transit/SeatModel.cs` (fold), formula in `docs/recommendations.md` |
| How are tactics chosen? | `Transit/JourneyPlanner.cs` — `Combine`, `Score`, `Options` |
| How does live data work? | `Web/Services/OpenData/PlaceService.cs`, `OverpassClient.cs` |
| State management? | `Web/Services/PlannerState.cs`, `Components/Layout/PlannerScope.razor` |
| Reusable / generic components? | `Shared/BarChart.razor`, `Segmented.razor`, `LiveMap.razor` + `wwwroot/js/map.js` |
| Tests? | `tests/Dwellio.Tests` — run `dotnet test` |

## Likely questions

- **Why not 2GIS?** It needs a key and sign-up; the brief was free and key-less. OSM has stops but no Astana route relations, so the open GPS dataset (routes 10, 12, 46) provides real timing; other trips are labelled estimates.
- **Is the seat chance real?** It is a model built from real dwell times. No one publishes passenger counts. Explain the four steps and the 2–95 % clamp.
- **Why immutable records?** `Preferences` changes produce new values (`with`), so caching (`CacheKey`), persistence and change detection are simple and safe across threads.
- **What is pure / impure?** `HomeMatcher.Evaluate` is pure. `PlaceService.RefreshAsync` is not: it does HTTP, writes SQLite and swaps the index.
- **What happens when Overpass is down?** Mirrors are tried in order; if all fail the snapshot/cached data is used and the label says so. See `OverpassClientTests`.
- **Why is a home "not fitting" still shown?** Transparency: hard requirements exclude, but the user sees exactly why and can relax them.
- **Passwords?** ASP.NET Core Identity hashing; demo users only via the `DWELLIO_DEMO_PASSWORD` environment variable.
- **AI assistance?** Explain truthfully what was generated, what you reviewed, changed and tested.
