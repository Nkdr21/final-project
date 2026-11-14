using System.Collections.Immutable;
using Dwellio.Core.Models;
using Dwellio.Core.Services;
using Dwellio.Web.Services;

namespace Dwellio.Tests;

public class RecommendationTests
{
    private readonly Apartment apartment = new(1, 1, "Study home", 150000, 15000, 100000, 1, 40, true, true);
    private readonly Complex complex = new(1, "Demo complex", "Astana", 51.12, 71.43,
        [new("gym", "Gym", 0, true, "Residents"), new("groceries", "Shop", 4, false, "Public")], "Demo");
    private static RoutesFound Routes(int outward = 25, int back = 35) => new(new(
        [new("Direct", outward, 5, 0, Crowding.High, "A", "B", "Out")],
        [new("Direct", back, 5, 0, Crowding.Medium, "B", "A", "Return")]));

    [Fact] public void Over_budget_is_excluded() => Assert.Null(RecommendationEngine.Evaluate(apartment, complex, new(Budget: 140000), Routes()));
    [Fact] public void Unavailable_home_is_excluded() => Assert.False(RecommendationEngine.FitsBudget(new())(apartment with { Available = false }));
    [Fact] public void Furniture_requirement_is_respected() => Assert.False(RecommendationEngine.FitsBudget(new(FurnishedOnly: true))(apartment with { Furnished = false }));
    [Fact] public void Required_missing_amenity_excludes_home() => Assert.Null(RecommendationEngine.Evaluate(apartment, complex, new(RequiredCategories: "football"), Routes()));
    [Fact]
    public void Desired_missing_amenity_explains_gap_without_excluding()
    {
        var result = RecommendationEngine.Evaluate(apartment, complex, new(Categories: "football"), Routes());
        Assert.NotNull(result); Assert.Contains(result.Missing, x => x.Contains("Football"));
    }
    [Fact] public void Return_trip_limit_is_checked_independently() => Assert.Null(RecommendationEngine.Evaluate(apartment, complex, new(MaxTravelMinutes: 40), Routes(back: 55)));
    [Fact]
    public void No_route_data_is_explicit_and_not_fabricated()
    {
        var result = RecommendationEngine.Evaluate(apartment, complex, new(), new RoutesUnavailable("Offline"));
        Assert.NotNull(result); Assert.Null(result.Outbound); Assert.Contains("Offline", result.Missing);
    }
    [Fact]
    public void Unknown_utilities_remain_unknown_and_deposit_is_separate()
    {
        var result = RecommendationEngine.CalculateCost(apartment with { Utilities = null }, complex, new());
        Assert.True(result.UtilitiesUnknown); Assert.Equal(158000, result.KnownTotal);
        Assert.Equal(100000, result.Deposit); Assert.Equal(258000, result.MoveInTotal);
    }
    [Fact] public void Included_gym_is_not_charged_twice() => Assert.Equal(0, RecommendationEngine.CalculateCost(apartment, complex, new()).GymCost);
    [Fact]
    public void Separate_gym_is_charged_only_when_requested()
    {
        var noGym = complex with { Amenities = [] };
        Assert.Equal(15000, RecommendationEngine.CalculateCost(apartment, noGym, new()).GymCost);
        Assert.Equal(0, RecommendationEngine.CalculateCost(apartment, noGym, new(Categories: "park")).GymCost);
    }
    [Fact]
    public void Evaluation_does_not_modify_input_and_is_deterministic()
    {
        var p = new Preferences(); var copy = p with { Budget = 300000 };
        Assert.Equal(220000, p.Budget); Assert.Equal(300000, copy.Budget);
        var first = RecommendationEngine.Evaluate(apartment, complex, p, Routes());
        var second = RecommendationEngine.Evaluate(apartment, complex, p, Routes());
        Assert.Equal(first!.Score, second!.Score); Assert.Equal(first.Reasons.ToArray(), second.Reasons.ToArray());
    }
    [Fact]
    public void Priorities_change_the_ranking()
    {
        var cheap = apartment with { Rent = 100000 };
        var expensive = apartment with { Rent = 190000 };
        var empty = complex with { Amenities = [] };
        var price = new Preferences(PriceWeight: 5, LifestyleWeight: 0, TravelWeight: 0);
        var life = price with { PriceWeight = 0, LifestyleWeight = 5 };
        Assert.True(RecommendationEngine.Evaluate(cheap, empty, price, Routes())!.Score > RecommendationEngine.Evaluate(expensive, complex, price, Routes())!.Score);
        Assert.True(RecommendationEngine.Evaluate(cheap, empty, life, Routes())!.Score < RecommendationEngine.Evaluate(expensive, complex, life, Routes())!.Score);
    }
    [Fact]
    public void Comfort_can_choose_a_slower_less_crowded_trip()
    {
        Trip[] trips = [new("Fast", 25, 5, 0, Crowding.High, "A", "B", "Out"), new("Quiet", 35, 8, 0, Crowding.Low, "A", "B", "Out")];
        Assert.Equal("Fast", RecommendationEngine.ChooseTrip(trips, new())!.Name);
        Assert.Equal("Quiet", RecommendationEngine.ChooseTrip(trips, new(Priority: "comfort"))!.Name);
    }
    [Theory]
    [InlineData(-1, 1, 1)]
    [InlineData(0, 0, 0)]
    public void Invalid_weights_are_rejected(int price, int life, int travel) => Assert.False(RecommendationEngine.Validate(new(PriceWeight: price, LifestyleWeight: life, TravelWeight: travel)).Success);
    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void Invalid_hours_are_rejected(int hour) => Assert.False(RecommendationEngine.Validate(new(ArrivalHour: hour)).Success);
    [Fact] public void Negative_cost_is_rejected() => Assert.False(RecommendationEngine.Validate(new(GymCost: -10)).Success);
    [Fact]
    public void Comparison_limit_and_removal_work()
    {
        var state = new SearchState();
        Assert.True(state.ToggleCompare(1).Success); state.ToggleCompare(2); state.ToggleCompare(3);
        Assert.False(state.ToggleCompare(4).Success); Assert.True(state.ToggleCompare(2).Success);
        Assert.True(state.ToggleCompare(4).Success); Assert.Equal(3, state.CompareIds.Count);
    }
    [Fact]
    public async Task Demo_routes_handle_missing_unavailable_and_cancellation()
    {
        var provider = new DemoRouteProvider();
        Assert.IsType<RoutesMissing>(await provider.GetAsync(complex, new(Destination: "other")));
        Assert.IsType<RoutesUnavailable>(await provider.GetAsync(complex, new(Destination: "unavailable")));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetAsync(complex, new(), new CancellationToken(true)));
    }
    [Fact]
    public async Task Demo_commutes_are_directional_and_time_dependent()
    {
        var provider = new DemoRouteProvider();
        var peak = Assert.IsType<RoutesFound>(await provider.GetAsync(complex, new()));
        var quiet = Assert.IsType<RoutesFound>(await provider.GetAsync(complex, new(ArrivalHour: 12)));
        Assert.True(peak.Value.Outbound[0].Minutes > quiet.Value.Outbound[0].Minutes);
        Assert.NotEqual(peak.Value.Outbound[0].Minutes, peak.Value.Return[0].Minutes);
        Assert.All(peak.Value.Outbound, x => Assert.True(x.IsDemo));
    }
}
