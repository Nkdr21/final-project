using System.Collections.Immutable;
using Dwellio.Core.Geo;
using Dwellio.Core.Places;
using Dwellio.Core.Transit;

namespace Dwellio.Tests;

/// <summary>Small, hand-made inputs so every test states exactly what it depends on.</summary>
public static class TestData
{
    public static readonly GeoPoint Centre = new(51.10, 71.42);

    /// <summary>A point <paramref name="meters"/> east of <see cref="Centre"/>.</summary>
    public static GeoPoint East(double meters) => GeoMath.Offset(Centre, 0, meters);

    public static Place At(string category, double metersEast, string name = "Test place") => new(category, name, East(metersEast));

    /// <summary>
    /// A straight bus line heading east, one stop every 400 m, two minutes apart.
    /// Every stop has the same dwell time so the load profile is easy to reason about.
    /// </summary>
    public static TransitRoute Line(int stops = 12, double dwellSeconds = 30, string reference = "99", int startMetersEast = -2000)
    {
        var hours = Enumerable.Range(5, 19).ToList();
        return new TransitRoute(reference, 1, "Test line", Enumerable.Range(0, stops).Select(i => new TransitStop(
            $"s{i}", $"Stop {i}", East(startMetersEast + i * 400),
            hours.ToImmutableSortedDictionary(h => h, _ => dwellSeconds),
            hours.ToImmutableSortedDictionary(h => h, _ => i * 120.0))).ToImmutableArray());
    }

    public static TransitNetwork Network(params TransitRoute[] routes) => new("test", "test", [.. routes]);

    public static string RepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dwellio.slnx"))) dir = dir.Parent;
        return Path.Combine([dir!.FullName, .. parts]);
    }

    public static readonly Lazy<TransitNetwork> RealNetwork = new(() =>
        TransitNetwork.Parse(File.ReadAllText(RepoFile("src", "Dwellio.Web", "Data", "Transit", "astana-gtfs.json"))));

    public static readonly Lazy<PlaceIndex> RealPlaces = new(() =>
        PlaceIndex.Create(OsmData.ParseSnapshot(File.ReadAllText(RepoFile("src", "Dwellio.Web", "Data", "Places", "astana-osm.json")))));
}
