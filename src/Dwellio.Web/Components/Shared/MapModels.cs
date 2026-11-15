namespace Dwellio.Web.Components.Shared;

// Plain data sent to wwwroot/js/map.js. Blazor serialises it as camelCase JSON.
public sealed record MapMarker(double Lat, double Lon, string Kind, string Label, string? Popup = null, string? Id = null, bool Emphasis = false);
public sealed record MapCircle(double Lat, double Lon, double Radius, string? Label = null, string? Id = null, string? Color = null);
public sealed record MapData(IReadOnlyList<MapMarker> Markers, IReadOnlyList<MapCircle>? Circles = null, IReadOnlyList<double[]>? Line = null, bool Fit = true)
{
    public static MapData Empty { get; } = new([]);
}
