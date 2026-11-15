using Dwellio.Web.Data;
using Dwellio.Web.Services;
using Microsoft.JSInterop;

namespace Dwellio.Tests;

public class MarketShortlistTests
{
    [Fact]
    public async Task Source_comparison_survives_reload_and_removal()
    {
        var storage = new Storage();
        var state = new MarketShortlist(storage);
        var url = MarketReferences.All[0].SourceUrl;
        Assert.True((await state.ToggleAsync(url)).Success);
        var reloaded = new MarketShortlist(storage);
        await reloaded.LoadAsync();
        Assert.Contains(url, reloaded.Selected);
        await reloaded.ToggleAsync(url);
        var removed = new MarketShortlist(storage);
        await removed.LoadAsync();
        Assert.Empty(removed.Selected);
    }
    [Fact]
    public async Task Unknown_urls_are_rejected_and_selection_is_limited()
    {
        var state = new MarketShortlist(new Storage());
        Assert.False((await state.ToggleAsync("https://example.com/unknown")).Success);
        foreach (var home in MarketReferences.All.Take(3)) Assert.True((await state.ToggleAsync(home.SourceUrl)).Success);
        Assert.False((await state.ToggleAsync(MarketReferences.All[3].SourceUrl)).Success);
        Assert.Equal(3, state.Selected.Count);
    }
    [Fact]
    public async Task Restoring_untrusted_storage_filters_unknown_and_duplicate_urls()
    {
        var url = MarketReferences.All[0].SourceUrl;
        var state = new MarketShortlist(new Storage { Urls = ["unknown", url, url] });
        await state.LoadAsync();
        Assert.Equal([url], state.Selected.ToArray());
    }
    [Fact]
    public async Task Blocked_storage_returns_clear_warning()
    {
        var state = new MarketShortlist(new Storage { FailWrite = true });
        var result = await state.ToggleAsync(MarketReferences.All[0].SourceUrl);
        Assert.True(result.Success);
        Assert.Contains("until reload", result.Message);
        Assert.Single(state.Selected);
    }
    private sealed class Storage : IJSRuntime
    {
        public string[] Urls = [];
        public bool FailWrite;
        public ValueTask<T> InvokeAsync<T>(string id, object?[]? args) => InvokeAsync<T>(id, CancellationToken.None, args);
        public ValueTask<T> InvokeAsync<T>(string id, CancellationToken token, object?[]? args)
        {
            if (id == "dwellioMarket.read") return ValueTask.FromResult((T)(object)Urls);
            if (!FailWrite) Urls = (string[])args![0]!;
            return ValueTask.FromResult((T)(object)!FailWrite);
        }
    }
}
