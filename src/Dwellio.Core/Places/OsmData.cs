using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Dwellio.Core.Geo;

namespace Dwellio.Core.Places;

/// <summary>Builds Overpass QL and parses its responses. Pure: strings in, records out.</summary>
public static class OsmData
{
    /// <summary>One query for every category plus bus stops around a point.</summary>
    public static string OverpassQuery(GeoPoint centre, int radiusMeters, int timeoutSeconds = 25)
    {
        var around = string.Create(CultureInfo.InvariantCulture, $"around:{radiusMeters},{centre.Lat:F6},{centre.Lon:F6}");
        var filters = PlaceCatalog.TagsByKey
            .Where(group => group.Key != "highway")
            .Select(group => $"nwr({around})[\"{group.Key}\"~\"^({string.Join('|', group.Order())})$\"];");
        return $"[out:json][timeout:{timeoutSeconds}];({string.Concat(filters)}node({around})[highway=bus_stop];);out center tags qt;";
    }

    /// <summary>Parses an Overpass JSON response. Elements without coordinates or a known tag are skipped.</summary>
    public static ImmutableArray<Place> ParseOverpass(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("elements", out var elements)) return [];
        return elements.EnumerateArray().Select(ParseElement).OfType<Place>().ToImmutableArray();
    }

    private static Place? ParseElement(JsonElement element)
    {
        var tags = element.TryGetProperty("tags", out var t)
            ? t.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "")
            : new Dictionary<string, string>();
        var category = PlaceCatalog.Classify(tags);
        var location = Coordinates(element) ?? (element.TryGetProperty("center", out var c) ? Coordinates(c) : null);
        return category is null || location is null ? null : new Place(category, BestName(tags), location.Value);
    }

    private static GeoPoint? Coordinates(JsonElement e) =>
        e.TryGetProperty("lat", out var lat) && e.TryGetProperty("lon", out var lon)
            ? new GeoPoint(lat.GetDouble(), lon.GetDouble())
            : null;

    private static string BestName(IReadOnlyDictionary<string, string> tags) =>
        new[] { "name:en", "name", "name:ru", "name:kk", "brand" }
            .Select(tags.GetValueOrDefault)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))?.Trim() ?? "";

    /// <summary>Parses the compact bundled snapshot: elements are [lat, lon, "key=value", "name"].</summary>
    public static ImmutableArray<Place> ParseSnapshot(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("elements").EnumerateArray()
            .Select(e => (Lat: e[0].GetDouble(), Lon: e[1].GetDouble(), Tag: e[2].GetString() ?? "", Name: e[3].GetString() ?? ""))
            .Select(e => PlaceCatalog.Classify(e.Tag) is { } category ? new Place(category, e.Name, new(e.Lat, e.Lon)) : null)
            .OfType<Place>()
            .ToImmutableArray();
    }

    /// <summary>Serialises places in the same compact format, used by the persistent cache.</summary>
    public static string ToSnapshotJson(IEnumerable<Place> places) => JsonSerializer.Serialize(new
    {
        elements = places.Select(p => new object[]
        {
            Math.Round(p.Location.Lat, 6), Math.Round(p.Location.Lon, 6),
            p.Category == PlaceCatalog.BusStop ? PlaceCatalog.BusStopTag : PlaceCatalog.Find(p.Category)!.OsmTags[0],
            p.Name
        })
    });
}
