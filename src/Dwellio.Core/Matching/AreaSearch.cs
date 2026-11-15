using System.Collections.Immutable;
using Dwellio.Core.Geo;
using Dwellio.Core.Models;
using Dwellio.Core.Places;
using Dwellio.Core.Transit;

namespace Dwellio.Core.Matching;

/// <summary>A neighbourhood worth considering, independent of any listing.</summary>
public sealed record AreaCandidate(GeoPoint Location, string Label, AreaReport Report, CommutePlan Commute, int Score);

/// <summary>
/// "Where should I live?" — scans a grid around the destination, scores each point on lifestyle
/// and commute, then greedily keeps the best points that are far enough apart to be different areas.
/// </summary>
public static class AreaSearch
{
    public static ImmutableArray<GeoPoint> Grid(GeoPoint centre, double radiusMeters, double stepMeters)
    {
        var steps = (int)(radiusMeters / stepMeters);
        return (from north in Enumerable.Range(-steps, 2 * steps + 1)
                from east in Enumerable.Range(-steps, 2 * steps + 1)
                let point = GeoMath.Offset(centre, north * stepMeters, east * stepMeters)
                where GeoMath.Meters(centre, point) <= radiusMeters && GeoMath.InAstana(point)
                select point).ToImmutableArray();
    }

    public static int Score(AreaReport report, CommutePlan commute, Preferences p)
    {
        var weight = p.LifestyleWeight + p.CommuteWeight;
        if (weight == 0) return 0;
        return (int)Math.Round(100 * (report.LifestyleScore * p.LifestyleWeight + HomeMatcher.CommuteScore(commute, p) * p.CommuteWeight) / weight);
    }

    /// <summary>A friendly name: the closest named bus stop, otherwise the closest named place.</summary>
    public static string Describe(GeoPoint at, PlaceIndex index)
    {
        var named = index.Near(at, 700).Where(x => !string.IsNullOrWhiteSpace(x.Place.Name)).ToList();
        var stop = named.FirstOrDefault(x => x.Place.Category == PlaceCatalog.BusStop).Place;
        return stop is not null ? $"Around {stop.Name} stop"
            : named.FirstOrDefault().Place is { } place ? $"Near {place.Name}"
            : "Unnamed area";
    }

    public static ImmutableArray<AreaCandidate> Best(Preferences p, PlaceIndex index, TransitNetwork network,
        int count = 6, double radiusMeters = 6000, double stepMeters = 450, double minSeparationMeters = 1100)
    {
        var scored = Grid(p.Destination, radiusMeters, stepMeters)
            .Select(point => (point, report: AreaAnalysis.Analyse(point, index, p.AllNeeds, p.MustHaves, p.WalkLimit)))
            // Skip empty land: nothing to walk to and no bus stop.
            .Where(x => x.report.SatisfiedCount > 0 || x.report.BusStopsWithinLimit > 0)
            .Where(x => x.report.MissingRequired.IsEmpty)
            .Select(x => (x.point, x.report, commute: JourneyPlanner.Plan(p.ToJourney(x.point), network, index)))
            .Where(x => x.commute.TotalMinutes is { } m && m <= p.MaxCommuteMinutes)
            .Select(x => (x.point, x.report, x.commute, score: Score(x.report, x.commute, p)))
            .OrderByDescending(x => x.score)
            .ToList();

        // Greedy fold: accept a point only if it is far from every point already accepted.
        var picked = scored.Aggregate(ImmutableList<(GeoPoint point, AreaReport report, CommutePlan commute, int score)>.Empty,
            (acc, x) => acc.Count >= count || acc.Any(a => GeoMath.Meters(a.point, x.point) < minSeparationMeters) ? acc : acc.Add(x));

        return picked.Select(x => new AreaCandidate(x.point, Describe(x.point, index), x.report, x.commute, x.score)).ToImmutableArray();
    }
}
