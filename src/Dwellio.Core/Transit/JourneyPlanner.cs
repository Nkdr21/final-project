using System.Collections.Immutable;
using Dwellio.Core.Geo;
using Dwellio.Core.Places;

namespace Dwellio.Core.Transit;

public enum CommutePriority { Balanced, Seat, Fastest }

public sealed record JourneyRequest(GeoPoint From, GeoPoint To, int ArriveByHour, int MaxWalkMinutes, CommutePriority Priority);

/// <summary>How an option differs from simply walking to the closest stop at the usual time.</summary>
public enum Tactic { NearestStop, BoardEarlierStop, LeaveEarlier, OtherRoute }

/// <summary>One concrete way to make the trip on a bus with open GPS data.</summary>
public sealed record JourneyOption(
    TransitRoute Route,
    int BoardIndex,
    int AlightIndex,
    int Hour,
    int WalkToStop,
    int WaitMinutes,
    int RideMinutes,
    int WalkFromStop,
    double SeatChance,
    double StandingMinutes,
    Tactic Tactic)
{
    /// <summary>Suggested time to leave home, in minutes after midnight (5 min safety buffer).</summary>
    public int LeaveAtMinute => (Hour + 1) * 60 - TotalMinutes - 5;
    public string LeaveAt => $"{LeaveAtMinute / 60:00}:{LeaveAtMinute % 60:00}";

    public TransitStop Board => Route.Stops[BoardIndex];
    public TransitStop Alight => Route.Stops[AlightIndex];
    public int Stops => AlightIndex - BoardIndex;
    public int TotalMinutes => WalkToStop + WaitMinutes + RideMinutes + WalkFromStop;
}

/// <summary>Result of planning a commute. Exactly one case applies (a closed set of outcomes).</summary>
public abstract record CommutePlan
{
    public abstract int? TotalMinutes { get; }
}

/// <summary>Ranked options on routes from the open GTFS dataset (best first).</summary>
public sealed record TransitPlan(ImmutableArray<JourneyOption> Options) : CommutePlan
{
    public JourneyOption Best => Options[0];
    public override int? TotalMinutes => Best.TotalMinutes;
}

/// <summary>No route data covers the trip: a rough estimate between real OpenStreetMap stops.</summary>
public sealed record EstimatedBusPlan(Place FromStop, Place ToStop, int WalkToStop, int WaitMinutes, int RideMinutes, int WalkFromStop) : CommutePlan
{
    public override int? TotalMinutes => WalkToStop + WaitMinutes + RideMinutes + WalkFromStop;
}

/// <summary>Close enough to walk.</summary>
public sealed record WalkPlan(int Minutes) : CommutePlan
{
    public override int? TotalMinutes => Minutes;
}

public sealed record NoCommute(string Reason) : CommutePlan
{
    public override int? TotalMinutes => null;
}

public static class JourneyPlanner
{
    /// <summary>Trips this short are suggested on foot.</summary>
    public const int WalkInsteadMinutes = 20;

    /// <summary>Average Astana bus speed between stops for the fallback estimate (≈17 km/h).</summary>
    private const double EstimatedBusMetersPerMinute = 285;

    // ---- Composable scoring: each criterion is a function; a priority is a weighted sum of them. ----

    public static readonly Func<JourneyOption, double> Minutes = o => o.TotalMinutes;
    public static readonly Func<JourneyOption, double> Standing = o => o.StandingMinutes;
    public static readonly Func<JourneyOption, double> Walking = o => o.WalkToStop + o.WalkFromStop;

    public static Func<T, double> Combine<T>(params (Func<T, double> Criterion, double Weight)[] parts) =>
        item => parts.Sum(p => p.Criterion(item) * p.Weight);

    /// <summary>Lower is better. "Seat" makes a standing minute cost three sitting minutes.</summary>
    public static Func<JourneyOption, double> Score(CommutePriority priority) => priority switch
    {
        CommutePriority.Fastest => Combine((Minutes, 1.0), (Walking, 0.1)),
        CommutePriority.Seat => Combine((Minutes, 1.0), (Standing, 3.0)),
        _ => Combine((Minutes, 1.0), (Standing, 1.0), (Walking, 0.2)),
    };

    public static CommutePlan Plan(JourneyRequest request, TransitNetwork network, PlaceIndex stops, BusAssumptions? assumptions = null)
    {
        var direct = GeoMath.WalkMinutes(request.From, request.To);
        if (direct <= WalkInsteadMinutes) return new WalkPlan(direct);

        var options = Options(request, network, assumptions ?? BusAssumptions.Default);
        if (!options.IsEmpty) return new TransitPlan(options);
        return (CommutePlan?)Estimate(request, stops) ?? new NoCommute("No bus stop within walking distance of both places.");
    }

