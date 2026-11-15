#!/usr/bin/env python3
"""Build Dwellio's bundled Astana transit snapshot from the open GTFS dataset.

Source: Mansurova et al., "From Raw GPS to GTFS: A Real-World Open Dataset for Bus
Travel Time Prediction", Zenodo, DOI 10.5281/zenodo.15769359, licence CC BY 4.0.
Download gtfs_data.zip from https://zenodo.org/records/15769359, unzip it, then run:

    python3 scripts/build_transit_snapshot.py <path-to/gtfs_data> src/Dwellio.Web/Data/Transit/astana-gtfs.json

The output is small (a few hundred KB): for each route direction it stores the most common
stop order and, per stop and hour, the median dwell time, the median time from the first
stop and the average number of departures per day. The app's C# code turns these numbers
into journey times, headways and seat-chance estimates.
"""
import csv
import json
import statistics
import sys
from collections import Counter, defaultdict
from pathlib import Path

HOURS = range(5, 24)
ROUTE_NAMES = {
    "10": "Railway Station – International Airport",
    "12": "Railway Station – International Airport",
    "46": "Karasu Street – Comfort Town",
}


def read(folder: Path, name: str):
    with open(folder / name, encoding="utf-8", errors="replace", newline="") as f:
        yield from csv.DictReader(f, delimiter="\t")


def seconds(value: str) -> int:
    h, m, s = (int(x) for x in value.split(":"))
    return h * 3600 + m * 60 + s


def main(source: Path, target: Path) -> None:
    routes = {r["route_id"]: r for r in read(source, "routes.txt")}
    trips = {t["trip_id"]: t for t in read(source, "trips.txt")}
    stops = {s["stop_id"]: s for s in read(source, "stops.txt")}
    days = {t["service_id"] for t in trips.values()}

    by_trip = defaultdict(list)
    for row in read(source, "stop_times.txt"):
        by_trip[row["trip_id"]].append(
            (int(row["stop_sequence"]), row["stop_id"], seconds(row["arrival_time"]), seconds(row["departure_time"])))

    # Group trips by route direction and find the most common stop pattern.
    groups = defaultdict(list)
    for trip_id, rows in by_trip.items():
        rows.sort()
        trip = trips[trip_id]
        groups[(routes[trip["route_id"]]["route_short_name"], trip["direction_id"])].append(rows)

    output = []
    for (ref, direction), trip_rows in sorted(groups.items()):
        pattern = Counter(tuple(r[1] for r in rows) for rows in trip_rows).most_common(1)[0][0]
        index = {stop_id: i for i, stop_id in enumerate(pattern)}
        dwell = defaultdict(list)       # (stop index, hour) -> seconds
        offset = defaultdict(list)      # (stop index, hour) -> seconds from trip start
        departures = Counter()          # hour -> trips starting in that hour
        for rows in trip_rows:
            if tuple(r[1] for r in rows) != pattern:
                continue
            start = rows[0][3]
            departures[(start // 3600) % 24] += 1
            hour = (start // 3600) % 24
            for _, stop_id, arrival, departure in rows:
                i = index[stop_id]
                offset[(i, hour)].append(arrival - start)
                if 0 < i < len(pattern) - 1:
                    dwell[(i, hour)].append(max(0, departure - arrival))

        def median(values):
            return round(statistics.median(values), 1) if values else None

        output.append({
            "ref": ref,
            "direction": int(direction),
            "name": ROUTE_NAMES.get(ref, ref),
            "stops": [{
                "id": stop_id,
                "name": stops[stop_id]["stop_name"],
                "lat": round(float(stops[stop_id]["stop_lat"]), 6),
                "lon": round(float(stops[stop_id]["stop_lon"]), 6),
                "dwell": {h: median(dwell[(i, h)]) for h in HOURS if dwell[(i, h)]},
                "offset": {h: median(offset[(i, h)]) for h in HOURS if offset[(i, h)]},
            } for i, stop_id in enumerate(pattern)],
            "departuresPerDay": {h: round(departures[h] / len(days), 2) for h in HOURS if departures[h]},
        })

    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps({
        "source": "Mansurova et al., Zenodo DOI 10.5281/zenodo.15769359 (CC BY 4.0)",
        "period": "2024-07-29 to 2024-09-22",
        "serviceDays": len(days),
        "routes": output,
    }, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    print(f"Wrote {target} with {len(output)} route directions from {len(days)} service days")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(Path(sys.argv[1]), Path(sys.argv[2]))
