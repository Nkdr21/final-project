using Dwellio.Core.Geo;
using Dwellio.Core.Matching;
using Dwellio.Core.Models;
using Dwellio.Core.Places;
using Dwellio.Core.Transit;
using static Dwellio.Tests.TestData;

namespace Dwellio.Tests;

public class HomeMatcherTests
{
    private static readonly Apartment Flat = new(1, 1, "Flat", 200_000, 20_000, 200_000, 1, 40, true, true);
    private static readonly Complex Building = new(1, "Building", "Street 1", Centre, "");
    private static readonly PlaceIndex Index = PlaceIndex.Create([At("gym", 200), At("cafe", 300)]);
    private static readonly Preferences Plan = new() { Budget = 250_000, Needs = ["gym", "cafe"], MaxCommuteMinutes = 40 };

    private static AreaReport Area(Preferences p) => AreaAnalysis.Analyse(Centre, Index, p.AllNeeds, p.MustHaves, p.WalkLimit);
    private static HomeMatch Evaluate(Apartment a, Preferences p, CommutePlan? commute = null) =>
        HomeMatcher.Evaluate(a, Building, Area(p), commute ?? new WalkPlan(15), p);

    [Fact]
    public void Good_home_is_eligible_with_reasons()
    {
        var match = Evaluate(Flat, Plan);
        Assert.True(match.Eligible);
        Assert.Contains(match.Reasons, r => r.StartsWith("Within budget"));
        Assert.Contains(match.Reasons, r => r.StartsWith("Gym:"));
        Assert.InRange(match.Score, 1, 100);
    }

    [Fact]
    public void Over_budget_is_excluded_with_an_explanation()
    {
        var match = Evaluate(Flat with { Rent = 300_000 }, Plan);
        Assert.False(match.Eligible);
        Assert.Contains(match.Exclusions, e => e.Contains("above your"));
    }

    [Fact]
    public void Missing_must_have_excludes_but_missing_want_only_lowers_score()
    {
        var withWant = Evaluate(Flat, Plan with { Needs = ["gym", "pharmacy"] });
        Assert.True(withWant.Eligible);
        Assert.Contains(withWant.Gaps, g => g.Contains("pharmacy", StringComparison.OrdinalIgnoreCase));

        var withMust = Evaluate(Flat, Plan with { MustHaves = ["pharmacy"] });
        Assert.False(withMust.Eligible);
        Assert.Contains(withMust.Exclusions, e => e.StartsWith("Must-have missing"));
    }

    [Fact]
    public void Commute_over_the_limit_excludes_the_home() =>
        Assert.False(Evaluate(Flat, Plan, new WalkPlan(55)).Eligible);

    [Fact]
    public void Unknown_commute_never_scores_as_a_good_commute()
    {
        Assert.Equal(0, HomeMatcher.CommuteScore(new NoCommute("none"), Plan));
        var unknown = Evaluate(Flat, Plan, new NoCommute("No bus"));
        Assert.True(unknown.Eligible);
        Assert.Contains("No bus", unknown.Gaps);
        Assert.True(unknown.Score < Evaluate(Flat, Plan, new WalkPlan(10)).Score);
    }

    [Fact]
    public void Estimates_score_below_an_equally_long_known_trip()
    {
        var stop = At(PlaceCatalog.BusStop, 0);
        var estimate = new EstimatedBusPlan(stop, stop, 5, 5, 10, 5);
        Assert.True(HomeMatcher.CommuteScore(estimate, Plan) < HomeMatcher.CommuteScore(new WalkPlan(25), Plan));
    }

    [Fact]
    public void Weights_change_the_ranking()
    {
        var cheap = Flat with { Id = 2, Rent = 120_000 };
        var priceFirst = Plan with { PriceWeight = 5, LifestyleWeight = 0, CommuteWeight = 0 };
        Assert.True(Evaluate(cheap, priceFirst).Score > Evaluate(Flat, priceFirst).Score);
        var lifestyleOnly = Plan with { PriceWeight = 0, LifestyleWeight = 5, CommuteWeight = 0 };
        Assert.Equal(Evaluate(cheap, lifestyleOnly).Score, Evaluate(Flat, lifestyleOnly).Score);
    }

