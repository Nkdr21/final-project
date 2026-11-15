using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dwellio.Web.Data;

public static class SeedData
{
    /// <summary>Date the sourced listings were reviewed. Prices and availability may have changed since.</summary>
    public static readonly DateOnly SourcesReviewedOn = new(2026, 11, 15);

    // Real complexes. Coordinates from OpenStreetMap Nominatim (© OpenStreetMap contributors, ODbL).
    private static readonly ComplexEntity[] Complexes =
    [
        new() { Id = 1, Name = "GreenLine.Silva", Address = "E-899 Street 5, Nura district", Latitude = 51.13795, Longitude = 71.38791,
            Description = "Part of BI Group's GreenLine quarter west of the Esil river, with parks and shopping planned around the buildings." },
        new() { Id = 2, Name = "GreenLine.Tokyo", Address = "Mukhamedkhanov Street 4B, Nura district", Latitude = 51.13639, Longitude = 71.39945,
            Description = "GreenLine building close to Khan Shatyr and the Turan avenue offices." },
        new() { Id = 3, Name = "GreenLine.Headliner", Address = "Tole Bi Street 48/1, Nura district", Latitude = 51.13163, Longitude = 71.39910,
            Description = "GreenLine building on Tole Bi street, a short walk from Khan Shatyr." },
        new() { Id = 4, Name = "Expo New Life", Address = "Mangilik El Avenue 51/2, Yesil district", Latitude = 51.09486, Longitude = 71.42113,
            Description = "Next to the EXPO site and Astana IT University on Mangilik El avenue." },
        new() { Id = 5, Name = "Promenade Expo", Address = "Mangilik El Avenue 52, Yesil district", Latitude = 51.09363, Longitude = 71.42478,
            Description = "On Mangilik El avenue near EXPO; bus 46 stops at ZhK Promenad Ekspo." },
        new() { Id = 6, Name = "Dream City", Address = "Mangilik El Avenue 47, Yesil district", Latitude = 51.09766, Longitude = 71.42690,
            Description = "On Mangilik El avenue between EXPO and the Triumphal Arch." },
        new() { Id = 7, Name = "Comfort Town", Address = "Yesil district", Latitude = 51.08910, Longitude = 71.44703,
            Description = "Large residential area south-east of EXPO and the terminus of bus 46." },
        new() { Id = 8, Name = "Nova City", Address = "Yesil district", Latitude = 51.08941, Longitude = 71.45204,
            Description = "Residential complex next to Comfort Town, served by bus 46 (ZhK Nova city stop)." },
        new() { Id = 9, Name = "Orynbor Towers", Address = "Yesil district", Latitude = 51.08989, Longitude = 71.42620,
            Description = "Towers near EXPO with a bus 46 stop of the same name." },
        new() { Id = 10, Name = "Highvill Astana", Address = "Saraishyq district", Latitude = 51.12712, Longitude = 71.46509,
            Description = "Large complex on the right bank with its own courtyards and shops." },
        new() { Id = 11, Name = "Abu Dhabi Plaza", Address = "Syganak Street 60/5, Yesil district", Latitude = 51.12205, Longitude = 71.42796,
            Description = "High-rise complex near the Presidential Park and the government quarter." },
    ];

    private const string SourcedNote = "Published asking rent from krisha.kz. Confirm price and availability with the advertiser.";
    private const string SampleNote = "Sample listing: the price is an illustration for planning, not an offer. The complex and its location are real.";

