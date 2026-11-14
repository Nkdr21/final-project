namespace Dwellio.Web.Data;

// Small manually researched snapshot, not a live feed or an availability guarantee.
public sealed record MarketReference(
    string Complex, string Address, int Rooms, decimal Area, decimal Rent,
    decimal? Deposit, string SourceUrl, string Evidence, bool OnMangilikEl = false, bool Archived = false);

public static class MarketReferences
{
    public static readonly DateOnly ReviewedOn = new(2026, 11, 15);
    public static IReadOnlyList<MarketReference> All { get; } = Array.AsReadOnly(new[]
    {
        new MarketReference("GreenLine.Silva", "Е-899 5, Astana", 1, 35, 250000, 100000,
            "https://krisha.kz/a/show/1010117448", "Individual listing reviewed; utilities charged separately. Furnished according to advertiser."),
        new MarketReference("GreenLine.Tokyo", "Мухамедханова 4б, Astana", 1, 48, 290000, 50000,
            "https://krisha.kz/a/show/672000122", "Observed in the complex rental search results; individual page could not be retrieved. Utilities charged separately."),
        new MarketReference("GreenLine.Headliner", "Толе би 48/1, Astana", 2, 61, 400000, null,
            "https://krisha.kz/a/show/1008481040", "Individual listing reviewed. Furnished according to advertiser; deposit and utility amount not provided."),
        new MarketReference("Expo New Life-2", "Мәңгілік Ел 51/2, Astana", 2, 77, 330000, 200000,
            "https://krisha.kz/a/show/1002990387", "Individual listing reviewed. Utilities extra; advertiser requests two months of rent in advance.", true),
        new MarketReference("Promenade Expo", "Мәңгілік Ел 52, Astana", 2, 74, 300000, null,
            "https://krisha.kz/a/show/1017128178", "Archived listing: historical price example only. Furnished according to advertiser; deposit not confirmed.", true, true),
        new MarketReference("Dream City", "Мәңгілік Ел 47, Astana", 2, 70, 350000, null,
            "https://krisha.kz/a/show/1013748996", "Individual listing reviewed. Furnished according to advertiser; deposit and utility amount not confirmed.", true)
    });
}
