using Dwellio.Core.Geo;
using Dwellio.Core.Places;
using static Dwellio.Tests.TestData;

namespace Dwellio.Tests;

public class GeoTests
{
    [Fact]
    public void Haversine_matches_known_distance()
    {
        // Astana IT University → Khan Shatyr is about 5.4 km in a straight line.
        var meters = GeoMath.Meters(new(51.09086, 71.41841), new(51.13250, 71.40386));
        Assert.InRange(meters, 4500, 5000);
    }

    [Fact]
    public void Distance_is_symmetric_and_zero_for_same_point()
    {
        var a = new GeoPoint(51.1, 71.4);
        var b = new GeoPoint(51.12, 71.45);
        Assert.Equal(GeoMath.Meters(a, b), GeoMath.Meters(b, a), 6);
        Assert.Equal(0, GeoMath.Meters(a, a));
        Assert.Equal(0, GeoMath.WalkMinutes(a, a));
    }

    [Theory]
    [InlineData(80, 2)]     // 80 m × 1.3 detour = 104 m → 1.3 min → rounds up to 2
    [InlineData(600, 10)]   // 780 m of streets at 80 m/min
    [InlineData(5, 1)]      // tiny distances still take a minute
    public void Walk_minutes_include_detour_and_round_up(double meters, int expected) =>
        Assert.Equal(expected, GeoMath.WalkMinutes(Centre, East(meters)));

    [Fact]
    public void Walk_radius_is_inverse_of_walk_minutes()
    {
        var radius = GeoMath.WalkRadiusMeters(10);
        Assert.Equal(10, GeoMath.WalkMinutes(Centre, East(radius - 1)));
    }

    [Fact]
    public void Offset_moves_by_requested_metres()
    {
        var moved = GeoMath.Offset(Centre, 1000, 1000);
        Assert.InRange(GeoMath.Meters(Centre, moved), 1400, 1430);
    }

    [Theory]
    [InlineData(51.128, 71.430, true)]
    [InlineData(43.238, 76.889, false)] // Almaty
    [InlineData(0, 0, false)]
    public void Astana_bounds_reject_other_cities(double lat, double lon, bool inside) =>
        Assert.Equal(inside, GeoMath.InAstana(new(lat, lon)));
}

public class PlaceCatalogTests
{
    [Theory]
    [InlineData("leisure=fitness_centre", "gym")]
    [InlineData("leisure=sauna", "spa")]
    [InlineData("amenity=fast_food", "restaurant")]
    [InlineData("shop=convenience", "groceries")]
    [InlineData("highway=bus_stop", PlaceCatalog.BusStop)]
    [InlineData("amenity=parking", null)]
    public void Tags_are_classified_into_one_category(string tag, string? expected) =>
        Assert.Equal(expected, PlaceCatalog.Classify(tag));

    [Fact]
    public void Every_tag_belongs_to_exactly_one_category()
    {
        var tags = PlaceCatalog.All.SelectMany(c => c.OsmTags).ToList();
        Assert.Equal(tags.Count, tags.Distinct().Count());
    }

    [Fact]
    public void Normalize_drops_unknown_and_duplicate_keys_and_keeps_catalogue_order()
    {
        var result = PlaceCatalog.Normalize(["cafe", "GYM", "unknown", "cafe"]);
        Assert.Equal(["gym", "cafe"], result.ToArray());
        Assert.Empty(PlaceCatalog.Normalize(null));
    }
}

public class OsmDataTests
{
    private const string OverpassResponse = """
    {"elements":[
      {"type":"node","id":1,"lat":51.1,"lon":71.4,"tags":{"leisure":"fitness_centre","name":"Iron Gym","name:en":"Iron Gym EN"}},
      {"type":"way","id":2,"center":{"lat":51.2,"lon":71.5},"tags":{"amenity":"cafe","name":"Coffee"}},
      {"type":"node","id":3,"lat":51.3,"lon":71.6,"tags":{"amenity":"parking"}},
      {"type":"way","id":4,"tags":{"amenity":"cafe"}},
      {"type":"node","id":5,"lat":51.4,"lon":71.7,"tags":{"highway":"bus_stop"}}
    ]}
    """;

    [Fact]
    public void Overpass_parser_reads_nodes_and_way_centres_and_skips_irrelevant()
    {
        var places = OsmData.ParseOverpass(OverpassResponse);
        Assert.Equal(3, places.Length);
        Assert.Equal(new Place("gym", "Iron Gym EN", new(51.1, 71.4)), places[0]);
        Assert.Equal(new GeoPoint(51.2, 71.5), places[1].Location);
        Assert.Equal(PlaceCatalog.BusStop, places[2].Category);
        Assert.Equal("Unnamed bus stop", places[2].DisplayName);
    }

