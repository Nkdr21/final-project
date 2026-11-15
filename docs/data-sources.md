# Data sources

Every source is free and needs no API key or sign-up.

| Data | Source | Licence | How Dwellio uses it |
|---|---|---|---|
| Places (13 categories) and 1,100+ bus stops | [OpenStreetMap](https://www.openstreetmap.org) via the [Overpass API](https://wiki.openstreetmap.org/wiki/Overpass_API) | ODbL 1.0 — attribution "© OpenStreetMap contributors" | Bundled snapshot (retrieved 2026-11-15) plus live refresh within 2.5 km of where you plan, cached 72 h |
| Address search | [Nominatim](https://nominatim.org) (fallback [Photon](https://photon.komoot.io)) | ODbL | Limited to Astana's bounding box, max 1 request/second, identified User-Agent, results cached 6 h |
| Map tiles | tile.openstreetmap.org via Leaflet | ODbL / OSM tile policy | Light interactive use with attribution |
| Bus timing | Mansurova et al., *From Raw GPS to GTFS: A Real-World Open Dataset for Bus Travel Time Prediction*, [Zenodo DOI 10.5281/zenodo.15769359](https://doi.org/10.5281/zenodo.15769359) | CC BY 4.0 | Routes 10, 12, 46 (both directions), 55 service days Jul–Sep 2024. Median dwell and run times per stop and hour |
| Asking rents | [krisha.kz](https://krisha.kz) listings, reviewed 2026-11-15 | Linked, not redistributed | Six "✔ Real listing" homes with source links; photos load from the provider |
| Complex coordinates | Nominatim lookups | ODbL | Eleven real complexes |

## Why not 2GIS or Yandex?

They have complete Astana transit data but require registration and an API key. The project brief for this version is "free, no sign-up", so Dwellio uses OpenStreetMap plus the open GPS dataset. OpenStreetMap has stops for Astana but no bus route relations, so trips that buses 10, 12 and 46 do not cover get a clearly labelled estimate.

## Limitations to state honestly

- **Seat chance is a model**, not a measurement: no open source publishes passenger counts in Kazakhstan. See [recommendations.md](recommendations.md#seat-model).
- The GPS data is from 2024; routes and timetables may have changed (CTS announces temporary changes).
- The dataset tracks a sample of vehicles, so it gives reliable *times* but not frequency; a 10-minute headway is assumed.
- OpenStreetMap is volunteer-edited: missing places (e.g. few gyms are mapped) mean "not mapped", not "does not exist".
- "Sample price" homes are real buildings with illustrative rents. Sourced rents may be outdated; one is archived and one has a price conflict, both labelled.

## Rebuilding the snapshots

```sh
python3 scripts/build_places_snapshot.py src/Dwellio.Web/Data/Places/astana-osm.json
# download gtfs_data.zip from https://zenodo.org/records/15769359 and unzip it, then:
python3 scripts/build_transit_snapshot.py <path>/gtfs_data src/Dwellio.Web/Data/Transit/astana-gtfs.json
```
