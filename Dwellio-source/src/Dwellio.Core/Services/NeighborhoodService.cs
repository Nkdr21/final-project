using Dwellio.Core.Models;

namespace Dwellio.Core.Services;

public static class NeighborhoodService
{
    // Every selected amenity must be available within the chosen walking limit.
    public static IEnumerable<Complex> Filter(IEnumerable<Complex> complexes, string search,
        IReadOnlyCollection<string> categories, int walkingLimit) => complexes
        .Where(c => c.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
        .Where(c => categories.All(category => c.Amenities.Any(a =>
            a.Category == category && a.WalkMinutes <= walkingLimit)))
        .OrderBy(c => c.Name);

    public static IEnumerable<Amenity> Inside(Complex complex) =>
        complex.Amenities.Where(a => a.WalkMinutes == 0).OrderBy(a => a.Category);

    public static IEnumerable<Amenity> Nearby(Complex complex) =>
        complex.Amenities.Where(a => a.WalkMinutes > 0).OrderBy(a => a.WalkMinutes);

    public static string CostLabel(Amenity amenity) => amenity.Included
        ? "Included / no separate fee in this scenario"
        : "Separate price or access conditions — see details";
}
