using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json.Serialization;
using Dwellio.Core.Geo;
using Dwellio.Core.Places;
using Dwellio.Core.Transit;

namespace Dwellio.Core.Models;

// Records describe values. EF entities live separately in the Web project and convert with ToModel().

public sealed record Apartment(int Id, int ComplexId, string Title, decimal Rent, decimal? Utilities,
    decimal? Deposit, int Rooms, int Area, bool Furnished, bool Available);

/// <summary>A real residential complex with real coordinates.</summary>
public sealed record Complex(int Id, string Name, string Address, GeoPoint Location, string Description);

/// <summary>
/// Everything the user tells the planner. Immutable: changes produce a new value with `with`,
/// which makes undo, comparison and caching trivial.
/// </summary>
public sealed record Preferences
{
    public string DestinationName { get; init; } = "Astana IT University";
    public double DestinationLat { get; init; } = 51.09086;
    public double DestinationLon { get; init; } = 71.41841;
    [JsonIgnore] public GeoPoint Destination => new(DestinationLat, DestinationLon);

    public decimal Budget { get; init; } = 350_000;
    public int MinRooms { get; init; } = 1;
    public bool FurnishedOnly { get; init; }

    public int ArriveByHour { get; init; } = 9;
    public int MaxCommuteMinutes { get; init; } = 45;
    public int WalkLimit { get; init; } = 10;
    public CommutePriority Priority { get; init; } = CommutePriority.Balanced;

    public ImmutableArray<string> Needs { get; init; } = ["groceries", "gym", "cafe", "pharmacy"];
    public ImmutableArray<string> MustHaves { get; init; } = [];

    public int PriceWeight { get; init; } = 3;
    public int LifestyleWeight { get; init; } = 4;
    public int CommuteWeight { get; init; } = 3;

    public decimal TransportCost { get; init; } = 4_400;
    public decimal GymCost { get; init; } = 15_000;

    /// <summary>All needs including must-haves, in catalogue order.</summary>
    [JsonIgnore] public ImmutableArray<string> AllNeeds => PlaceCatalog.Normalize(Needs.NullToEmpty().Concat(MustHaves.NullToEmpty()));

    /// <summary>Stable text key for caching (ImmutableArray equality is by reference).</summary>
    [JsonIgnore] public string CacheKey => string.Join('|', DestinationLat, DestinationLon, Budget, MinRooms, FurnishedOnly,
        ArriveByHour, MaxCommuteMinutes, WalkLimit, Priority, string.Join(',', AllNeeds), string.Join(',', MustHaves.NullToEmpty()),
        PriceWeight, LifestyleWeight, CommuteWeight, TransportCost, GymCost);

    public JourneyRequest ToJourney(GeoPoint from) => new(from, Destination, ArriveByHour, Math.Max(WalkLimit, 10), Priority);
}

public sealed record ScoreParts(double Price, double Lifestyle, double Commute);

/// <summary>
/// A home evaluated against preferences. <see cref="Exclusions"/> lists hard requirements that
/// failed; such homes are still returned so the UI can explain "why not" instead of hiding them.
/// </summary>
public sealed record HomeMatch(
    Apartment Apartment,
    Complex Complex,
    AreaReport Area,
    CommutePlan Commute,
    int Score,
    ScoreParts Parts,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> Gaps,
    ImmutableArray<string> Exclusions)
{
    public bool Eligible => Exclusions.IsEmpty;
}

public sealed record MonthlyCost(decimal Rent, decimal? Utilities, decimal Transport, decimal Gym, decimal? Deposit)
{
    public bool UtilitiesUnknown => Utilities is null;
    public decimal KnownTotal => Rent + (Utilities ?? 0) + Transport + Gym;
    public decimal MoveInTotal => KnownTotal + (Deposit ?? 0);
}

public sealed record OperationResult(bool Success, string Message)
{
    public static OperationResult Ok(string message = "Saved") => new(true, message);
    public static OperationResult Fail(string message) => new(false, message);
}

public static class Labels
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public static string Money(decimal value) => value.ToString("N0", Russian) + " ₸";
    public static string Percent(double value) => $"{Math.Round(value * 100):0}%";
    public static string Minutes(int? value) => value is null ? "—" : value < 60 ? $"{value} min" : $"{value / 60} h {value % 60} min";
    public static string Hour(int hour) => $"{hour:00}:00";

    public static string SeatWord(double chance) => chance switch
    {
        >= 0.75 => "Likely seat",
        >= 0.4 => "Maybe a seat",
        > 0.05 => "Seat unlikely",
        _ => "Expect to stand",
    };

    public static string Priority(CommutePriority p) => p switch
    {
        CommutePriority.Seat => "Get a seat",
        CommutePriority.Fastest => "Fastest",
        _ => "Balanced",
    };
}

public static class ImmutableArrayExtensions
{
    /// <summary>Deserialised JSON may leave an ImmutableArray uninitialised; treat that as empty.</summary>
    public static ImmutableArray<T> NullToEmpty<T>(this ImmutableArray<T> array) => array.IsDefault ? [] : array;
}