    [Fact]
    public void Unknown_utilities_stay_unknown_and_deposit_is_separate()
    {
        var cost = HomeMatcher.Cost(Flat with { Utilities = null }, Plan);
        Assert.True(cost.UtilitiesUnknown);
        Assert.Equal(200_000 + Plan.TransportCost + Plan.GymCost, cost.KnownTotal);
        Assert.Equal(cost.KnownTotal + 200_000, cost.MoveInTotal);
        Assert.Equal(0, HomeMatcher.Cost(Flat, Plan with { Needs = ["cafe"] }).Gym);
    }

    [Fact]
    public void Rank_puts_eligible_homes_first_then_by_score()
    {
        var good = Evaluate(Flat, Plan);
        var excluded = Evaluate(Flat with { Id = 3, Rent = 900_000 }, Plan);
        var ranked = HomeMatcher.Rank([excluded, good]);
        Assert.Equal([1, 3], ranked.Select(m => m.Apartment.Id).ToArray());
    }

    [Theory]
    [InlineData(0, 1, 9, 45, 10, "budget")]
    [InlineData(100000, 7, 9, 45, 10, "Rooms")]
    [InlineData(100000, 1, 3, 45, 10, "Arrival")]
    [InlineData(100000, 1, 9, 500, 10, "commute")]
    [InlineData(100000, 1, 9, 45, 90, "Walking")]
    public void Validation_explains_the_problem(decimal budget, int rooms, int hour, int commute, int walk, string expected)
    {
        var result = HomeMatcher.Validate(new Preferences { Budget = budget, MinRooms = rooms, ArriveByHour = hour, MaxCommuteMinutes = commute, WalkLimit = walk });
        Assert.False(result.Success);
        Assert.Contains(expected, result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Destination_outside_astana_is_rejected() =>
        Assert.False(HomeMatcher.Validate(new Preferences { DestinationLat = 43.2, DestinationLon = 76.9 }).Success);

    [Fact]
    public void Sanitize_repairs_tampered_storage()
    {
        var tampered = new Preferences { Budget = -5, Needs = ["gym", "hack"], MustHaves = default, DestinationLat = 0, DestinationLon = 0, DestinationName = " " };
        var clean = HomeMatcher.Sanitize(tampered);
        Assert.True(HomeMatcher.Validate(clean).Success);
        Assert.Equal(["gym"], clean.Needs.ToArray());
        Assert.Empty(clean.MustHaves);
        Assert.True(GeoMath.InAstana(clean.Destination));
    }
}

public class AreaSearchTests
{
    [Fact]
    public void Grid_stays_inside_radius_and_city()
    {
        var grid = AreaSearch.Grid(new(51.1, 71.42), 1000, 250);
        Assert.NotEmpty(grid);
        Assert.All(grid, p => Assert.True(GeoMath.Meters(new(51.1, 71.42), p) <= 1000.01));
    }

    [Fact]
    public void Best_areas_are_distinct_satisfy_must_haves_and_are_sorted()
    {
        var plan = new Preferences { MustHaves = ["pharmacy"], Needs = ["groceries", "cafe"] };
        var areas = AreaSearch.Best(plan, RealPlaces.Value, RealNetwork.Value);
        Assert.NotEmpty(areas);
        Assert.Equal(areas.OrderByDescending(a => a.Score).Select(a => a.Score), areas.Select(a => a.Score));
        Assert.All(areas, a => Assert.Empty(a.Report.MissingRequired));
        Assert.All(areas, a => Assert.True(a.Commute.TotalMinutes <= plan.MaxCommuteMinutes));
        foreach (var (a, b) in areas.SelectMany((a, i) => areas.Skip(i + 1).Select(b => (a, b))))
            Assert.True(GeoMath.Meters(a.Location, b.Location) >= 1100);
    }
}
