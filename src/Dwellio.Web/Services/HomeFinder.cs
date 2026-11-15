using System.Collections.Immutable;
using Dwellio.Core.Geo;
using Dwellio.Core.Matching;
using Dwellio.Core.Models;
using Dwellio.Core.Places;
using Dwellio.Web.Data;
using Dwellio.Web.Services.OpenData;
using Microsoft.Extensions.Caching.Memory;

namespace Dwellio.Web.Services;

/// <summary>A listing together with its complex and its evaluation for the current preferences.</summary>
public sealed record HomeResult(ListingEntity Listing, ComplexEntity Complex, HomeMatch Match);

/// <summary>
/// Glue between storage, open data and the pure Core functions. All decisions are made in Core;
/// this class only gathers inputs, runs heavy work off the UI thread and caches results.
/// </summary>
public sealed class HomeFinder(HousingService housing, PlaceService places, TransitService transit, IMemoryCache cache)
{
    public async Task<ImmutableArray<HomeResult>> RankAsync(Preferences p, CancellationToken ct = default)
    {
        var listings = await housing.Listings();
        var complexes = (await housing.Complexes()).ToDictionary(c => c.Id);
        var results = await EvaluateAsync(listings.Where(l => complexes.ContainsKey(l.ComplexId)).Select(l => (l, complexes[l.ComplexId])), p, ct);
        var order = HomeMatcher.Rank(results.Select(r => r.Match)).Select((m, i) => (m.Apartment.Id, i)).ToDictionary(x => x.Id, x => x.i);
        return results.OrderBy(r => order[r.Listing.Id]).ToImmutableArray();
    }

    public async Task<HomeResult?> ForListingAsync(int id, Preferences p, CancellationToken ct = default)
    {
        var listing = await housing.Listing(id);
        if (listing is null) return null;
        var complex = (await housing.Complexes()).FirstOrDefault(c => c.Id == listing.ComplexId);
        if (complex is null) return null;
        return (await EvaluateAsync([(listing, complex)], p, ct))[0];
    }

    private async Task<ImmutableArray<HomeResult>> EvaluateAsync(IEnumerable<(ListingEntity Listing, ComplexEntity Complex)> homes, Preferences p, CancellationToken ct)
    {
        var index = await places.GetIndexAsync(ct);
        var network = transit.Network;
        // Commute and area depend only on the complex, so compute them once per building.
        var byComplex = homes.GroupBy(h => h.Complex.Id).ToList();
        var perComplex = await Task.Run(() => byComplex.ToDictionary(g => g.Key, g =>
        {
            var location = g.First().Complex.ToModel().Location;
            var key = ("home", g.Key, p.CacheKey, places.Version);
            return cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                return (Area: AreaAnalysis.Analyse(location, index, p.AllNeeds, p.MustHaves, p.WalkLimit),
                        Commute: Core.Transit.JourneyPlanner.Plan(p.ToJourney(location), network, index));
            });
        }), ct);
        return byComplex.SelectMany(g => g).Select(h =>
        {
            var (area, commute) = perComplex[h.Complex.Id];
            return new HomeResult(h.Listing, h.Complex, HomeMatcher.Evaluate(h.Listing.ToModel(), h.Complex.ToModel(), area, commute, p));
        }).ToImmutableArray();
    }

    /// <summary>Best neighbourhoods for the preferences (cached for all users with the same inputs).</summary>
    public async Task<ImmutableArray<AreaCandidate>> AreasAsync(Preferences p, CancellationToken ct = default)
    {
        var index = await places.GetIndexAsync(ct);
        var key = ("areas", p.CacheKey, places.Version);
        if (cache.TryGetValue(key, out ImmutableArray<AreaCandidate> cached)) return cached;
        var areas = await Task.Run(() => AreaSearch.Best(p, index, transit.Network), ct);
        cache.Set(key, areas, TimeSpan.FromMinutes(20));
        return areas;
    }

    public async Task<AreaReport> AnalyseAsync(GeoPoint at, Preferences p, CancellationToken ct = default) =>
        AreaAnalysis.Analyse(at, await places.GetIndexAsync(ct), p.AllNeeds, p.MustHaves, p.WalkLimit);

    /// <summary>Places of the selected categories around a point, for drawing on the map.</summary>
    public async Task<ImmutableArray<Place>> PlacesAroundAsync(GeoPoint at, IEnumerable<string> categories, int walkLimit, CancellationToken ct = default)
    {
        var index = await places.GetIndexAsync(ct);
        var wanted = categories.ToHashSet();
        return index.Near(at, GeoMath.WalkRadiusMeters(walkLimit))
            .Where(x => wanted.Contains(x.Place.Category))
            .Select(x => x.Place)
            .Take(250)
            .ToImmutableArray();
    }
}
