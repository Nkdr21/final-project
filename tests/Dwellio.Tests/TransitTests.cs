using Dwellio.Core.Geo;
using Dwellio.Core.Places;
using Dwellio.Core.Transit;
using static Dwellio.Tests.TestData;

namespace Dwellio.Tests;

public class SeatModelTests
{
    private static readonly BusAssumptions A = BusAssumptions.Default;

    [Theory]
    [InlineData(8, 0)]       // doors only: nobody got on or off
    [InlineData(3, 0)]       // shorter than door time never goes negative
    [InlineData(33, 10)]     // (33 − 8) / 2.5
    public void Activity_comes_from_dwell_time(double dwell, double expected) =>
        Assert.Equal(expected, SeatModel.Activity(dwell, A), 6);

    [Fact]
    public void Boarding_share_falls_linearly_along_the_route()
    {
        Assert.Equal(1, SeatModel.BoardingShare(0, 11));
        Assert.Equal(0.5, SeatModel.BoardingShare(5, 11));
        Assert.Equal(0, SeatModel.BoardingShare(10, 11));
    }

    [Fact]
    public void Seat_chance_falls_as_the_bus_fills_and_stays_within_bounds()
    {
        var chances = new[] { 0, 15, 25, 30, 40, 60, 95 }.Select(load => SeatModel.SeatChance(load, 4, A)).ToList();
        Assert.Equal(chances.OrderByDescending(x => x), chances);
        Assert.Equal(0.95, chances[0]);   // never "guaranteed"
        Assert.Equal(0.02, chances[^1]);  // never "impossible"
    }

    [Fact]
    public void Profile_fold_keeps_load_physical()
    {
        var profile = SeatModel.Profile(Line(stops: 20, dwellSeconds: 40), hour: 8);
        Assert.Equal(20, profile.Length);
        Assert.All(profile, s => Assert.InRange(s.OnBoard, 0, A.Capacity));
        Assert.All(profile.Zip(profile.Skip(1)), pair => Assert.True(pair.Second.Alighting <= pair.First.OnBoard + 1e-9));
        Assert.Equal(0, profile[^1].OnBoard, 6); // everyone leaves at the terminus
    }

    [Fact]
    public void Busy_middle_of_route_is_worse_than_the_start()
    {
        var profile = SeatModel.Profile(Line(stops: 20, dwellSeconds: 40), hour: 8);
        Assert.True(profile[0].SeatChance > profile[10].SeatChance);
        Assert.True(profile[10].OnBoard > profile[1].OnBoard);
    }

    [Fact]
    public void Standing_rider_learns_where_seats_free_up()
    {
        var profile = SeatModel.Profile(Line(stops: 20, dwellSeconds: 40), hour: 8);
        var crowded = profile.First(p => p.OnBoard > A.Seats);
        Assert.NotNull(crowded.SeatFreeFromIndex);
        Assert.True(profile[crowded.SeatFreeFromIndex!.Value].OnBoard < A.Seats);
    }

    [Fact]
    public void Real_route_46_is_more_crowded_mid_route_than_at_its_ends()
    {
        var route = RealNetwork.Value.Find("46-1")!;
        var profile = SeatModel.Profile(route, 8);
        var middle = profile[profile.Length / 2].OnBoard;
        Assert.True(middle > profile[1].OnBoard && middle > profile[^2].OnBoard);
    }
}

public class TransitNetworkTests
{
    [Fact]
    public void Bundled_gtfs_summary_has_both_directions_of_three_routes()
    {
        var network = RealNetwork.Value;
        Assert.Equal(6, network.Routes.Length);
        Assert.Equal(["10", "12", "46"], network.Routes.Select(r => r.Ref).Distinct().Order().ToArray());
        Assert.All(network.Routes, r => Assert.All(r.Stops, s => Assert.True(GeoMath.InAstana(s.Location))));
        Assert.Contains("10.5281/zenodo.15769359", network.Source);
    }

    [Fact]
    public void Missing_hours_fall_back_to_the_nearest_hour_with_data()
    {
        var stop = RealNetwork.Value.Routes[0].Stops[5];
        Assert.Equal(stop.Offset(6), stop.Offset(2)); // no buses at 2 am → closest hour (06:00) is used
    }

    [Fact]
    public void Offsets_grow_along_a_real_route()
    {
        var route = RealNetwork.Value.Find("46-2")!;
        Assert.True(route.Stops[^1].Offset(8) > route.Stops[1].Offset(8));
    }
}

public class JourneyPlannerTests
{
    private static readonly PlaceIndex NoStops = PlaceIndex.Empty;

    [Fact]
    public void Short_trips_are_walked()
    {
        var plan = JourneyPlanner.Plan(new(Centre, East(800), 9, 10, CommutePriority.Balanced), Network(Line()), NoStops);
        var walk = Assert.IsType<WalkPlan>(plan);
        Assert.Equal(GeoMath.WalkMinutes(Centre, East(800)), walk.Minutes);
    }

