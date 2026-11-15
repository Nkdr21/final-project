using System.Globalization;
using Dwellio.Core.Geo;

namespace Dwellio.Web.Services;

/// <summary>Safe parsing of values that arrive in URLs (users can edit them).</summary>
public static class Query
{
    public static GeoPoint? Point(string? lat, string? lon) =>
        double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) &&
        double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var b) &&
        GeoMath.InAstana(new(a, b))
            ? new GeoPoint(a, b)
            : null;
}
