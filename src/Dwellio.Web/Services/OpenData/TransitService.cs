using System.Collections.Immutable;
using Dwellio.Core.Transit;

namespace Dwellio.Web.Services.OpenData;

/// <summary>
/// Loads the bundled Astana GTFS summary once (singleton) and plans commutes with it.
/// Source: Mansurova et al., Zenodo DOI 10.5281/zenodo.15769359, CC BY 4.0.
/// </summary>
public sealed class TransitService
{
    private readonly PlaceService places;
    private readonly Lazy<TransitNetwork> network;

    public TransitService(IWebHostEnvironment environment, PlaceService places, ILogger<TransitService> log)
    {
        this.places = places;
        network = new(() =>
        {
            try
            {
                var loaded = TransitNetwork.Parse(File.ReadAllText(PlaceService.DataFile(environment, "Transit", "astana-gtfs.json")));
                log.LogInformation("Loaded {Count} bus route directions", loaded.Routes.Length);
                return loaded;
            }
            catch (Exception e) when (e is IOException or System.Text.Json.JsonException or KeyNotFoundException)
            {
                // Without the file the planner still works using estimates between OpenStreetMap stops.
                log.LogError(e, "Transit data unavailable; commute plans will be estimates only");
                return TransitNetwork.Empty;
            }
        });
    }

    public TransitNetwork Network => network.Value;

    public ImmutableArray<string> RouteRefs => Network.Routes.Select(r => r.Ref).Distinct().Order().ToImmutableArray();

    public async Task<CommutePlan> PlanAsync(JourneyRequest request, CancellationToken ct = default) =>
        JourneyPlanner.Plan(request, Network, await places.GetIndexAsync(ct));

    public ImmutableArray<StopLoad> Profile(TransitRoute route, int hour) => SeatModel.Profile(route, hour);
}
