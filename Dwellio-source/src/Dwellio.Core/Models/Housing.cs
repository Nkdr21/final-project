using System.Collections.Immutable;

namespace Dwellio.Core.Models;

// Records describe values. EF entities live separately in the Web project.
public record Apartment(int Id, int ComplexId, string Title, decimal Rent, decimal? Utilities,
    decimal Deposit, int Rooms, int Area, bool Furnished, bool Available);
public record Amenity(string Category, string Name, int WalkMinutes, bool Included, string Access);
public record Complex(int Id, string Name, string Address, double Latitude, double Longitude,
    ImmutableArray<Amenity> Amenities, string Description);
public record Preferences(decimal Budget = 220000, int MinRooms = 1, bool FurnishedOnly = false,
    string Destination = "AITU", int ArrivalHour = 9, int ReturnHour = 18,
    int MaxTravelMinutes = 60, int MaxTransfers = 2, int WalkLimit = 15,
    int PriceWeight = 3, int LifestyleWeight = 4, int TravelWeight = 3,
    string Priority = "balanced", string Categories = "gym,groceries", string RequiredCategories = "",
    decimal TransportCost = 8000, decimal GymCost = 15000)
{
    public string[] Wanted => Categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray();
    public string[] Required => RequiredCategories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray();
}
public enum Crowding { Unknown, Low, Medium, High }
public record Trip(string Name, int Minutes, int WalkMinutes, int Transfers, Crowding Crowding,
    string Boarding, string Exit, string Direction, bool IsDemo = true);
public record Commute(ImmutableArray<Trip> Outbound, ImmutableArray<Trip> Return);
public abstract record RouteResult;
public sealed record RoutesFound(Commute Value) : RouteResult;
public sealed record RoutesMissing(string Message) : RouteResult;
public sealed record RoutesUnavailable(string Message) : RouteResult;
public record Match(Apartment Apartment, Complex Complex, decimal Score, ImmutableArray<string> Reasons,
    ImmutableArray<string> Missing, Trip? Outbound, Trip? Return);
public record MonthlyCost(decimal KnownTotal, decimal Deposit, bool UtilitiesUnknown, decimal GymCost)
{
    public decimal MoveInTotal => KnownTotal + Deposit;
}
public record OperationResult(bool Success, string Message)
{
    public static OperationResult Ok(string message = "Saved") => new(true, message);
    public static OperationResult Fail(string message) => new(false, message);
}
public static class Labels
{
    public static readonly IReadOnlyDictionary<string, string> Categories = new Dictionary<string, string>
    {
        ["gym"] = "Gym",
        ["groceries"] = "Groceries",
        ["football"] = "Football pitch",
        ["park"] = "Park",
        ["pharmacy"] = "Pharmacy",
        ["coworking"] = "Coworking",
        ["cafe"] = "Cafe"
    };
    public static string Category(string key) => Categories.GetValueOrDefault(key, key);
    public static string Load(Crowding value) => value switch
    {
        Crowding.Low => "Low load",
        Crowding.Medium => "Medium load",
        Crowding.High => "High load",
        _ => "Load unknown"
    };
    public static string Money(decimal value) => value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("ru-RU")) + " ₸";
}
