using System.Collections.Immutable;

namespace Dwellio.Core.Transit;

/// <summary>
/// Explicit, tunable assumptions behind the seat estimate. Shown on the About page so the model
/// can be questioned. Defaults describe a typical 12 m Astana city bus.
/// </summary>
public sealed record BusAssumptions(
    int Seats = 30,
    int Capacity = 95,
    double DoorSeconds = 8,
    double SecondsPerPassenger = 2.5,
    int HeadwayMinutes = 10)
{
    public static BusAssumptions Default { get; } = new();
}

/// <summary>Estimated passenger flow at one stop for one hour of the day.</summary>
public sealed record StopLoad(
    TransitStop Stop,
    int Index,
    double Boarding,
    double Alighting,
    double OnBoard,
    double SeatChance,
    int? SeatFreeFromIndex);

/// <summary>
/// Estimates how full the bus is along a route.
///
/// The open dataset has no passenger counts, but it does have dwell time: how long the bus stood
/// at each stop. Doors stay open longer when more people get on and off, so
///   activity = max(0, dwell − door time) / seconds per passenger.
/// Early stops are mostly boardings and late stops mostly alightings (a linear split). Folding
/// over the stops in order gives the on-board load, and from that the chance of a free seat.
/// The result is a model, not a measurement — the UI always says so.
/// </summary>
public static class SeatModel
{
    public static double Activity(double dwellSeconds, BusAssumptions a) =>
        Math.Max(0, dwellSeconds - a.DoorSeconds) / a.SecondsPerPassenger;

    /// <summary>Share of activity at stop <paramref name="index"/> that is boarding (1 at the first stop, 0 at the last).</summary>
    public static double BoardingShare(int index, int stopCount) =>
        stopCount <= 1 ? 1 : 1 - (double)index / (stopCount - 1);

    /// <summary>
    /// Chance that a person boarding gets a seat. The load computed above is an average; real
    /// buses vary a lot (bunching, a delayed bus collects two buses' worth of people), so the
    /// passengers ahead of you are treated as roughly normally distributed around
    /// "on board + half of the people boarding with you", with a spread that grows with the load.
    /// Φ is approximated by the logistic function 1 / (1 + e^(−1.702·z)).
    /// Clamped to 2–95 %: a seat is never guaranteed or impossible.
    /// </summary>
    public static double SeatChance(double onBoardBeforeBoarding, double boarding, BusAssumptions a)
    {
        var ahead = onBoardBeforeBoarding + boarding / 2;
        var spread = 3 + 0.25 * onBoardBeforeBoarding;
        var z = (a.Seats - 0.5 - ahead) / spread;
        return Math.Clamp(1 / (1 + Math.Exp(-1.702 * z)), 0.02, 0.95);
    }

    public static ImmutableArray<StopLoad> Profile(TransitRoute route, int hour, BusAssumptions? assumptions = null)
    {
        var a = assumptions ?? BusAssumptions.Default;
        var n = route.Stops.Length;
        var activity = route.Stops.Select(s => s.Dwell(hour) is { } d ? Activity(d, a) : (double?)null).ToArray();
        // Terminals have no meaningful dwell (layover); use the route's typical stop activity instead.
        var typical = activity.Where(x => x is not null).Select(x => x!.Value).DefaultIfEmpty(0).Average();

        // The fold: the state is (load so far, stops built so far). Each stop is a pure step.
        var (_, loads) = route.Stops.Select((stop, i) => (stop, i)).Aggregate(
            (Load: 0.0, Built: ImmutableArray<StopLoad>.Empty),
            (state, x) =>
            {
                var total = activity[x.i] ?? typical;
                var share = BoardingShare(x.i, n);
                var alighting = Math.Min(state.Load, total * (1 - share));
                var boarding = x.i == n - 1 ? 0 : total * share;
                var standing = state.Load - alighting;
                var load = Math.Clamp(standing + boarding, 0, a.Capacity);
                var step = new StopLoad(x.stop, x.i, boarding, alighting, load, SeatChance(standing, boarding, a), null);
                return (load, state.Built.Add(step));
            });

        // Second pass: for someone who must stand, from which stop is a seat likely to be free?
        return loads.Select(l => l with
        {
            SeatFreeFromIndex = loads.Skip(l.Index + 1).FirstOrDefault(next => next.OnBoard < a.Seats)?.Index
        }).ToImmutableArray();
    }

    /// <summary>Expected minutes standing on a ride from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static double ExpectedStandingMinutes(ImmutableArray<StopLoad> profile, int from, int to, int hour)
    {
        var start = profile[from];
        var seatAt = start.SeatFreeFromIndex is { } k && k < to ? k : to;
        var standingSeconds = (profile[seatAt].Stop.Offset(hour) ?? 0) - (start.Stop.Offset(hour) ?? 0);
        return (1 - start.SeatChance) * Math.Max(0, standingSeconds) / 60;
    }
}
