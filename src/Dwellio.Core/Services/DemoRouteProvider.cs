using System.Collections.Immutable;
using Dwellio.Core.Models;
namespace Dwellio.Core.Services;

public interface IRouteProvider
{
    Task<RouteResult> GetAsync(Complex complex, Preferences preferences, CancellationToken cancellationToken = default);
}
// Deliberately synthetic, deterministic transport scenarios; never a live bus prediction.
public class DemoRouteProvider : IRouteProvider
{
    public Task<RouteResult> GetAsync(Complex complex, Preferences p, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (p.Destination == "unavailable") return Task.FromResult<RouteResult>(new RoutesUnavailable("Route provider unavailable. Ranking without transport data."));
        int destination = p.Destination switch { "ENU" => 7, "NU" => 2, "AITU" => 4, _ => -1 };
        if (destination < 0) return Task.FromResult<RouteResult>(new RoutesMissing("No transport data for this destination yet."));
        int distance = 15 + Math.Abs(complex.Id - destination) * 4;
        bool morningPeak = p.ArrivalHour is >= 8 and <= 10;
        bool eveningPeak = p.ReturnHour is >= 17 and <= 19;
        ImmutableArray<Trip> Direction(bool back, bool peak) => [
            new("Direct · scenario", distance + (peak ? 12 : 0) + (back ? 5 : 0), 7, 0,
                peak ? Crowding.High : Crowding.Medium, back ? "Campus stop (demo)" : "Home stop (demo)",
                back ? "Home stop (demo)" : "Campus stop (demo)", back ? "Homebound" : "To campus"),
            new("Alternative · scenario", distance + 13 + (back ? 3 : 0), 12, 1,
                Crowding.Low, "Nearby stop (demo)", back ? "Home stop (demo)" : "Campus stop (demo)", back ? "Homebound" : "To campus")];
        return Task.FromResult<RouteResult>(new RoutesFound(new(Direction(false, morningPeak), Direction(true, eveningPeak))));
    }
}