    [Fact]
    public void Bus_option_boards_near_start_and_alights_near_destination()
    {
        var line = Line(stops: 20, startMetersEast: 0);
        var plan = JourneyPlanner.Plan(new(East(0), East(6000), 9, 10, CommutePriority.Fastest), Network(line), NoStops);
        var best = Assert.IsType<TransitPlan>(plan).Best;
        Assert.Equal(0, best.BoardIndex);
        Assert.Equal(15, best.AlightIndex);  // stop 15 is at 6000 m
        Assert.Equal(best.WalkToStop + best.WaitMinutes + best.RideMinutes + best.WalkFromStop, best.TotalMinutes);
    }

    [Fact]
    public void Walking_back_to_an_emptier_stop_is_offered_when_the_bus_fills_up()
    {
        // The bus fills up gradually; the user lives near stop 6, where it is already about full.
        var line = Line(stops: 20, dwellSeconds: 25, startMetersEast: 0);
        var request = new JourneyRequest(East(6 * 400), East(18 * 400), 9, 15, CommutePriority.Seat);
        var options = JourneyPlanner.Options(request, Network(line), BusAssumptions.Default);
        var usual = options.Single(o => o.Tactic == Tactic.NearestStop);
        Assert.Equal(6, usual.BoardIndex);
        var earlier = Assert.Single(options, o => o.Tactic == Tactic.BoardEarlierStop);
        Assert.True(earlier.BoardIndex < usual.BoardIndex);
        Assert.True(earlier.SeatChance > usual.SeatChance + 0.1);
        Assert.True(earlier.TotalMinutes > usual.TotalMinutes); // the trade-off is shown honestly
    }

    [Fact]
    public void Dominated_alternatives_are_hidden_but_the_usual_trip_stays()
    {
        var options = JourneyPlanner.Options(new(East(800), East(6800), 9, 15, CommutePriority.Balanced),
            Network(Line(stops: 30, dwellSeconds: 45, startMetersEast: -4000)), BusAssumptions.Default);
        Assert.Contains(options, o => o.Tactic == Tactic.NearestStop);
        Assert.DoesNotContain(options, o => o.Tactic != Tactic.NearestStop && options.Any(other => JourneyPlanner.Dominates(other, o)));
    }

    [Fact]
    public void Alternative_identical_to_the_usual_trip_is_not_shown()
    {
        // Same data every hour: "leave an hour earlier" is exactly as good as the usual trip.
        var options = JourneyPlanner.Options(new(East(0), East(6000), 9, 5, CommutePriority.Balanced),
            Network(Line(stops: 20, startMetersEast: 0)), BusAssumptions.Default);
        Assert.Equal(Tactic.NearestStop, Assert.Single(options).Tactic);
    }

    [Fact]
    public void Without_route_data_an_estimate_uses_real_stops()
    {
        var stops = PlaceIndex.Create([At(PlaceCatalog.BusStop, 100, "A"), At(PlaceCatalog.BusStop, 5900, "B")]);
        var plan = JourneyPlanner.Plan(new(Centre, East(6000), 9, 10, CommutePriority.Balanced), TransitNetwork.Empty, stops);
        var estimate = Assert.IsType<EstimatedBusPlan>(plan);
        Assert.Equal(("A", "B"), (estimate.FromStop.Name, estimate.ToStop.Name));
        Assert.True(estimate.TotalMinutes > estimate.RideMinutes);
    }

    [Fact]
    public void No_stops_anywhere_is_an_explicit_result()
    {
        var plan = JourneyPlanner.Plan(new(Centre, East(6000), 9, 10, CommutePriority.Balanced), TransitNetwork.Empty, NoStops);
        Assert.IsType<NoCommute>(plan);
        Assert.Null(plan.TotalMinutes);
    }

    [Fact]
    public void Combine_builds_a_weighted_sum_of_criteria()
    {
        var score = JourneyPlanner.Combine<int>((x => x, 2), (x => 10, 0.5));
        Assert.Equal(13, score(4));
    }

    [Fact]
    public void Real_trip_comfort_town_to_khan_shatyr_uses_bus_46()
    {
        var request = new JourneyRequest(new(51.0891, 71.4470), new(51.1325, 71.4039), 9, 12, CommutePriority.Seat);
        var plan = Assert.IsType<TransitPlan>(JourneyPlanner.Plan(request, RealNetwork.Value, RealPlaces.Value));
        Assert.Equal("46", plan.Best.Route.Ref);
        Assert.InRange(plan.Best.TotalMinutes, 25, 70);
        Assert.InRange(plan.Best.LeaveAtMinute, 6 * 60, 9 * 60);
    }
}
