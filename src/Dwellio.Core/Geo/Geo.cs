namespace Dwellio.Core.Geo;

/// <summary>A WGS-84 coordinate. A value type: two points with the same numbers are equal.</summary>
public readonly record struct GeoPoint(double Lat, double Lon)
{
    public override string ToString() => FormattableString.Invariant($"{Lat:F5},{Lon:F5}");
}

/// <summary>A point with a human-readable name, e.g. a university picked in the planner.</summary>
public sealed record NamedPoint(string Name, GeoPoint Location, string? Detail = null);

/// <summary>Pure geometry helpers. No I/O, no clock: every result depends only on the arguments.</summary>
public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_000;

    /// <summary>Average adult walking speed (≈4.8 km/h).</summary>
    public const double WalkMetersPerMinute = 80;

    /// <summary>Streets are not straight lines; 1.3 is a common urban detour factor.</summary>
    public const double DetourFactor = 1.3;

    /// <summary>Astana bounding box used to validate user input and geocoder results.</summary>
    public static readonly (GeoPoint SouthWest, GeoPoint NorthEast) Astana = (new(50.98, 71.25), new(51.28, 71.66));

    public static readonly GeoPoint AstanaCentre = new(51.1282, 71.4304);

    /// <summary>Great-circle distance using the haversine formula.</summary>
    public static double Meters(GeoPoint a, GeoPoint b)
    {
        static double Rad(double degrees) => degrees * Math.PI / 180;
        var dLat = Rad(b.Lat - a.Lat);
        var dLon = Rad(b.Lon - a.Lon);
        var h = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(Rad(a.Lat)) * Math.Cos(Rad(b.Lat)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>Estimated walking minutes, rounded up, never below one minute for distinct points.</summary>
    public static int WalkMinutes(GeoPoint a, GeoPoint b)
    {
        var meters = Meters(a, b);
        return meters < 1 ? 0 : Math.Max(1, (int)Math.Ceiling(meters * DetourFactor / WalkMetersPerMinute));
    }

    /// <summary>Straight-line radius reachable on foot within the given minutes (inverse of <see cref="WalkMinutes"/>).</summary>
    public static double WalkRadiusMeters(int minutes) => minutes * WalkMetersPerMinute / DetourFactor;

    public static bool InAstana(GeoPoint p) =>
        p.Lat >= Astana.SouthWest.Lat && p.Lat <= Astana.NorthEast.Lat &&
        p.Lon >= Astana.SouthWest.Lon && p.Lon <= Astana.NorthEast.Lon;

    /// <summary>Moves a point by metres north and east. Used to build search grids.</summary>
    public static GeoPoint Offset(GeoPoint p, double northMeters, double eastMeters) => new(
        p.Lat + northMeters / 111_320,
        p.Lon + eastMeters / (111_320 * Math.Cos(p.Lat * Math.PI / 180)));
}
