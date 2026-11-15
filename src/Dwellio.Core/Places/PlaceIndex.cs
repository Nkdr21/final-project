using System.Collections.Immutable;
using Dwellio.Core.Geo;

namespace Dwellio.Core.Places;

/// <summary>
/// Immutable spatial index. Places are bucketed into ~550 m grid cells so "what is near this
/// point?" only inspects a few cells instead of every place in the city. Updating returns a new
/// index; readers holding the old one are never affected (safe to share between Blazor circuits).
/// </summary>
public sealed class PlaceIndex
{
    private const double CellDegreesLat = 0.005;
    private const double CellDegreesLon = 0.008;
    private readonly ILookup<(int, int), Place> cells;

    public ImmutableArray<Place> Places { get; }
    public int Count => Places.Length;

    public static PlaceIndex Empty { get; } = new([]);

    private PlaceIndex(ImmutableArray<Place> places)
    {
        Places = places;
        cells = places.ToLookup(p => Cell(p.Location));
    }

    public static PlaceIndex Create(IEnumerable<Place> places) => new(places.Distinct().ToImmutableArray());

    private static (int, int) Cell(GeoPoint p) =>
        ((int)Math.Floor(p.Lat / CellDegreesLat), (int)Math.Floor(p.Lon / CellDegreesLon));

    /// <summary>Places within <paramref name="meters"/> of a point, nearest first.</summary>
    public IEnumerable<(Place Place, double Meters)> Near(GeoPoint point, double meters, string? category = null)
    {
        var (row, col) = Cell(point);
        var rows = (int)Math.Ceiling(meters / 111_320 / CellDegreesLat);
        var cols = (int)Math.Ceiling(meters / (111_320 * Math.Cos(point.Lat * Math.PI / 180)) / CellDegreesLon);
        return (from r in Enumerable.Range(row - rows, 2 * rows + 1)
                from c in Enumerable.Range(col - cols, 2 * cols + 1)
                from place in cells[(r, c)]
                where category is null || place.Category == category
                let distance = GeoMath.Meters(point, place.Location)
                where distance <= meters
                orderby distance
                select (place, distance));
    }

    /// <summary>The nearest place of a category within a search radius, if any.</summary>
    public (Place Place, double Meters)? Nearest(GeoPoint point, string category, double maxMeters = 3000) =>
        Near(point, maxMeters, category).Select(x => ((Place, double)?)x).FirstOrDefault();

    /// <summary>
    /// Replaces every place inside a circle with fresher data (e.g. a live Overpass answer),
    /// keeping everything outside the circle. Returns a new index.
    /// </summary>
    public PlaceIndex ReplaceArea(GeoPoint centre, double radiusMeters, IEnumerable<Place> fresh) => Create(
        Places.Where(p => GeoMath.Meters(centre, p.Location) > radiusMeters).Concat(fresh));
}
