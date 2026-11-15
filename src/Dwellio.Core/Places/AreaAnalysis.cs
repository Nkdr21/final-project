using System.Collections.Immutable;
using Dwellio.Core.Geo;

namespace Dwellio.Core.Places;

/// <summary>How one need is served from a location.</summary>
public sealed record NeedResult(PlaceCategory Category, Place? Nearest, int? WalkMinutes, int CountWithinLimit, bool Required)
{
    public bool Satisfied(int walkLimit) => WalkMinutes is { } minutes && minutes <= walkLimit;
}

/// <summary>Everything Dwellio knows about daily life around one point.</summary>
public sealed record AreaReport(
    GeoPoint Location,
    int WalkLimit,
    ImmutableArray<NeedResult> Needs,
    Place? NearestStop,
    int? StopWalkMinutes,
    int BusStopsWithinLimit)
{
    public int SatisfiedCount => Needs.Count(n => n.Satisfied(WalkLimit));

    /// <summary>Share of selected needs reachable on foot (0–1). No needs means nothing to score.</summary>
    public double Coverage => Needs.IsEmpty ? 0 : (double)SatisfiedCount / Needs.Length;

    public ImmutableArray<NeedResult> MissingRequired => Needs.Where(n => n.Required && !n.Satisfied(WalkLimit)).ToImmutableArray();

    /// <summary>
    /// Smooth 0–1 lifestyle score: a need inside the limit scores 1, then decays linearly to 0 at
    /// twice the limit. "Eight minutes away" is better than "not within ten" but worse than "two".
    /// </summary>
    public double LifestyleScore => Needs.IsEmpty ? 0 : Needs.Average(n => n.WalkMinutes switch
    {
        null => 0,
        var m when m <= WalkLimit => 1,
        var m => Math.Max(0, 1 - (double)(m - WalkLimit) / WalkLimit),
    });
}

public static class AreaAnalysis
{
    /// <summary>Search radius for "nearest" lookups: twice the walk limit, so near-misses are still shown.</summary>
    public static double SearchRadius(int walkLimit) => GeoMath.WalkRadiusMeters(walkLimit * 2);

    public static AreaReport Analyse(GeoPoint at, PlaceIndex index, IEnumerable<string> needs, IEnumerable<string> required, int walkLimit)
    {
        var requiredSet = required.ToHashSet();
        var limitMeters = GeoMath.WalkRadiusMeters(walkLimit);
        var search = SearchRadius(walkLimit);

        NeedResult Evaluate(PlaceCategory category)
        {
            var nearby = index.Near(at, search, category.Key).ToList();
            var nearest = nearby.Select(x => x.Place).FirstOrDefault();
            return new(category, nearest, nearest is null ? null : GeoMath.WalkMinutes(at, nearest.Location),
                nearby.Count(x => x.Meters <= limitMeters), requiredSet.Contains(category.Key));
        }

        var results = PlaceCatalog.Normalize(needs.Concat(requiredSet))
            .Select(key => Evaluate(PlaceCatalog.Find(key)!))
            .ToImmutableArray();
        var stops = index.Near(at, search, PlaceCatalog.BusStop).ToList();
        var stop = stops.Select(x => x.Place).FirstOrDefault();
        return new(at, walkLimit, results, stop, stop is null ? null : GeoMath.WalkMinutes(at, stop.Location),
            stops.Count(x => x.Meters <= limitMeters));
    }
}
