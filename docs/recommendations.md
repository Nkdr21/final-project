# How results are calculated

All formulas live in `src/Dwellio.Core` as pure functions and are covered by tests.

## Walking time

`minutes = ceil(haversine metres × 1.3 / 80)` (`GeoMath.WalkMinutes`). 1.3 is an urban detour factor; 80 m/min ≈ 4.8 km/h.

## Area report

For each selected need, `AreaAnalysis.Analyse` finds the nearest place of that category within twice the walk limit (using the grid-based `PlaceIndex`) and counts options within the limit.

`lifestyle = average over needs of: 1 if minutes ≤ limit; else max(0, 1 − (minutes − limit) / limit); 0 if none found`.

No selected needs → 0 (nothing is assumed to be good).

## Seat model

`SeatModel.Profile(route, hour)` is a fold over the stops in travel order:

1. `activity = max(0, dwell − 8 s) / 2.5 s` passengers at a stop (median dwell from GPS for that hour). Terminals use the route's average activity.
2. `boardingShare = 1 − index / (stops − 1)`; boarding = activity × share, alighting = activity × (1 − share), never more than are on board.
3. `onBoard = clamp(previous − alighting + boarding, 0, 95)`.
4. Seat chance when boarding: `z = (30 − 0.5 − (onBoardBefore + boarding/2)) / (3 + 0.25 × onBoardBefore)`, `p = 1 / (1 + e^(−1.702 z))`, clamped to 2–95 %. The spread models bus-to-bus variation (bunching).
5. `SeatFreeFromIndex`: the first later stop where the average load drops below the seat count.

Expected standing minutes = `(1 − p) × ride time until seats free up (or until you get off)`.

## Journey planner

`JourneyPlanner.Plan`:

1. Destination ≤ 20 min on foot → `WalkPlan`.
2. Otherwise, for every route direction: boarding stops within the walk limit of home × alighting stops within the walk limit of the destination (after the boarding stop), at the usual hour (arrival − 1) and one hour earlier. Ride = difference of median times from the start; wait = half of a 10-minute headway.
3. Each option gets a tactic relative to the usual trip: **nearest stop**, **board an earlier stop**, **leave earlier**, **other route**.
4. Ranking uses composed criteria: `Combine((Minutes, 1), (Standing, w), (Walking, v))` with `w = 3` for "A seat", 1 for "Balanced", 0 for "Fastest". Alternatives that are no better than an option already kept on both time and seat are dropped (a fold), so every suggestion is a real trade-off.
5. No route covers the trip → `EstimatedBusPlan` between the nearest real OSM stops at ≈17 km/h plus a full headway of waiting, without a seat chance. No stop nearby → `NoCommute`.

## Home score

Hard requirements (excluded homes are still shown with reasons): available, rent ≤ budget, rooms, furnished, every must-have within the walk limit, commute ≤ maximum (when known).

```
price     = clamp(1 − rent / budget, 0, 1) × 0.6 + 0.4 (if within budget)
lifestyle = AreaReport.LifestyleScore
commute   = clamp(1 − minutes / (1.5 × max), 0, 1)
            × (0.5 + 0.5 × seat chance)   when priority is "A seat"
            × 0.85                        for estimates
            = 0                           when unknown
score     = round(100 × (price·Wp + lifestyle·Wl + commute·Wc) / (Wp + Wl + Wc))
```

Monthly cost = rent + utilities (if known) + bus fares + gym membership (if "gym" is a need). The deposit is shown separately and added only to the move-in total.

## Best areas

`AreaSearch.Best` evaluates a 450 m grid within 6 km of the destination, skips empty land and points missing a must-have or over the commute limit, scores `(lifestyle·Wl + commute·Wc) / (Wl + Wc)` and greedily keeps the best points at least 1.1 km apart (a fold). Each area is named after its nearest named bus stop.
