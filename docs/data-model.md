# Data model

## Core records (immutable values)

| Record | Meaning |
|---|---|
| `GeoPoint(Lat, Lon)` | Coordinate (value type) |
| `Place(Category, Name, Location)` | One OpenStreetMap place classified into a Dwellio category |
| `PlaceCategory(Key, Label, Icon, OsmTags, Hint)` | A lifestyle need and the OSM tags that satisfy it |
| `NeedResult`, `AreaReport` | Nearest place and walking time per need, bus stops, coverage and lifestyle score |
| `TransitStop`, `TransitRoute`, `TransitNetwork` | Stops in travel order with per-hour median dwell and time-from-start |
| `StopLoad` | Estimated boarding, alighting, passengers on board and seat chance at one stop |
| `JourneyOption` | Route, boarding/alighting stop, hour, walk/wait/ride minutes, seat chance, tactic |
| `CommutePlan` | Closed set: `TransitPlan`, `EstimatedBusPlan`, `WalkPlan`, `NoCommute` |
| `Preferences` | Destination, budget, rooms, needs/must-haves, walk limit, arrival hour, priority, weights, costs |
| `HomeMatch` | Evaluation of one home: score parts, reasons, gaps and exclusions |
| `AreaCandidate` | A recommended neighbourhood with its report, commute and score |

## SQLite (housing.db)

| Table | Key fields |
|---|---|
| `Complexes` | Name, address, real latitude/longitude, description |
| `Listings` | Complex, `Kind` (Sourced / Owner / Sample), rent, utilities?, deposit?, rooms, area, floor, furnished, available, hidden, owner, photo, source URL, evidence |
| `Favorites` | (UserId, ListingId) |
| `Profiles` | UserId → saved `Preferences` as JSON |
| `Reviews` | One per user and complex; three 1–5 ratings |
| `PlaceCache` | ~1 km tile key → last live Overpass answer (compact JSON) and time |

Identity uses a separate `identity.db` with EF migrations. The housing database uses `EnsureCreated` plus a schema version: when `HousingDbContext.SchemaVersion` changes, the local housing database is rebuilt and reseeded (local favourites, reviews and owner listings are lost — acceptable for this classroom version, not for production).

## Bundled files

| File | Built by | Size |
|---|---|---|
| `src/Dwellio.Web/Data/Places/astana-osm.json` | `scripts/build_places_snapshot.py` | ~240 KB, ~4,400 places |
| `src/Dwellio.Web/Data/Transit/astana-gtfs.json` | `scripts/build_transit_snapshot.py` | ~110 KB, 6 route directions |
