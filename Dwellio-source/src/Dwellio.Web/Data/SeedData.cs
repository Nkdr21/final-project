using System.Text.Json;
using Dwellio.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Dwellio.Web.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await identity.Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Student", "Owner", "Admin" })
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new(role));
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var password = config["DWELLIO_DEMO_PASSWORD"];
        // Demo users are opt-in; no password is tracked in Git.
        if (!string.IsNullOrWhiteSpace(password))
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var role in new[] { "Student", "Owner", "Admin" })
            {
                var email = $"{role.ToLowerInvariant()}@dwellio.test";
                if (await users.FindByEmailAsync(email) is null)
                {
                    var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    var result = await users.CreateAsync(user, password);
                    if (!result.Succeeded) throw new InvalidOperationException("Demo password does not satisfy Identity password policy.");
                    await users.AddToRoleAsync(user, role);
                }
            }
        }
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HousingDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        if (await db.Complexes.AnyAsync()) return;
        string[] names = ["Garden Quarter", "River House", "Campus Park", "Urban Yard", "Green Avenue", "Skyline Place", "Quiet Corner", "Sport Residence"];
        double[] lat = [51.115, 51.135, 51.094, 51.125, 51.105, 51.145, 51.155, 51.100];
        double[] lon = [71.425, 71.435, 71.410, 71.455, 71.445, 71.415, 71.445, 71.430];
        for (int i = 0; i < names.Length; i++)
        {
            var amenities = new List<Amenity> {
                new("groceries", "Local grocery · demo", 3 + i % 4, false, "Public access; opening hours unverified"),
                new("pharmacy", "Pharmacy · demo", 5 + i, false, "Public access; opening hours unverified"),
                new("park", "Walking area · demo", 4 + i * 2, true, "Open area · scenario"),
                new("gym", "Fitness space · demo", i % 3 == 0 ? 0 : 6 + i * 2, i % 3 == 0, i % 3 == 0 ? "Residents only, included · scenario" : "Paid membership · scenario") };
            if (i % 2 == 0) amenities.Add(new("football", "Sports court · demo", 4 + i, true, "Public access · scenario"));
            if (i % 3 != 0) amenities.Add(new("coworking", "Study space · demo", 2 + i, false, "Price unknown"));
            amenities.Add(new("cafe", "Cafe · demo", 4 + i, false, "Public access"));
            db.Complexes.Add(new()
            {
                Id = i + 1,
                Name = names[i],
                Address = "Astana · demo location " + (i + 1),
                Latitude = lat[i],
                Longitude = lon[i],
                Photo = $"/images/home-{i % 4 + 1}.svg",
                AmenitiesJson = JsonSerializer.Serialize(amenities),
                Description = "A fictional complex for an educational demonstration. Bright shared spaces, a courtyard and room for everyday life. Coordinates are illustrative; amenities have not been verified on site."
            });
            for (int j = 0; j < 4; j++) db.Listings.Add(new()
            {
                ComplexId = i + 1,
                Title = new[] { "A bright home with courtyard views", "Space to study and slow down", "A cosy home with a separate kitchen", "More room for your everyday" }[j],
                Description = "A demo listing, not a rental offer. Thoughtful spaces for study and rest, a bright living area and useful storage. The image is a concept illustration, not a photograph of an actual apartment.",
                Rent = 125000 + i * 12000 + j * 25000,
                Utilities = j == 3 ? null : 15000 + i * 1000,
                Deposit = 100000,
                Rooms = j < 2 ? 1 : 2,
                Area = 35 + j * 12,
                Floor = 3 + i,
                Furnished = j != 2,
                OwnerId = "demo",
                IsDemo = true,
                Photo = $"/images/home-{(i + j) % 4 + 1}.svg",
                Contact = "Demo listing: no real owner contact"
            });
        }
        await db.SaveChangesAsync();
    }
}