    private static ListingEntity[] Listings() =>
    [
        Sourced(1, "1-room apartment in GreenLine.Silva", 250000, 100000, 1, 35, true, "https://krisha.kz/a/show/1010117448",
            "https://krisha-photos.kcdn.online/webp/34/345f84d9-3612-4b70-8dc1-91566b260d79/8-750x470.jpg",
            "Individual listing reviewed; utilities charged separately. Furnished according to the advertiser."),
        Sourced(2, "1-room apartment in GreenLine.Tokyo", 290000, 50000, 1, 48, true, "https://krisha.kz/a/show/672000122", null,
            "Seen in complex search results; the individual page could not be retrieved. Utilities charged separately."),
        Sourced(3, "2-room apartment in GreenLine.Headliner", 400000, null, 2, 61, true, "https://krisha.kz/a/show/1008481040",
            "https://krisha-photos.kcdn.online/webp/32/3206084c-0f69-4979-a19d-dfca6c427ca5/2-750x470.jpg",
            "Individual listing reviewed. Furnished according to the advertiser; deposit and utilities not provided."),
        Sourced(4, "2-room apartment in Expo New Life", 330000, 200000, 2, 77, true, "https://krisha.kz/a/show/1002990387",
            "https://krisha-photos.kcdn.online/webp/ae/ae29f4e7-c557-45b7-9fea-6746b1fc6e89/1-750x470.jpg",
            "Individual listing reviewed. Utilities extra; the advertiser asks for two months in advance."),
        Sourced(5, "2-room apartment in Promenade Expo", 300000, null, 2, 74, true, "https://krisha.kz/a/show/1017128178",
            "https://krisha-photos.kcdn.online/webp/27/2760d0de-d3c7-4c4a-86a4-886763442f47/1-750x470.jpg",
            "Archived listing — historical price only. Furnished according to the advertiser; deposit not confirmed.", available: false),
        Sourced(6, "2-room apartment in Dream City", 350000, 200000, 2, 70, true, "https://krisha.kz/a/show/1013748996",
            "https://krisha-photos.kcdn.online/webp/e9/e92db17f-4d88-437c-8963-4b0389d6f9a9/2-750x470.jpg",
            "Price conflict: headline says 350,000 ₸, description says 400,000 ₸ plus utilities. Confirm with the advertiser."),
        Sample(7, "Studio for a student in Comfort Town", 150000, 18000, 1, 28, true, "/images/home-1.svg"),
        Sample(7, "1-room apartment in Comfort Town", 180000, 20000, 1, 38, true, "/images/home-2.svg"),
        Sample(8, "2-room family apartment in Nova City", 260000, 26000, 2, 62, false, "/images/home-3.svg"),
        Sample(9, "1-room apartment in Orynbor Towers", 280000, 22000, 1, 42, true, "/images/home-4.svg"),
        Sample(10, "2-room apartment in Highvill", 450000, 35000, 2, 75, true, "/images/home-2.svg"),
        Sample(11, "1-room apartment in Abu Dhabi Plaza", 400000, 30000, 1, 45, true, "/images/home-3.svg"),
    ];

    private static ListingEntity Sourced(int complex, string title, decimal rent, decimal? deposit, int rooms, int area, bool furnished,
        string url, string? photo, string evidence, bool available = true) => new()
    {
        ComplexId = complex, Kind = ListingKind.Sourced, Title = title, Rent = rent, Deposit = deposit, Rooms = rooms, Area = area,
        Furnished = furnished, Available = available, SourceUrl = url, Photo = photo ?? "", Evidence = evidence,
        Description = $"{evidence} {SourcedNote}", OwnerId = "source", Contact = "See the original listing", Floor = 5,
        UpdatedUtc = SourcesReviewedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
    };

    private static ListingEntity Sample(int complex, string title, decimal rent, decimal utilities, int rooms, int area, bool furnished, string photo) => new()
    {
        ComplexId = complex, Kind = ListingKind.Sample, Title = title, Rent = rent, Utilities = utilities, Deposit = rent,
        Rooms = rooms, Area = area, Furnished = furnished, Photo = photo, Evidence = SampleNote, Floor = 7,
        Description = $"{SampleNote} Use it to see how Dwellio scores daily life and the commute from this building.",
        OwnerId = "sample", Contact = "Sample listing — no contact",
    };

    public static async Task Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await identity.Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Student", "Owner", "Admin" })
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new(role));

        var password = scope.ServiceProvider.GetRequiredService<IConfiguration>()["DWELLIO_DEMO_PASSWORD"];
        // Demo users are opt-in; no password is tracked in Git.
        if (!string.IsNullOrWhiteSpace(password))
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var role in new[] { "Student", "Owner", "Admin" })
            {
                var email = $"{role.ToLowerInvariant()}@dwellio.test";
                if (await users.FindByEmailAsync(email) is not null) continue;
                var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await users.CreateAsync(user, password);
                if (!result.Succeeded) throw new InvalidOperationException("Demo password does not satisfy the Identity password policy.");
                await users.AddToRoleAsync(user, role);
            }
        }

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HousingDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.EnsureCurrentSchemaAsync();
        await SeedHousingAsync(db);
    }

    public static async Task SeedHousingAsync(HousingDbContext db)
    {
        if (await db.Complexes.AnyAsync()) return;
        db.Complexes.AddRange(Complexes.Select(c => new ComplexEntity
        {
            Id = c.Id, Name = c.Name, Address = c.Address, Latitude = c.Latitude, Longitude = c.Longitude, Description = c.Description,
        }));
        db.Listings.AddRange(Listings());
        await db.SaveChangesAsync();
    }
}
