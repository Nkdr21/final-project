using System.Collections.Immutable;
using System.Text.Json;
using Dwellio.Core.Geo;

namespace Dwellio.Core.Transit;

/// <summary>A stop on one route direction with per-hour statistics observed from GPS traces.</summary>
public sealed record TransitStop(
    string Id,
    string Name,
    GeoPoint Location,
    ImmutableSortedDictionary<int, double> DwellSeconds,
    ImmutableSortedDictionary<int, double> OffsetSeconds)
{
    public double? Dwell(int hour) => Hourly.Lookup(DwellSeconds, hour);
    public double? Offset(int hour) => Hourly.Lookup(OffsetSeconds, hour);
}

/// <summary>One direction of a bus route, with stops in travel order.</summary>
public sealed record TransitRoute(string Ref, int Direction, string Name, ImmutableArray<TransitStop> Stops)
{
    public string Key => $"{Ref}-{Direction}";
    public string Terminus => Stops[^1].Name;
    public string Label => $"Bus {Ref} → {Terminus}";
}

public sealed record TransitNetwork(string Source, string Period, ImmutableArray<TransitRoute> Routes)
{
    public static TransitNetwork Empty { get; } = new("", "", []);

    public TransitRoute? Find(string key) => Routes.FirstOrDefault(r => r.Key == key);

    public static TransitNetwork Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        static ImmutableSortedDictionary<int, double> Hours(JsonElement e) => e.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.Number)
            .ToImmutableSortedDictionary(p => int.Parse(p.Name), p => p.Value.GetDouble());
        var routes = root.GetProperty("routes").EnumerateArray().Select(r => new TransitRoute(
            r.GetProperty("ref").GetString()!,
            r.GetProperty("direction").GetInt32(),
            r.GetProperty("name").GetString()!,
            r.GetProperty("stops").EnumerateArray().Select(s => new TransitStop(
                s.GetProperty("id").GetString()!,
                s.GetProperty("name").GetString()!,
                new GeoPoint(s.GetProperty("lat").GetDouble(), s.GetProperty("lon").GetDouble()),
                Hours(s.GetProperty("dwell")),
                Hours(s.GetProperty("offset")))).ToImmutableArray()));
        return new(root.GetProperty("source").GetString() ?? "", root.GetProperty("period").GetString() ?? "",
            routes.Where(r => r.Stops.Length >= 2).ToImmutableArray());
    }
}

internal static class Hourly
{
    /// <summary>Value for an hour, or for the closest hour that has data (buses do not run at 3 am).</summary>
    public static double? Lookup(ImmutableSortedDictionary<int, double> values, int hour) =>
        values.IsEmpty ? null
        : values.TryGetValue(hour, out var exact) ? exact
        : values.MinBy(kv => Math.Abs(kv.Key - hour)).Value;
}