    [Fact]
    public void Unexpected_json_shape_returns_no_places()
    {
        Assert.Empty(OsmData.ParseOverpass("""{"remark":"runtime error"}"""));
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => OsmData.ParseOverpass("<html>busy</html>"));
    }

    [Fact]
    public void Snapshot_format_round_trips()
    {
        var places = OsmData.ParseOverpass(OverpassResponse);
        var restored = OsmData.ParseSnapshot(OsmData.ToSnapshotJson(places));
        Assert.Equal(places.Select(p => (p.Category, p.Name)), restored.Select(p => (p.Category, p.Name)));
        // Coordinates are stored with 6 decimals (≈ 0.1 m), so compare within a few centimetres.
        Assert.All(places.Zip(restored), pair => Assert.True(GeoMath.Meters(pair.First.Location, pair.Second.Location) < 0.1));
    }

    [Fact]
    public void Query_asks_for_every_catalogue_tag_and_bus_stops()
    {
        var query = OsmData.OverpassQuery(new(51.1, 71.4), 1500);
        Assert.Contains("around:1500,51.100000,71.400000", query);
        Assert.Contains("highway=bus_stop", query);
        foreach (var tag in PlaceCatalog.All.SelectMany(c => c.OsmTags))
            Assert.Contains(tag.Split('=')[1], query);
    }

    [Fact]
    public void Bundled_snapshot_covers_astana()
    {
        var index = RealPlaces.Value;
        Assert.True(index.Count > 4000);
        // Large areas (e.g. a park) are stored at their centre, which can sit just outside the box.
        Assert.True(index.Places.Count(p => !GeoMath.InAstana(p.Location)) <= 5);
        Assert.True(index.Places.Count(p => p.Category == PlaceCatalog.BusStop) > 1000);
    }
}

public class PlaceIndexTests
{
    private static readonly PlaceIndex Index = PlaceIndex.Create(
    [
        At("cafe", 100, "Near cafe"), At("cafe", 900, "Far cafe"), At("gym", 300), At("cafe", 5000, "Other district"),
    ]);

    [Fact]
    public void Near_returns_only_places_in_radius_nearest_first()
    {
        var found = Index.Near(Centre, 1000, "cafe").Select(x => x.Place.Name).ToList();
        Assert.Equal(["Near cafe", "Far cafe"], found);
    }

    [Fact]
    public void Near_matches_brute_force_on_real_data()
    {
        var index = RealPlaces.Value;
        var point = new GeoPoint(51.0909, 71.4184);
        var fast = index.Near(point, 1200).Select(x => x.Place).ToHashSet();
        var slow = index.Places.Where(p => GeoMath.Meters(point, p.Location) <= 1200).ToHashSet();
        Assert.True(slow.SetEquals(fast));
    }

    [Fact]
    public void Nearest_is_null_when_nothing_in_radius()
    {
        Assert.Null(Index.Nearest(Centre, "pharmacy"));
        Assert.Equal("Near cafe", Index.Nearest(Centre, "cafe")!.Value.Place.Name);
    }

    [Fact]
    public void Replace_area_swaps_inside_circle_and_keeps_original_unchanged()
    {
        var fresh = Index.ReplaceArea(Centre, 1000, [At("pharmacy", 50, "New pharmacy")]);
        Assert.Equal(["New pharmacy", "Other district"], fresh.Places.Select(p => p.Name).Order().ToArray());
        Assert.Equal(4, Index.Count); // immutable: the old index is untouched
    }
}

public class AreaAnalysisTests
{
    private static readonly PlaceIndex Index = PlaceIndex.Create(
        [At("cafe", 200), At("gym", 1100), At(PlaceCatalog.BusStop, 150, "Corner stop"), At(PlaceCatalog.BusStop, 300)]);

    [Fact]
    public void Needs_report_nearest_place_and_walk_time()
    {
        var report = AreaAnalysis.Analyse(Centre, Index, ["cafe", "gym"], [], walkLimit: 10);
        var cafe = report.Needs.Single(n => n.Category.Key == "cafe");
        Assert.True(cafe.Satisfied(10));
        Assert.Equal(4, cafe.WalkMinutes);
        var gym = report.Needs.Single(n => n.Category.Key == "gym");
        Assert.False(gym.Satisfied(10));   // 1100 m ≈ 18 minutes
        Assert.Equal(0.5, report.Coverage);
        Assert.Equal("Corner stop", report.NearestStop!.Name);
        Assert.Equal(2, report.BusStopsWithinLimit);
    }

    [Fact]
    public void Missing_must_have_is_reported_and_never_invented()
    {
        var report = AreaAnalysis.Analyse(Centre, Index, ["cafe"], ["pharmacy"], walkLimit: 10);
        var missing = Assert.Single(report.MissingRequired);
        Assert.Equal("pharmacy", missing.Category.Key);
        Assert.Null(missing.Nearest);
        Assert.Null(missing.WalkMinutes);
    }

    [Fact]
    public void Lifestyle_score_decays_after_the_walk_limit()
    {
        var close = AreaAnalysis.Analyse(Centre, Index, ["cafe"], [], 10).LifestyleScore;
        var gym = AreaAnalysis.Analyse(Centre, Index, ["gym"], [], 10).LifestyleScore;  // 18 min vs limit 10
        var none = AreaAnalysis.Analyse(Centre, Index, ["pharmacy"], [], 10).LifestyleScore;
        Assert.Equal(1, close);
        Assert.InRange(gym, 0.1, 0.3);
        Assert.Equal(0, none);
    }

    [Fact]
    public void No_selected_needs_scores_zero_instead_of_perfect() =>
        Assert.Equal(0, AreaAnalysis.Analyse(Centre, Index, [], [], 10).LifestyleScore);
}
