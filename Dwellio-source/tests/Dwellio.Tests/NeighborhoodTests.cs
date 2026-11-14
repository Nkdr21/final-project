using Dwellio.Core.Models;
using Dwellio.Core.Services;

namespace Dwellio.Tests;

public class NeighborhoodTests
{
    private static Complex Example(int id, string name, params Amenity[] amenities) =>
        new(id, name, "Demo", 0, 0, [.. amenities], "Demo");
    private static readonly Amenity Gym = new("gym", "Gym", 0, true, "Residents");
    private static readonly Amenity Park = new("park", "Park", 12, true, "Public");

    [Fact]
    public void Search_is_case_insensitive_and_trims_spaces()
    {
        var homes = new[] { Example(1, "Garden House"), Example(2, "River House") };
        Assert.Equal(1, Assert.Single(NeighborhoodService.Filter(homes, " GARDEN ", [], 15)).Id);
    }
    [Fact]
    public void Every_selected_category_must_match()
    {
        var homes = new[] { Example(1, "One", Gym), Example(2, "Two", Gym, Park) };
        Assert.Equal(2, Assert.Single(NeighborhoodService.Filter(homes, "", ["gym", "park"], 15)).Id);
    }
    [Fact]
    public void Walking_limit_is_inclusive()
    {
        var homes = new[] { Example(1, "One", Park) };
        Assert.Empty(NeighborhoodService.Filter(homes, "", ["park"], 11));
        Assert.Single(NeighborhoodService.Filter(homes, "", ["park"], 12));
    }
    [Fact]
    public void Inside_and_nearby_groups_do_not_overlap()
    {
        var home = Example(1, "One", Gym, Park);
        Assert.Equal(Gym, Assert.Single(NeighborhoodService.Inside(home)));
        Assert.Equal(Park, Assert.Single(NeighborhoodService.Nearby(home)));
    }
    [Fact]
    public void Missing_amenities_are_not_invented()
    {
        var home = Example(1, "Empty");
        Assert.Empty(NeighborhoodService.Inside(home));
        Assert.Empty(NeighborhoodService.Nearby(home));
        Assert.Empty(NeighborhoodService.Filter([home], "", ["gym"], 15));
    }
}
