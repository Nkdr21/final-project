using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Dwellio.Core.Geo;
using Dwellio.Core.Places;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Dwellio.Web.Services.OpenData;

public enum SuggestionKind { Popular, Stop, Address }

public sealed record Suggestion(string Name, string Detail, GeoPoint Location, SuggestionKind Kind)
{
    public NamedPoint ToPoint() => new(Name, Location, Detail);
}

/// <summary>
/// Turns text into coordinates. Local results (popular destinations and named bus stops) are
/// instant; Nominatim (with Photon as fallback) adds street addresses. Both are free and key-less.
/// Nominatim's policy allows at most one request per second, enforced by a shared gate.
/// </summary>
public sealed class GeocodingService(HttpClient http, PlaceService places, IMemoryCache cache,
    IOptions<OpenDataOptions> options, ILogger<GeocodingService> log)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTime lastRequestUtc = DateTime.MinValue;

    /// <summary>Common destinations for students and workers. Coordinates from OpenStreetMap Nominatim.</summary>
    public static readonly ImmutableArray<Suggestion> Popular =
    [
        new("Astana IT University", "Mangilik El avenue · EXPO, block C1", new(51.09086, 71.41841), SuggestionKind.Popular),
        new("Nazarbayev University", "Kabanbay Batyr 53", new(51.09207, 71.39765), SuggestionKind.Popular),
        new("L.N. Gumilyov Eurasian National University", "Kazhymukan Street · Almaty district", new(51.15579, 71.47210), SuggestionKind.Popular),
        new("Astana Hub", "EXPO · tech park", new(51.08835, 71.41459), SuggestionKind.Popular),
        new("Mega Silk Way", "Kabanbay Batyr 62", new(51.08909, 71.40729), SuggestionKind.Popular),
        new("Khan Shatyr", "Turan Avenue 37", new(51.13250, 71.40386), SuggestionKind.Popular),
        new("Baiterek", "Government quarter", new(51.12829, 71.43046), SuggestionKind.Popular),
        new("Abu Dhabi Plaza", "Syganak Street 60/5", new(51.12205, 71.42796), SuggestionKind.Popular),
        new("Astana-1 railway station", "Saryarka district", new(51.19590, 71.40999), SuggestionKind.Popular),
        new("Nursultan Nazarbayev International Airport", "Airport", new(51.02585, 71.46420), SuggestionKind.Popular),
    ];

    /// <summary>Instant matches: popular destinations and named bus stops from the place index.</summary>
    public async Task<ImmutableArray<Suggestion>> LocalAsync(string query, CancellationToken ct = default)
    {
        var text = query.Trim();
        if (text.Length == 0) return Popular;
        bool Match(string value) => value.Contains(text, StringComparison.CurrentCultureIgnoreCase);
        var index = await places.GetIndexAsync(ct);
        var stops = index.Places
            .Where(p => p.Category == PlaceCatalog.BusStop && !string.IsNullOrWhiteSpace(p.Name) && Match(p.Name))
            .DistinctBy(p => p.Name)
            .Take(4)
            .Select(p => new Suggestion(p.Name, "Bus stop", p.Location, SuggestionKind.Stop));
        return Popular.Where(s => Match(s.Name) || Match(s.Detail)).Concat(stops).Take(8).ToImmutableArray();
    }

    /// <summary>Address search limited to Astana. Never throws: returns an empty list when offline.</summary>
    public async Task<ImmutableArray<Suggestion>> RemoteAsync(string query, CancellationToken ct = default)
    {
        var text = query.Trim();
        if (text.Length < 3 || !options.Value.LiveRequests) return [];
        var key = "geo:" + text.ToLowerInvariant();
        if (cache.TryGetValue(key, out ImmutableArray<Suggestion> cached)) return cached;

        var results = await NominatimAsync(text, ct) ?? await PhotonAsync(text, ct) ?? [];
        cache.Set(key, results, TimeSpan.FromHours(6));
        return results;
    }

    private async Task<ImmutableArray<Suggestion>?> NominatimAsync(string text, CancellationToken ct)
    {
        var (sw, ne) = GeoMath.Astana;
        var url = FormattableString.Invariant(
            $"{options.Value.NominatimUrl}?q={Uri.EscapeDataString(text)}&format=jsonv2&limit=6&countrycodes=kz&bounded=1&viewbox={sw.Lon},{ne.Lat},{ne.Lon},{sw.Lat}&accept-language=en");
        await Gate.WaitAsync(ct);
        try
        {
            var wait = lastRequestUtc.AddSeconds(1.1) - DateTime.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
            lastRequestUtc = DateTime.UtcNow;
            using var doc = await GetJsonAsync(url, ct);
            if (doc is null) return null;
            return doc.RootElement.EnumerateArray().Select(e =>
            {
                var display = e.GetProperty("display_name").GetString() ?? "";
                var parts = display.Split(", ");
                var name = e.TryGetProperty("name", out var n) && !string.IsNullOrWhiteSpace(n.GetString()) ? n.GetString()! : parts[0];
                return new Suggestion(name, string.Join(", ", parts.Skip(1).Take(3)),
                    new(Parse(e.GetProperty("lat")), Parse(e.GetProperty("lon"))), SuggestionKind.Address);
            }).Where(s => GeoMath.InAstana(s.Location)).ToImmutableArray();
        }
        finally { Gate.Release(); }
    }

    private async Task<ImmutableArray<Suggestion>?> PhotonAsync(string text, CancellationToken ct)
    {
        var c = GeoMath.AstanaCentre;
        var url = FormattableString.Invariant($"{options.Value.PhotonUrl}?q={Uri.EscapeDataString(text)}&lat={c.Lat}&lon={c.Lon}&limit=6");
        using var doc = await GetJsonAsync(url, ct);
        if (doc is null) return null;
        return doc.RootElement.GetProperty("features").EnumerateArray().Select(f =>
        {
            var coords = f.GetProperty("geometry").GetProperty("coordinates");
            var props = f.GetProperty("properties");
            string Prop(string name) => props.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";
            var street = string.Join(" ", new[] { Prop("street"), Prop("housenumber") }.Where(x => x.Length > 0));
            return new Suggestion(Prop("name") is { Length: > 0 } name ? name : street,
                string.Join(", ", new[] { street, Prop("district") }.Where(x => x.Length > 0)),
                new(coords[1].GetDouble(), coords[0].GetDouble()), SuggestionKind.Address);
        }).Where(s => s.Name.Length > 0 && GeoMath.InAstana(s.Location)).ToImmutableArray();
    }

    private async Task<JsonDocument?> GetJsonAsync(string url, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.GeocoderTimeoutSeconds));
        try
        {
            using var response = await http.GetAsync(url, timeout.Token);
            if (!response.IsSuccessStatusCode) { log.LogInformation("Geocoder {Url} answered {Status}", url, (int)response.StatusCode); return null; }
            return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
        }
        catch (Exception e) when (!ct.IsCancellationRequested && e is HttpRequestException or TaskCanceledException or JsonException)
        {
            log.LogInformation("Geocoder request failed: {Message}", e.Message);
            return null;
        }
    }

    private static double Parse(JsonElement e) => e.ValueKind == JsonValueKind.Number
        ? e.GetDouble() : double.Parse(e.GetString()!, CultureInfo.InvariantCulture);
}