    /// <summary>All sensible journeys on GTFS routes, ranked by the requested priority, one per tactic.</summary>
    public static ImmutableArray<JourneyOption> Options(JourneyRequest request, TransitNetwork network, BusAssumptions a)
    {
        var score = Score(request.Priority);
        var candidates = network.Routes.SelectMany(route => RouteOptions(route, request, a)).ToList();
        if (candidates.Count == 0) return [];

        // Baseline = closest boarding stop, usual departure time, best route. Tactics are labelled against it.
        var usualHour = candidates.Max(o => o.Hour);
        var baseline = candidates.Where(o => o.Hour == usualHour).OrderBy(o => o.WalkToStop).ThenBy(score).First();
        JourneyOption Label(JourneyOption o) => o with
        {
            Tactic = o.Hour < usualHour ? Tactic.LeaveEarlier
                : o.Route.Key != baseline.Route.Key ? Tactic.OtherRoute
                : o.BoardIndex < baseline.BoardIndex ? Tactic.BoardEarlierStop
                : Tactic.NearestStop,
        };

        var best = candidates.Select(Label).OrderBy(score).DistinctBy(o => (o.Tactic, o.Route.Ref)).ToList();
        // Keep the usual trip as a reference. Fold over the rest in ranked order and only add an
        // alternative if nothing already kept is at least as good on both time and seat.
        var usual = best.First(o => o.Tactic == Tactic.NearestStop);
        var kept = best.Where(o => o != usual).Aggregate(ImmutableList.Create(usual),
            (acc, o) => acc.Any(k => AtLeastAsGood(k, o)) ? acc : acc.Add(o));
        return kept.OrderBy(score).Take(4).ToImmutableArray();
    }

    /// <summary>True when <paramref name="a"/> is no slower than <paramref name="b"/> and no worse for a seat.</summary>
    public static bool AtLeastAsGood(JourneyOption a, JourneyOption b) =>
        a.TotalMinutes <= b.TotalMinutes && a.SeatChance >= b.SeatChance - 0.01;

    /// <summary>True when <paramref name="a"/> is at least as good as <paramref name="b"/> and strictly better on time or seat.</summary>
    public static bool Dominates(JourneyOption a, JourneyOption b) =>
        AtLeastAsGood(a, b) && (a.TotalMinutes < b.TotalMinutes || a.SeatChance > b.SeatChance + 0.01);

    private static IEnumerable<JourneyOption> RouteOptions(TransitRoute route, JourneyRequest r, BusAssumptions a)
    {
        var stops = route.Stops.Select((stop, i) => (stop, i)).ToList();
        var boards = stops.Select(s => (s.i, Walk: GeoMath.WalkMinutes(r.From, s.stop.Location))).Where(s => s.Walk <= r.MaxWalkMinutes).ToList();
        var alights = stops.Select(s => (s.i, Walk: GeoMath.WalkMinutes(s.stop.Location, r.To))).Where(s => s.Walk <= r.MaxWalkMinutes).ToList();
        if (boards.Count == 0 || alights.Count == 0) return [];

        // Usual departure: the hour before arrival; "leave earlier" checks one hour before that.
        var hours = new[] { r.ArriveByHour - 1, r.ArriveByHour - 2 }.Where(h => h >= 5).Distinct();
        return from hour in hours
               let profile = SeatModel.Profile(route, hour, a)
               from board in boards
               let alight = alights.Where(x => x.i > board.i)
                   .Select(x => (x.i, x.Walk, Ride: RideMinutes(route, board.i, x.i, hour)))
                   .OrderBy(x => x.Walk + x.Ride)
                   .Cast<(int i, int Walk, int Ride)?>()
                   .FirstOrDefault()
               where alight is not null
               let ride = alight.Value.Ride
               select new JourneyOption(route, board.i, alight.Value.i, hour, board.Walk, a.HeadwayMinutes / 2, ride,
                   alight.Value.Walk, profile[board.i].SeatChance,
                   Math.Round(SeatModel.ExpectedStandingMinutes(profile, board.i, alight.Value.i, hour), 1),
                   Tactic.NearestStop);
    }

    /// <summary>Ride time from GPS medians; at least one minute per stop if the data is inconsistent.</summary>
    public static int RideMinutes(TransitRoute route, int from, int to, int hour)
    {
        var seconds = (route.Stops[to].Offset(hour) ?? 0) - (route.Stops[from].Offset(hour) ?? 0);
        return Math.Max(to - from, (int)Math.Round(seconds / 60));
    }

    /// <summary>Fallback when no open route data covers the trip: nearest real stops and an average bus speed.</summary>
    public static EstimatedBusPlan? Estimate(JourneyRequest r, PlaceIndex index)
    {
        var radius = GeoMath.WalkRadiusMeters(r.MaxWalkMinutes);
        var from = index.Nearest(r.From, PlaceCatalog.BusStop, radius);
        var to = index.Nearest(r.To, PlaceCatalog.BusStop, radius);
        if (from is null || to is null) return null;
        var ride = (int)Math.Ceiling(GeoMath.Meters(from.Value.Place.Location, to.Value.Place.Location) * GeoMath.DetourFactor / EstimatedBusMetersPerMinute);
        return new(from.Value.Place, to.Value.Place, GeoMath.WalkMinutes(r.From, from.Value.Place.Location),
            // Frequency (and possible transfers) unknown: assume a full headway of waiting.
            BusAssumptions.Default.HeadwayMinutes, ride,GeoMath.WalkMinutes(to.Value.Place.Location, r.To));
    }
}
