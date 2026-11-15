using System.Collections.Immutable;
using Dwellio.Core.Models;
using Dwellio.Core.Places;
using Dwellio.Core.Transit;

namespace Dwellio.Core.Matching;

/// <summary>
/// Ranks homes for a person. Every input is an argument — no database, HTTP or clock inside —
/// so the same inputs always give the same ranking and every rule is unit-testable.
/// </summary>
public static class HomeMatcher
{
    public static OperationResult Validate(Preferences p)
    {
        if (p.Budget <= 0) return OperationResult.Fail("Enter a monthly rent budget above zero.");
        if (p.MinRooms is < 1 or > 6) return OperationResult.Fail("Rooms must be between 1 and 6.");
        if (p.ArriveByHour is < 6 or > 23) return OperationResult.Fail("Arrival time must be between 06:00 and 23:00.");
        if (p.MaxCommuteMinutes is < 5 or > 180) return OperationResult.Fail("Maximum commute must be 5–180 minutes.");
        if (p.WalkLimit is < 3 or > 30) return OperationResult.Fail("Walking limit must be 3–30 minutes.");
        if (p.PriceWeight < 0 || p.LifestyleWeight < 0 || p.CommuteWeight < 0 || p.PriceWeight + p.LifestyleWeight + p.CommuteWeight == 0)
            return OperationResult.Fail("Give at least one priority a weight above zero.");
        if (p.TransportCost < 0 || p.GymCost < 0) return OperationResult.Fail("Costs cannot be negative.");
        if (!Geo.GeoMath.InAstana(p.Destination)) return OperationResult.Fail("Choose a destination inside Astana.");
        return OperationResult.Ok();
    }

    /// <summary>Repairs values loaded from old or tampered storage instead of failing.</summary>
    public static Preferences Sanitize(Preferences p)
    {
        var defaults = new Preferences();
        var clean = p with
        {
            DestinationName = string.IsNullOrWhiteSpace(p.DestinationName) ? defaults.DestinationName : p.DestinationName.Trim(),
            Needs = PlaceCatalog.Normalize(p.Needs.NullToEmpty()),
            MustHaves = PlaceCatalog.Normalize(p.MustHaves.NullToEmpty()),
            Priority = Enum.IsDefined(p.Priority) ? p.Priority : CommutePriority.Balanced,
        };
        if (!Geo.GeoMath.InAstana(clean.Destination))
            clean = clean with { DestinationName = defaults.DestinationName, DestinationLat = defaults.DestinationLat, DestinationLon = defaults.DestinationLon };
        return Validate(clean).Success ? clean : defaults with
        {
            DestinationName = clean.DestinationName, DestinationLat = clean.DestinationLat, DestinationLon = clean.DestinationLon,
            Needs = clean.Needs, MustHaves = clean.MustHaves,
        };
    }

    public static MonthlyCost Cost(Apartment a, Preferences p) =>
        new(a.Rent, a.Utilities, p.TransportCost, p.AllNeeds.Contains("gym") ? p.GymCost : 0, a.Deposit);

    // ---- Score parts: each is a pure function returning 0..1. ----

    public static double PriceScore(Apartment a, Preferences p) => (double)Math.Clamp(1 - a.Rent / p.Budget, 0, 1) * 0.6 + (a.Rent <= p.Budget ? 0.4 : 0);

    public static double CommuteScore(CommutePlan plan, Preferences p)
    {
        if (plan.TotalMinutes is not { } minutes) return 0; // unknown never becomes a positive rating
        var time = Math.Clamp(1 - (double)minutes / (p.MaxCommuteMinutes * 1.5), 0, 1);
        return plan switch
        {
            TransitPlan t when p.Priority == CommutePriority.Seat => time * (0.5 + 0.5 * t.Best.SeatChance),
            TransitPlan => time,
            EstimatedBusPlan => time * 0.85, // rough estimate: discounted for uncertainty
            _ => time,
        };
    }

    public static HomeMatch Evaluate(Apartment a, Complex c, AreaReport area, CommutePlan commute, Preferences p)
    {
        var exclusions = ImmutableArray.CreateBuilder<string>();
        var reasons = ImmutableArray.CreateBuilder<string>();
        var gaps = ImmutableArray.CreateBuilder<string>();

        if (!a.Available) exclusions.Add("No longer available");
        if (a.Rent > p.Budget) exclusions.Add($"Rent {Labels.Money(a.Rent)} is above your {Labels.Money(p.Budget)} budget");
        else reasons.Add($"Within budget · {Labels.Money(p.Budget - a.Rent)} to spare");
        if (a.Rooms < p.MinRooms) exclusions.Add($"Only {a.Rooms} room(s); you need {p.MinRooms}");
        if (p.FurnishedOnly && !a.Furnished) exclusions.Add("Not furnished");
        foreach (var need in area.MissingRequired)
            exclusions.Add($"Must-have missing: no {need.Category.Label.ToLowerInvariant()} within {p.WalkLimit} min walk");
        if (commute.TotalMinutes is { } total && total > p.MaxCommuteMinutes)
            exclusions.Add($"Commute about {Labels.Minutes(total)}, above your {p.MaxCommuteMinutes} min limit");

        foreach (var need in area.Needs)
        {
            if (need.Satisfied(p.WalkLimit)) reasons.Add($"{need.Category.Label}: {need.Nearest!.DisplayName}, {need.WalkMinutes} min walk");
            else if (!need.Required) gaps.Add(need.WalkMinutes is { } m ? $"{need.Category.Label} is {m} min away" : $"No {need.Category.Label.ToLowerInvariant()} found nearby in OpenStreetMap");
        }
        switch (commute)
        {
            case TransitPlan t: reasons.Add($"Bus {t.Best.Route.Ref}: {Labels.Minutes(t.Best.TotalMinutes)} door to door · {Labels.SeatWord(t.Best.SeatChance).ToLowerInvariant()}"); break;
            case WalkPlan w: reasons.Add($"Walk to {p.DestinationName} in {w.Minutes} min"); break;
            case EstimatedBusPlan e: gaps.Add($"Commute ≈ {Labels.Minutes(e.TotalMinutes)} (estimate: no open timetable for this trip)"); break;
            case NoCommute n: gaps.Add(n.Reason); break;
        }
        if (a.Utilities is null) gaps.Add("Utilities cost not provided");

        var parts = new ScoreParts(PriceScore(a, p), area.LifestyleScore, CommuteScore(commute, p));
        var weight = (double)(p.PriceWeight + p.LifestyleWeight + p.CommuteWeight);
        var score = (int)Math.Round(100 * (parts.Price * p.PriceWeight + parts.Lifestyle * p.LifestyleWeight + parts.Commute * p.CommuteWeight) / weight);

        return new(a, c, area, commute, score, parts, reasons.ToImmutable(), gaps.ToImmutable(), exclusions.ToImmutable());
    }

    /// <summary>Eligible homes first (best score first), then excluded ones with their reasons.</summary>
    public static ImmutableArray<HomeMatch> Rank(IEnumerable<HomeMatch> matches) => matches
        .OrderByDescending(m => m.Eligible)
        .ThenByDescending(m => m.Score)
        .ThenBy(m => m.Apartment.Rent)
        .ToImmutableArray();
}
