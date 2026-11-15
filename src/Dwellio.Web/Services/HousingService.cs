using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Dwellio.Core.Models;
using Dwellio.Core.Matching;
using Dwellio.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
namespace Dwellio.Web.Services;

public class HousingService(IDbContextFactory<HousingDbContext> factory, AuthenticationStateProvider auth)
{
    private async Task<ClaimsPrincipal> User() => (await auth.GetAuthenticationStateAsync()).User;
    private async Task<string?> UserId() => (await User()).FindFirstValue(ClaimTypes.NameIdentifier);
    public async Task<List<ComplexEntity>> Complexes()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Complexes.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }
    public async Task<List<ListingEntity>> Listings(bool own = false, bool admin = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Listings.AsNoTracking();
        var user = await User();
        if (admin) { if (!user.IsInRole("Admin")) return []; }
        else if (own) { var id = await UserId(); if (id is null) return []; query = query.Where(x => x.OwnerId == id); }
        else query = query.Where(x => !x.Hidden && x.Available);
        return await query.OrderByDescending(x => x.UpdatedUtc).Take(500).ToListAsync();
    }
    public async Task<ListingEntity?> Listing(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var item = await db.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return null;
        var user = await User();
        return item.Hidden && item.OwnerId != await UserId() && !user.IsInRole("Admin") ? null : item;
    }
    public async Task<OperationResult> SaveListing(ListingEntity input)
    {
        var user = await User(); var id = await UserId();
        if (id is null || !(user.IsInRole("Owner") || user.IsInRole("Admin"))) return OperationResult.Fail("An owner account is required.");
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true)) return OperationResult.Fail(string.Join(" ", errors.Select(x => x.ErrorMessage)));
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Complexes.AnyAsync(x => x.Id == input.ComplexId)) return OperationResult.Fail("Select a residential complex.");
        ListingEntity entity;
        if (input.Id == 0) { entity = new ListingEntity { OwnerId = id, Kind = ListingKind.Owner }; db.Listings.Add(entity); }
        else
        {
            var existing = await db.Listings.FindAsync(input.Id);
            if (existing is null || (existing.OwnerId != id && !user.IsInRole("Admin"))) return OperationResult.Fail("You cannot edit this listing.");
            entity = existing;
        }
        entity.Title = input.Title.Trim(); entity.Description = input.Description.Trim(); entity.ComplexId = input.ComplexId;
        entity.Rent = input.Rent; entity.Utilities = input.Utilities; entity.Deposit = input.Deposit;
        entity.Rooms = input.Rooms; entity.Area = input.Area; entity.Floor = input.Floor;
        entity.Furnished = input.Furnished; entity.Available = input.Available; entity.Contact = input.Contact.Trim();
        // Only local uploaded images or bundled illustrations can be persisted.
        if (input.Photo.StartsWith("/uploads/") || input.Photo.StartsWith("/images/")) entity.Photo = input.Photo;
        entity.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(); input.Id = entity.Id;
        return OperationResult.Ok();
    }
    public async Task<HashSet<int>> Favorites()
    {
        var id = await UserId(); if (id is null) return [];
        await using var db = await factory.CreateDbContextAsync();
        return (await db.Favorites.Where(x => x.UserId == id).Select(x => x.ListingId).ToListAsync()).ToHashSet();
    }
    public async Task<OperationResult> ToggleFavorite(int listing)
    {
        var id = await UserId(); if (id is null) return OperationResult.Fail("Sign in to save a home.");
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Listings.AnyAsync(x => x.Id == listing && !x.Hidden && x.Available)) return OperationResult.Fail("Listing unavailable.");
        var item = await db.Favorites.FindAsync(id, listing);
        if (item is null) db.Favorites.Add(new() { UserId = id, ListingId = listing }); else db.Favorites.Remove(item);
        await db.SaveChangesAsync(); return OperationResult.Ok(item is null ? "Added to saved homes" : "Removed from saved homes");
    }
    public async Task<Preferences?> LoadPreferences()
    {
        var id = await UserId(); if (id is null) return null;
        await using var db = await factory.CreateDbContextAsync();
        var profile = await db.Profiles.FindAsync(id);
        if (profile is null) return null;
        try { return JsonSerializer.Deserialize<Preferences>(profile.PreferencesJson) is { } p ? HomeMatcher.Sanitize(p) : null; }
        catch (JsonException) { return null; } // saved by an older version: start from defaults
    }
    public async Task<OperationResult> SavePreferences(Preferences value)
    {
        var valid = HomeMatcher.Validate(value); if (!valid.Success) return valid;
        var id = await UserId(); if (id is null) return OperationResult.Fail("Sign in to keep your plan on every device. It is already saved in this browser.");
        await using var db = await factory.CreateDbContextAsync();
        var profile = await db.Profiles.FindAsync(id);
        if (profile is null) { profile = new() { UserId = id }; db.Profiles.Add(profile); }
        profile.PreferencesJson = JsonSerializer.Serialize(value);
        await db.SaveChangesAsync(); return OperationResult.Ok("Plan saved to your profile");
    }
    public async Task<List<ReviewEntity>> Reviews(int complexId = 0, bool admin = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Reviews.AsNoTracking();
        if (admin && !(await User()).IsInRole("Admin")) return [];
        if (!admin) query = query.Where(x => !x.Hidden);
        if (complexId != 0) query = query.Where(x => x.ComplexId == complexId);
        return await query.OrderByDescending(x => x.CreatedUtc).ToListAsync();
    }
    public async Task<OperationResult> SaveReview(ReviewEntity input)
    {
        var id = await UserId(); if (id is null) return OperationResult.Fail("Sign in to write a review.");
        input.Text = (input.Text ?? "").Trim();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true)) return OperationResult.Fail("Text must be 10–1000 characters; ratings must be 1–5.");
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Complexes.AnyAsync(x => x.Id == input.ComplexId)) return OperationResult.Fail("Complex not found.");
        var item = await db.Reviews.SingleOrDefaultAsync(x => x.UserId == id && x.ComplexId == input.ComplexId);
        if (item is null) { item = new() { UserId = id, ComplexId = input.ComplexId, Author = "User" }; db.Reviews.Add(item); }
        item.Text = input.Text.Trim(); item.Cleanliness = input.Cleanliness; item.Quietness = input.Quietness;
        item.Facilities = input.Facilities; item.Hidden = false; item.CreatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(); return OperationResult.Ok("Review saved. Residency is not verified.");
    }
    public async Task<OperationResult> Moderate(int id, bool review)
    {
        if (!(await User()).IsInRole("Admin")) return OperationResult.Fail("Access denied.");
        await using var db = await factory.CreateDbContextAsync();
        if (review) { var item = await db.Reviews.FindAsync(id); if (item is null) return OperationResult.Fail("Not found"); item.Hidden = !item.Hidden; }
        else { var item = await db.Listings.FindAsync(id); if (item is null) return OperationResult.Fail("Not found"); item.Hidden = !item.Hidden; }
        await db.SaveChangesAsync(); return OperationResult.Ok();
    }
}
