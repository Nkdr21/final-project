using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Dwellio.Core.Geo;
using Dwellio.Core.Matching;
using Dwellio.Core.Models;
using Dwellio.Web.Data;
using Dwellio.Web.Services;
using Dwellio.Web.Services.OpenData;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Dwellio.Tests;

/// <summary>Fake browser storage: records what PlannerState writes, can simulate blocked storage.</summary>
internal sealed class FakeBrowser : IJSRuntime
{
    public Dictionary<string, string> Local { get; } = [];
    public Dictionary<string, string> Session { get; } = [];
    public bool Blocked { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args)
    {
        var store = (bool)args![^1]! ? Session : Local;
        var key = (string)args[0]!;
        object? result = identifier switch
        {
            "dwellioStore.get" => store.GetValueOrDefault(key),
            "dwellioStore.set" when Blocked => false,
            "dwellioStore.set" => Set(store, key, (string)args[1]!),
            _ => throw new JSException("unknown " + identifier),
        };
        return ValueTask.FromResult((TValue)result!);
    }

    private static bool Set(Dictionary<string, string> store, string key, string value) { store[key] = value; return true; }
}

public class PlannerStateTests
{
    [Fact]
    public async Task Plan_survives_a_new_circuit_through_browser_storage()
    {
        var browser = new FakeBrowser();
        var first = new PlannerState(browser);
        await first.EnsureLoadedAsync();
        Assert.True(first.IsFirstVisit);
        Assert.True((await first.Update(p => p with { Budget = 200_000, Needs = ["spa"] })).Success);

        var reloaded = new PlannerState(browser);
        await reloaded.EnsureLoadedAsync();
        Assert.False(reloaded.IsFirstVisit);
        Assert.Equal(200_000, reloaded.Preferences.Budget);
        Assert.Equal(["spa"], reloaded.Preferences.Needs.ToArray());
    }

    [Fact]
    public async Task Invalid_update_is_rejected_and_state_is_kept()
    {
        var state = new PlannerState(new FakeBrowser());
        var changes = 0;
        state.Changed += () => changes++;
        var result = await state.Update(p => p with { Budget = 0 });
        Assert.False(result.Success);
        Assert.Equal(new Preferences().Budget, state.Preferences.Budget);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Tampered_storage_is_ignored_or_repaired()
    {
        var browser = new FakeBrowser();
        browser.Local[PlannerState.PlanKey] = "{\"Budget\":-1,\"Needs\":[\"gym\",\"<script>\"],\"WalkLimit\":999}";
        browser.Session[PlannerState.CompareKey] = "[-1,0,2,2,4,6,8]";
        var state = new PlannerState(browser);
        await state.EnsureLoadedAsync();
        Assert.True(HomeMatcher.Validate(state.Preferences).Success);
        Assert.Equal(["gym"], state.Preferences.Needs.ToArray());
        Assert.Equal([2, 4, 6], state.CompareIds.ToArray());

        browser.Local[PlannerState.PlanKey] = "{broken";
        var other = new PlannerState(browser);
        await other.EnsureLoadedAsync();
        Assert.Equal(new Preferences().CacheKey, other.Preferences.CacheKey);
    }

    [Fact]
    public async Task Comparison_is_limited_and_removal_persists()
    {
        var browser = new FakeBrowser();
        var state = new PlannerState(browser);
        foreach (var id in new[] { 1, 2, 3 }) Assert.True((await state.ToggleCompareAsync(id)).Success);
        Assert.False((await state.ToggleCompareAsync(4)).Success);
        await state.ToggleCompareAsync(2);
        var reloaded = new PlannerState(browser);
        await reloaded.EnsureLoadedAsync();
        Assert.Equal([1, 3], reloaded.CompareIds.ToArray());
    }

    [Fact]
    public async Task Blocked_storage_keeps_the_session_working_and_says_so()
    {
        var state = new PlannerState(new FakeBrowser { Blocked = true });
        var result = await state.ToggleCompareAsync(5);
        Assert.True(result.Success);
        Assert.Contains("blocked", result.Message);
        Assert.Contains(5, state.CompareIds);
        Assert.Contains("blocked", (await state.Update(p => p with { Budget = 300_000 })).Message);
    }
}

public class OverpassClientTests
{
    private const string Ok = """{"elements":[{"type":"node","lat":51.1,"lon":71.4,"tags":{"amenity":"cafe","name":"Live cafe"}}]}""";

