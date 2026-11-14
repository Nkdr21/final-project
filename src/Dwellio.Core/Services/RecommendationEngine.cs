using System.Collections.Immutable;
using Dwellio.Core.Models;

namespace Dwellio.Core.Services;

public static class RecommendationEngine
{
    // A predicate is a function supplied to LINQ Where: lecture's higher-order functions.
    public static Func<Apartment, bool> FitsBudget(Preferences preferences) => apartment =>
        apartment.Available && apartment.Rent <= preferences.Budget && apartment.Rooms >= preferences.MinRooms
        && (!preferences.FurnishedOnly || apartment.Furnished);

    public static OperationResult Validate(Preferences p)
    {
        if (p.Budget <= 0 || p.MinRooms is < 1 or > 6) return OperationResult.Fail("Check the budget and number of rooms.");
        if (p.ArrivalHour is < 0 or > 23 || p.ReturnHour is < 0 or > 23) return OperationResult.Fail("Hours must be between 0 and 23.");
        if (p.MaxTravelMinutes is < 5 or > 180 || p.MaxTransfers is < 0 or > 4 || p.WalkLimit is < 1 or > 60)
            return OperationResult.Fail("Check commute and walking limits.");
        if (p.PriceWeight < 0 || p.LifestyleWeight < 0 || p.TravelWeight < 0 ||
            p.PriceWeight + p.LifestyleWeight + p.TravelWeight == 0) return OperationResult.Fail("Choose at least one priority.");
        if (p.TransportCost < 0 || p.GymCost < 0) return OperationResult.Fail("Costs cannot be negative.");
        return OperationResult.Ok();
    }

    public static Trip? ChooseTrip(IEnumerable<Trip> trips, Preferences p) => trips
        .Where(t => t.Minutes <= p.MaxTravelMinutes && t.Transfers <= p.MaxTransfers)
        .OrderBy(t => p.Priority switch
        {
            "comfort" => t.Minutes + t.Transfers * 15 + (t.Crowding switch { Crowding.High => 35, Crowding.Medium => 15, Crowding.Unknown => 20, _ => 0 }),
            "transfers" => t.Transfers * 200 + t.Minutes,
            _ => t.Minutes
        }).ThenBy(t => t.Name).FirstOrDefault();

    // All external information is an argument. No database, clock or HTTP inside this function.
    public static Match? Evaluate(Apartment apartment, Complex complex, Preferences p, RouteResult routes)
    {
        if (!Validate(p).Success || !FitsBudget(p)(apartment)) return null;
        bool Nearby(string category) => complex.Amenities.Any(a => a.Category == category && a.WalkMinutes <= p.WalkLimit);
        if (p.Required.Any(category => !Nearby(category))) return null;
        var reasons = ImmutableArray.CreateBuilder<string>();
        var missing = ImmutableArray.CreateBuilder<string>();
        reasons.Add("Rent is within budget");
        foreach (var category in p.Wanted)
            if (Nearby(category)) reasons.Add($"{Labels.Category(category)} within {p.WalkLimit} min");
            else missing.Add($"Not confirmed: {Labels.Category(category)} within {p.WalkLimit} min");
        var outbound = routes is RoutesFound found ? ChooseTrip(found.Value.Outbound, p) : null;
        var back = routes is RoutesFound foundBack ? ChooseTrip(foundBack.Value.Return, p) : null;
        if (routes is RoutesFound && (outbound is null || back is null)) return null;
        if (outbound is not null && back is not null) reasons.Add($"Outbound / return: {outbound.Minutes} / {back.Minutes} min · demo");
        else missing.Add(routes switch { RoutesMissing x => x.Message, RoutesUnavailable x => x.Message, _ => "Commute data unavailable" });
        if (apartment.Utilities is null) missing.Add("Utilities cost is unknown");
        decimal price = Math.Clamp(1 - apartment.Rent / p.Budget, 0, 1);
        decimal lifestyle = p.Wanted.Length == 0 ? 0 : (decimal)p.Wanted.Count(Nearby) / p.Wanted.Length;
        decimal travel = outbound is null || back is null ? 0 : Math.Clamp(1 - (outbound.Minutes + back.Minutes) / (2m * p.MaxTravelMinutes), 0, 1);
        var weight = p.PriceWeight + p.LifestyleWeight + p.TravelWeight;
        var score = Math.Round(100 * (price * p.PriceWeight + lifestyle * p.LifestyleWeight + travel * p.TravelWeight) / weight);
        return new(apartment, complex, score, reasons.ToImmutable(), missing.ToImmutable(), outbound, back);
    }

    public static MonthlyCost CalculateCost(Apartment apartment, Complex complex, Preferences p)
    {
        bool includedGym = complex.Amenities.Any(a => a.Category == "gym" && a.Included && a.WalkMinutes == 0);
        decimal gym = p.Wanted.Contains("gym") && !includedGym ? p.GymCost : 0;
        return new(apartment.Rent + (apartment.Utilities ?? 0) + p.TransportCost + gym,
            apartment.Deposit, apartment.Utilities is null, gym);
    }
}
