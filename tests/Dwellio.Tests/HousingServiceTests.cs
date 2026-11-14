using System.Security.Claims;
using Dwellio.Core.Models;
using Dwellio.Web.Data;
using Dwellio.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dwellio.Tests;

// Real in-memory SQLite verifies EF constraints and persistence, not mocked LINQ.
public class HousingServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private DbContextOptions<HousingDbContext> options = default!;
    private readonly TestAuthentication auth = new();
    private HousingService service = default!;
    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        options = new DbContextOptionsBuilder<HousingDbContext>().UseSqlite(connection).Options;
        service = new HousingService(new TestFactory(options), auth);
        await using var db = new HousingDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Complexes.Add(new() { Id = 1, Name = "Test complex" });
        db.Listings.Add(new() { Id = 1, ComplexId = 1, OwnerId = "owner", Title = "Test apartment", Description = "A sufficiently detailed apartment description.", Rent = 150000 });
        await db.SaveChangesAsync();
    }
    public async Task DisposeAsync() => await connection.DisposeAsync();
    [Fact]
    public async Task Guest_cannot_save_preferences_or_favorites()
    {
        Assert.False((await service.SavePreferences(new())).Success);
        Assert.False((await service.ToggleFavorite(1)).Success);
    }
    [Fact]
    public async Task Favorites_are_isolated_between_users_and_toggle()
    {
        auth.SignIn("alice", "Student"); Assert.True((await service.ToggleFavorite(1)).Success);
        Assert.Contains(1, await service.Favorites());
        auth.SignIn("bob", "Student"); Assert.Empty(await service.Favorites());
        auth.SignIn("alice", "Student"); await service.ToggleFavorite(1); Assert.Empty(await service.Favorites());
    }
    [Fact]
    public async Task Preferences_round_trip_and_are_private()
    {
        auth.SignIn("alice", "Student"); var preferences = new Preferences(Budget: 175000, Categories: "park");
        Assert.True((await service.SavePreferences(preferences)).Success); Assert.Equal(preferences, await service.LoadPreferences());
        auth.SignIn("bob", "Student"); Assert.Null(await service.LoadPreferences());
    }
    [Fact]
    public async Task Owner_cannot_edit_another_owners_listing()
    {
        auth.SignIn("other", "Owner"); var item = (await service.Listing(1))!; item.Rent = 200000;
        Assert.False((await service.SaveListing(item)).Success); Assert.Equal(150000, (await service.Listing(1))!.Rent);
    }
    [Fact]
    public async Task Student_cannot_publish_listing()
    {
        auth.SignIn("student", "Student"); Assert.False((await service.SaveListing((await service.Listing(1))!)).Success);
    }
    [Fact]
    public async Task Owner_can_update_own_listing_but_not_owner_id()
    {
        auth.SignIn("owner", "Owner"); var item = (await service.Listing(1))!; item.Rent = 180000; item.OwnerId = "forged";
        Assert.True((await service.SaveListing(item)).Success);
        var saved = (await service.Listing(1))!; Assert.Equal(180000, saved.Rent); Assert.Equal("owner", saved.OwnerId);
    }
    [Fact]
    public async Task Invalid_listing_is_not_persisted()
    {
        auth.SignIn("owner", "Owner"); var item = (await service.Listing(1))!; item.Rent = -100;
        Assert.False((await service.SaveListing(item)).Success); Assert.Equal(150000, (await service.Listing(1))!.Rent);
    }
    [Fact]
    public async Task Only_admin_can_hide_and_restore()
    {
        auth.SignIn("owner", "Owner"); Assert.False((await service.Moderate(1, false)).Success);
        auth.SignIn("admin", "Admin"); Assert.True((await service.Moderate(1, false)).Success);
        auth.SignIn("guest", "Student"); Assert.Null(await service.Listing(1)); Assert.Empty(await service.Listings());
        auth.SignIn("admin", "Admin"); await service.Moderate(1, false);
        auth.SignIn("guest", "Student"); Assert.NotNull(await service.Listing(1));
    }
    [Fact]
    public async Task Review_upsert_keeps_one_review_per_user_and_complex()
    {
        auth.SignIn("student", "Student");
        var review = new ReviewEntity { ComplexId = 1, Text = "Useful shared spaces and a quiet courtyard." };
        Assert.True((await service.SaveReview(review)).Success);
        review.Text = "Updated experience after another month here.";
        await service.SaveReview(review);
        var all = await service.Reviews(1); Assert.Single(all); Assert.Equal(review.Text, all[0].Text);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Invalid_review_rating_is_rejected(int rating)
    {
        auth.SignIn("student", "Student");
        Assert.False((await service.SaveReview(new() { ComplexId = 1, Text = "A detailed review of the place.", Cleanliness = rating })).Success);
        Assert.Empty(await service.Reviews(1));
    }
    private class TestFactory(DbContextOptions<HousingDbContext> options) : IDbContextFactory<HousingDbContext>
    {
        public HousingDbContext CreateDbContext() => new(options);
    }
    private class TestAuthentication : AuthenticationStateProvider
    {
        private ClaimsPrincipal user = new(new ClaimsIdentity());
        public void SignIn(string id, string role) => user = new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Role, role)], "Test"));
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }
}
