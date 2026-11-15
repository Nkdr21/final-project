namespace Dwellio.Web.Services.OpenData;

/// <summary>
/// Settings for the free, key-less public APIs (section "OpenData" in appsettings.json).
/// All of them are community services: be polite (identify the app, cache, rate-limit).
/// </summary>
public sealed class OpenDataOptions
{
    public const string Section = "OpenData";

    /// <summary>Set to false to run fully offline from the bundled snapshots (used by tests and slow networks).</summary>
    public bool LiveRequests { get; set; } = true;

    public string UserAgent { get; set; } = "Dwellio/2.0 (student project; +https://github.com/Nkdr21/final-project)";

    /// <summary>Overpass API mirrors, tried in order until one answers.</summary>
    public string[] OverpassMirrors { get; set; } =
    [
        "https://overpass-api.de/api/interpreter",
        "https://maps.mail.ru/osm/tools/overpass/api/interpreter",
        "https://overpass.private.coffee/api/interpreter",
    ];

    public int OverpassTimeoutSeconds { get; set; } = 35;
    public int LiveRadiusMeters { get; set; } = 2500;
    public int CacheHours { get; set; } = 72;

    public string NominatimUrl { get; set; } = "https://nominatim.openstreetmap.org/search";
    public string PhotonUrl { get; set; } = "https://photon.komoot.io/api/";
    public int GeocoderTimeoutSeconds { get; set; } = 8;
}
