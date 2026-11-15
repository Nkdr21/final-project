#!/usr/bin/env python3
"""Download a compact OpenStreetMap snapshot of everyday places and bus stops in Astana.

Data © OpenStreetMap contributors, ODbL 1.0 (https://www.openstreetmap.org/copyright).
The app uses this file when the live Overpass API is slow or unreachable, so the planner
always opens instantly. Run from the repository root:

    python3 scripts/build_places_snapshot.py src/Dwellio.Web/Data/Places/astana-osm.json

Each element is stored as [lat, lon, "key=value", "name"]. Classification into Dwellio
categories happens in C# (Dwellio.Core PlaceCatalog), so live and bundled data use the
same rules.
"""
import json
import sys
import time
import urllib.parse
import urllib.request
from datetime import date
from pathlib import Path

BBOX = "50.98,71.25,51.28,71.66"  # south, west, north, east around Astana
KEYS = {
    "leisure": "fitness_centre|sports_centre|sauna|park|pitch|swimming_pool",
    "amenity": "restaurant|fast_food|cafe|pharmacy|library|coworking_space|clinic|hospital|doctors|public_bath|atm|bank|kindergarten",
    "shop": "supermarket|convenience|greengrocer|massage|mall",
    "office": "coworking",
}
MIRRORS = [
    "https://overpass-api.de/api/interpreter",
    "https://maps.mail.ru/osm/tools/overpass/api/interpreter",
    "https://overpass.private.coffee/api/interpreter",
]
USER_AGENT = "Dwellio-student-project/1.0 (educational; snapshot builder)"


def query() -> str:
    parts = [f'nwr({BBOX})["{k}"~"^({v})$"];' for k, v in KEYS.items()]
    parts.append(f"node({BBOX})[highway=bus_stop];")
    return f"[out:json][timeout:180];({''.join(parts)});out center tags qt;"


def fetch() -> dict:
    data = urllib.parse.urlencode({"data": query()}).encode()
    for attempt in range(3):
        for url in MIRRORS:
            try:
                request = urllib.request.Request(url, data=data, headers={"User-Agent": USER_AGENT})
                with urllib.request.urlopen(request, timeout=200) as response:
                    return json.load(response)
            except Exception as error:  # noqa: BLE001 - report and try the next mirror
                print(f"{url}: {error}", file=sys.stderr)
        time.sleep(10 * (attempt + 1))
    sys.exit("All Overpass mirrors failed; try again later.")


def compact(element: dict):
    tags = element.get("tags", {})
    lat = element.get("lat", element.get("center", {}).get("lat"))
    lon = element.get("lon", element.get("center", {}).get("lon"))
    if lat is None or lon is None:
        return None
    if tags.get("highway") == "bus_stop":
        tag = "highway=bus_stop"
    else:
        tag = next((f"{k}={tags[k]}" for k in KEYS if k in tags and tags[k] in KEYS[k].split("|")), None)
    if tag is None:
        return None
    name = tags.get("name:en") or tags.get("name") or tags.get("name:ru") or tags.get("name:kk") or ""
    return [round(lat, 6), round(lon, 6), tag, name]


def main(target: Path) -> None:
    elements = [x for x in map(compact, fetch()["elements"]) if x]
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps({
        "source": "© OpenStreetMap contributors, ODbL 1.0",
        "retrieved": date.today().isoformat(),
        "bbox": BBOX,
        "elements": elements,
    }, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    print(f"Wrote {len(elements)} places to {target}")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(Path(sys.argv[1]))
