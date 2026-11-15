using System.Collections.Frozen;
using System.Collections.Immutable;
using Dwellio.Core.Geo;

namespace Dwellio.Core.Places;

/// <summary>A lifestyle need (gym, spa…) and the OpenStreetMap tags that satisfy it.</summary>
public sealed record PlaceCategory(string Key, string Label, string Icon, ImmutableArray<string> OsmTags, string Hint);

/// <summary>One real place from OpenStreetMap, already classified into a Dwellio category.</summary>
public sealed record Place(string Category, string Name, GeoPoint Location)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Unnamed {PlaceCatalog.Label(Category).ToLowerInvariant()}" : Name;
}

/// <summary>
/// Single source of truth for categories. The Overpass query, the live parser and the bundled
/// snapshot all classify places through <see cref="Classify"/>, so the rules cannot drift apart.
/// </summary>
public static class PlaceCatalog
{
    public const string BusStop = "bus_stop";
    public const string BusStopTag = "highway=bus_stop";

    public static readonly ImmutableArray<PlaceCategory> All =
    [
        new("gym", "Gym", "🏋️", ["leisure=fitness_centre", "leisure=sports_centre"], "Fitness clubs and sports centres"),
        new("spa", "Spa & sauna", "🧖", ["leisure=sauna", "amenity=public_bath", "shop=massage"], "Saunas, bathhouses and massage"),
        new("restaurant", "Restaurants", "🍽️", ["amenity=restaurant", "amenity=fast_food"], "Restaurants and quick food"),
        new("cafe", "Cafés", "☕", ["amenity=cafe"], "Coffee and a place to sit"),
        new("groceries", "Groceries", "🛒", ["shop=supermarket", "shop=convenience", "shop=greengrocer"], "Supermarkets and corner shops"),
        new("pharmacy", "Pharmacy", "💊", ["amenity=pharmacy"], "Pharmacies"),
        new("park", "Parks", "🌳", ["leisure=park"], "Parks and green space"),
        new("sports", "Sports grounds", "⚽", ["leisure=pitch", "leisure=swimming_pool"], "Pitches, courts and pools"),
        new("study", "Study spaces", "📚", ["amenity=library", "amenity=coworking_space", "office=coworking"], "Libraries and coworking"),
        new("health", "Clinics", "🏥", ["amenity=clinic", "amenity=hospital", "amenity=doctors"], "Clinics and hospitals"),
        new("bank", "ATM & bank", "🏧", ["amenity=atm", "amenity=bank"], "Cash machines and banks"),
        new("mall", "Shopping malls", "🛍️", ["shop=mall"], "Malls and shopping centres"),
        new("kids", "Kindergartens", "🧸", ["amenity=kindergarten"], "Kindergartens for families"),
    ];

    private static readonly FrozenDictionary<string, string> ByTag = All
        .SelectMany(c => c.OsmTags.Select(tag => (Tag: tag, Category: c.Key)))
        .Append((Tag: BusStopTag, Category: BusStop))
        .ToFrozenDictionary(x => x.Tag, x => x.Category);

    private static readonly FrozenDictionary<string, PlaceCategory> ByKey = All.ToFrozenDictionary(c => c.Key);

    /// <summary>Every OSM tag Dwellio asks Overpass for, grouped by key (e.g. amenity → [cafe, pharmacy…]).</summary>
    public static ILookup<string, string> TagsByKey { get; } = ByTag.Keys
        .Select(tag => tag.Split('=', 2))
        .ToLookup(parts => parts[0], parts => parts[1]);

    /// <summary>Returns the Dwellio category for an OSM "key=value" tag, or null when it is not relevant.</summary>
    public static string? Classify(string tag) => ByTag.GetValueOrDefault(tag);

    /// <summary>Finds the first relevant tag among an element's tags.</summary>
    public static string? Classify(IReadOnlyDictionary<string, string> tags) => tags
        .Select(t => Classify($"{t.Key}={t.Value}"))
        .FirstOrDefault(c => c is not null);

    public static PlaceCategory? Find(string key) => ByKey.GetValueOrDefault(key);

    public static string Label(string key) => key == BusStop ? "Bus stop" : Find(key)?.Label ?? key;

    public static string Icon(string key) => key == BusStop ? "🚌" : Find(key)?.Icon ?? "📍";

    /// <summary>Keeps only known category keys, without duplicates, in catalogue order.</summary>
    public static ImmutableArray<string> Normalize(IEnumerable<string>? keys)
    {
        var wanted = (keys ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return All.Where(c => wanted.Contains(c.Key)).Select(c => c.Key).ToImmutableArray();
    }
}
