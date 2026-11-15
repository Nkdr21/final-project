using System.Collections.Concurrent;
using System.Collections.Immutable;
using Dwellio.Core.Geo;
using Dwellio.Core.Places;
using Dwellio.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dwellio.Web.Services.OpenData;

/// <summary>Where the places around a point came from, shown to the user next to results.</summary>
public sealed record Freshness(bool Live, DateTime AsOfUtc, string Label);

/// <summary>An area refreshed from the live Overpass API.</summary>
public sealed record LiveArea(GeoPoint Centre, int RadiusMeters, DateTime FetchedUtc);

/// <summary>
/// Owns the city-wide place index (singleton). Three layers, fastest first:
///  1. bundled OpenStreetMap snapshot (instant, offline, may be weeks old);
///  2. live areas saved in SQLite (survive restarts and outages);
///  3. a live Overpass request when an area has no fresh data.
/// The index is immutable and swapped atomically, so readers never see a half-updated state.
/// </summary>
public sealed class PlaceService(
    IWebHostEnvironment environment,
    IDbContextFactory<HousingDbContext> dbFactory,
    OverpassClient overpass,
    IOptions<OpenDataOptions> options,
    ILogger<PlaceService> log)
{
    private readonly SemaphoreSlim initLock = new(1, 1);
    private readonly ConcurrentDictionary<string, Task<bool>> inFlight = new();
    private readonly object swapLock = new();
    private PlaceIndex index = PlaceIndex.Empty;
    private ImmutableList<LiveArea> liveAreas = [];
    private DateTime snapshotDate = DateTime.MinValue;
    private bool initialised;

    /// <summary>Increases whenever the index changes; used as part of cache keys.</summary>
    public int Version { get; private set; }

    /// <summary>Raised after live data replaced an area (on a background thread).</summary>
    public event Action<GeoPoint>? AreaRefreshed;

    public async Task<PlaceIndex> GetIndexAsync(CancellationToken ct = default)
    {
        if (!initialised) await InitialiseAsync(ct);
        return index;
    }

    public Freshness FreshnessAt(GeoPoint point)
    {
        var live = liveAreas.Where(a => GeoMath.Meters(a.Centre, point) <= a.RadiusMeters * 0.8).MaxBy(a => a.FetchedUtc);
        return live is not null
            ? new(true, live.FetchedUtc, "Live OpenStreetMap data")
            : new(false, snapshotDate, "OpenStreetMap snapshot");
    }

    /// <summary>
    /// Makes sure the area around <paramref name="centre"/> has fresh data. Returns true if new
    /// data arrived. Concurrent requests for the same area share one HTTP call.
    /// </summary>
    public Task<bool> RefreshAsync(GeoPoint centre, CancellationToken ct = default)
    {
        var settings = options.Value;
        var fresh = liveAreas.Any(a => GeoMath.Meters(a.Centre, centre) <= a.RadiusMeters * 0.6 &&
                                       DateTime.UtcNow - a.FetchedUtc < TimeSpan.FromHours(settings.CacheHours));
        if (fresh || !settings.LiveRequests) return Task.FromResult(false);
        var key = TileKey(centre);
        // The shared fetch must not be cancelled by whichever page asked first; mirrors have their own timeouts.
        return inFlight.GetOrAdd(key, _ => FetchAndStoreAsync(key, centre, CancellationToken.None)
            .ContinueWith(t => { inFlight.TryRemove(key, out Task<bool>? _); return t.IsCompletedSuccessfully && t.Result; }, TaskScheduler.Default));
    }

    /// <summary>Areas are rounded to ~1 km tiles so nearby requests reuse each other's results.</summary>
    private static string TileKey(GeoPoint p) => FormattableString.Invariant($"{Math.Round(p.Lat, 2):F2},{Math.Round(p.Lon * 2, 2) / 2:F3}");

    private async Task<bool> FetchAndStoreAsync(string key, GeoPoint centre, CancellationToken ct)
    {
        await GetIndexAsync(ct);
        var radius = options.Value.LiveRadiusMeters;
        var places = await overpass.FetchAsync(centre, radius, ct);
        if (places is null) return false;
        var area = new LiveArea(centre, radius, DateTime.UtcNow);
        Apply(area, places.Value);
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var row = await db.PlaceCache.FindAsync([key], ct);
            if (row is null) db.PlaceCache.Add(row = new PlaceCacheEntity { Key = key });
            (row.Latitude, row.Longitude, row.RadiusMeters, row.FetchedUtc) = (centre.Lat, centre.Lon, radius, area.FetchedUtc);
            row.PlacesJson = OsmData.ToSnapshotJson(places.Value);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception e) when (e is DbUpdateException or InvalidOperationException)
        {
            log.LogWarning(e, "Could not persist live places for {Key}; they stay in memory", key);
        }
        AreaRefreshed?.Invoke(centre);
        return true;
    }

    private void Apply(LiveArea area, ImmutableArray<Place> places)
    {
        lock (swapLock)
        {
            index = index.ReplaceArea(area.Centre, area.RadiusMeters, places);
            liveAreas = liveAreas.RemoveAll(a => GeoMath.Meters(a.Centre, area.Centre) < 50).Add(area);
            Version++;
        }
    }

    private async Task InitialiseAsync(CancellationToken ct)
    {
        await initLock.WaitAsync(ct);
        try
        {
            if (initialised) return;
            var path = DataFile(environment, "Places", "astana-osm.json");
            var json = await File.ReadAllTextAsync(path, ct);
            index = PlaceIndex.Create(OsmData.ParseSnapshot(json));
            snapshotDate = File.GetLastWriteTimeUtc(path);
            using (var doc = System.Text.Json.JsonDocument.Parse(json))
                if (doc.RootElement.TryGetProperty("retrieved", out var r) && DateTime.TryParse(r.GetString(), out var d)) snapshotDate = d;
            log.LogInformation("Loaded {Count} places from the bundled snapshot", index.Count);

            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                foreach (var row in await db.PlaceCache.AsNoTracking().OrderBy(r => r.FetchedUtc).ToListAsync(ct))
                    Apply(new(new(row.Latitude, row.Longitude), row.RadiusMeters, row.FetchedUtc), OsmData.ParseSnapshot(row.PlacesJson));
            }
            catch (Exception e) when (e is Microsoft.Data.Sqlite.SqliteException or InvalidOperationException or System.Text.Json.JsonException)
            {
                log.LogWarning(e, "Ignoring unreadable place cache");
            }
            initialised = true;
        }
        finally { initLock.Release(); }
    }

    /// <summary>Bundled data lives under Data/ next to the app (content root in development, output folder when published).</summary>
    public static string DataFile(IWebHostEnvironment environment, params string[] parts)
    {
        var fromContent = Path.Combine([environment.ContentRootPath, "Data", .. parts]);
        return File.Exists(fromContent) ? fromContent : Path.Combine([AppContext.BaseDirectory, "Data", .. parts]);
    }
}