    private sealed class ScriptedHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] answers) : HttpMessageHandler
    {
        public List<Uri> Calls { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls.Add(request.RequestUri!);
            return Task.FromResult(answers[Math.Min(Calls.Count - 1, answers.Length - 1)](request));
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static OverpassClient Client(ScriptedHandler handler, bool live = true) => new(new Factory(handler),
        Options.Create(new OpenDataOptions { LiveRequests = live, OverpassMirrors = ["https://a.test/api", "https://b.test/api"] }),
        NullLogger<OverpassClient>.Instance);

    [Fact]
    public async Task Busy_mirror_is_skipped_and_the_next_one_answers()
    {
        var handler = new ScriptedHandler(
            _ => new HttpResponseMessage(HttpStatusCode.GatewayTimeout),
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Ok) });
        var places = await Client(handler).FetchAsync(new(51.1, 71.4), 1000, CancellationToken.None);
        Assert.Equal("Live cafe", Assert.Single(places!.Value).Name);
        Assert.Equal(["a.test", "b.test"], handler.Calls.Select(u => u.Host).ToArray());
    }

    [Fact]
    public async Task All_mirrors_failing_returns_null_instead_of_throwing()
    {
        var handler = new ScriptedHandler(
            _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>runtime error</html>") });
        Assert.Null(await Client(handler).FetchAsync(new(51.1, 71.4), 1000, CancellationToken.None));
    }

    [Fact]
    public async Task Offline_mode_makes_no_requests()
    {
        var handler = new ScriptedHandler(_ => throw new InvalidOperationException("should not be called"));
        Assert.Null(await Client(handler, live: false).FetchAsync(new(51.1, 71.4), 1000, CancellationToken.None));
        Assert.Empty(handler.Calls);
    }
}

/// <summary>End-to-end over the real bundled data and seed, fully offline.</summary>
public class HomeFinderIntegrationTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private DbContextOptions<HousingDbContext> options = default!;
    private HomeFinder finder = default!;
    private PlaceService places = default!;

    private sealed class Env : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Dwellio.Web";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = TestData.RepoFile("src", "Dwellio.Web");
        public string EnvironmentName { get; set; } = "Test";
    }

    private sealed class Factory(DbContextOptions<HousingDbContext> o) : IDbContextFactory<HousingDbContext>
    {
        public HousingDbContext CreateDbContext() => new(o);
    }

    private sealed class Guest : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        options = new DbContextOptionsBuilder<HousingDbContext>().UseSqlite(connection).Options;
        await using (var db = new HousingDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            await SeedData.SeedHousingAsync(db);
        }
        var factory = new Factory(options);
        var offline = Options.Create(new OpenDataOptions { LiveRequests = false });
        var env = new Env();
        places = new PlaceService(env, factory, new OverpassClient(new NoHttp(), offline, NullLogger<OverpassClient>.Instance), offline, NullLogger<PlaceService>.Instance);
        var transit = new TransitService(env, places, NullLogger<TransitService>.Instance);
        finder = new HomeFinder(new HousingService(factory, new Guest()), places, transit, new MemoryCache(new MemoryCacheOptions()));
    }

    private sealed class NoHttp : IHttpClientFactory { public HttpClient CreateClient(string name) => throw new InvalidOperationException(); }

    public async Task DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task Seeded_homes_are_all_evaluated_and_ranked()
    {
        var results = await finder.RankAsync(new Preferences());
        Assert.Equal(11, results.Length); // 12 seeded, 1 archived listing is unavailable and not public
        Assert.True(results.First().Match.Eligible);
        var eligible = results.Where(r => r.Match.Eligible).Select(r => r.Match.Score).ToList();
        Assert.Equal(eligible.OrderByDescending(s => s), eligible);
        Assert.All(results, r => Assert.True(GeoMath.InAstana(r.Complex.ToModel().Location)));
    }

    [Fact]
    public async Task Homes_near_aitu_can_walk_and_comfort_town_takes_bus_46()
    {
        var results = await finder.RankAsync(new Preferences());
        Assert.IsType<Dwellio.Core.Transit.WalkPlan>(results.First(r => r.Complex.Name == "Expo New Life").Match.Commute);
        var comfort = Assert.IsType<Dwellio.Core.Transit.TransitPlan>(results.First(r => r.Complex.Name == "Comfort Town").Match.Commute);
        Assert.Equal("46", comfort.Best.Route.Ref);
    }

    [Fact]
    public async Task Offline_refresh_does_nothing_and_reports_snapshot()
    {
        Assert.False(await places.RefreshAsync(new(51.09, 71.42)));
        Assert.False(places.FreshnessAt(new(51.09, 71.42)).Live);
    }

    [Fact]
    public async Task Changing_the_plan_changes_the_results()
    {
        var cheap = await finder.RankAsync(new Preferences { Budget = 160_000 });
        Assert.Single(cheap, r => r.Match.Eligible);
    }
}
