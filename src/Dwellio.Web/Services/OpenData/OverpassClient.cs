using System.Collections.Immutable;
using System.Text.Json;
using Dwellio.Core.Geo;
using Dwellio.Core.Places;
using Microsoft.Extensions.Options;

namespace Dwellio.Web.Services.OpenData;

/// <summary>
/// Fetches live places from the OpenStreetMap Overpass API (free, no key).
/// Public Overpass servers are often busy (HTTP 429/502/504), so each mirror is tried in turn
/// with its own timeout. Returns null instead of throwing: callers fall back to cached data.
/// </summary>
public sealed class OverpassClient(IHttpClientFactory httpFactory, IOptions<OpenDataOptions> options, ILogger<OverpassClient> log)
{
    public const string HttpClientName = "overpass";

    public async Task<ImmutableArray<Place>?> FetchAsync(GeoPoint centre, int radiusMeters, CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.LiveRequests) return null;
        var query = OsmData.OverpassQuery(centre, radiusMeters, settings.OverpassTimeoutSeconds);
        var http = httpFactory.CreateClient(HttpClientName);

        foreach (var mirror in settings.OverpassMirrors)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.OverpassTimeoutSeconds + 5));
            try
            {
                using var content = new FormUrlEncodedContent([new("data", query)]);
                using var response = await http.PostAsync(mirror, content, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    log.LogInformation("Overpass mirror {Mirror} answered {Status}", mirror, (int)response.StatusCode);
                    continue;
                }
                var places = OsmData.ParseOverpass(await response.Content.ReadAsStringAsync(timeout.Token));
                log.LogInformation("Overpass mirror {Mirror} returned {Count} places around {Centre}", mirror, places.Length, centre);
                return places;
            }
            catch (Exception e) when (!ct.IsCancellationRequested && e is HttpRequestException or TaskCanceledException or JsonException)
            {
                log.LogInformation("Overpass mirror {Mirror} failed: {Message}", mirror, e.Message);
            }
        }
        return null;
    }
}
