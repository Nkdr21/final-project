namespace Dwellio.Web.Data;

// Small manually researched snapshot, not a live feed or an availability guarantee.
public sealed record MarketReference(
    string Complex, string Address, int Rooms, decimal Area, decimal Rent,
    decimal? Deposit, string SourceUrl, string Evidence, bool OnMangilikEl = false, bool Archived = false, string? PhotoUrl = null);

public static class MarketReferences
{
    public static readonly DateOnly ReviewedOn = new(2026, 11, 15);
    public static IReadOnlyList<MarketReference> All { get; } = Array.AsReadOnly(new[]
    {
        new MarketReference("GreenLine.Silva", "E-899 5, Astana", 1, 35, 250000, 100000,
            "https://krisha.kz/a/show/1010117448", "Individual listing reviewed; utilities charged separately. Furnished according to advertiser.", PhotoUrl: "https://krisha-photos.kcdn.online/webp/34/345f84d9-3612-4b70-8dc1-91566b260d79/8-750x470.jpg"),
        new MarketReference("GreenLine.Tokyo", "Mukhamedkhanov Street 4B, Astana", 1, 48, 290000, 50000,
            "https://krisha.kz/a/show/672000122", "Observed in the complex rental search results; individual page could not be retrieved. Utilities charged separately."),
        new MarketReference("GreenLine.Headliner", "Tole Bi Street 48/1, Astana", 2, 61, 400000, null,
            "https://krisha.kz/a/show/1008481040", "Individual listing reviewed. Furnished according to advertiser; deposit and utility amount not provided.", PhotoUrl: "https://krisha-photos.kcdn.online/webp/32/3206084c-0f69-4979-a19d-dfca6c427ca5/2-750x470.jpg"),
        new MarketReference("Expo New Life-2", "Mangilik El Avenue 51/2, Astana", 2, 77, 330000, 200000,
            "https://krisha.kz/a/show/1002990387", "Individual listing reviewed. Utilities extra; advertiser requests two months of rent in advance.", true, PhotoUrl: "https://krisha-photos.kcdn.online/webp/ae/ae29f4e7-c557-45b7-9fea-6746b1fc6e89/1-750x470.jpg"),
        new MarketReference("Promenade Expo", "Mangilik El Avenue 52, Astana", 2, 74, 300000, null,
            "https://krisha.kz/a/show/1017128178", "Archived listing: historical price example only. Furnished according to advertiser; deposit not confirmed.", true, true, PhotoUrl: "https://krisha-photos.kcdn.online/webp/27/2760d0de-d3c7-4c4a-86a4-886763442f47/1-750x470.jpg"),
        new MarketReference("Dream City", "Mangilik El Avenue 47, Astana", 2, 70, 350000, 200000,
            "https://krisha.kz/a/show/1013748996", "Price conflict: headline says 350,000 KZT; description says 400,000 KZT plus utilities. Confirm with advertiser. Deposit stated as 200,000 KZT.", true, PhotoUrl: "https://krisha-photos.kcdn.online/webp/e9/e92db17f-4d88-437c-8963-4b0389d6f9a9/2-750x470.jpg")
    });
}
