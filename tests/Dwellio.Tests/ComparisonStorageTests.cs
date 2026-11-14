using Dwellio.Web.Services;
using Microsoft.JSInterop;

namespace Dwellio.Tests;

public class ComparisonStorageTests
{
    [Fact]
    public async Task New_circuit_restores_selection_and_removed_home_stays_removed()
    {
        var browser = new BrowserStorage();
        var first = new SearchState(browser);
        await first.ToggleCompareAsync(12);
        await first.ToggleCompareAsync(7);
        var reloaded = new SearchState(browser);
        await reloaded.LoadComparisonAsync();
        Assert.True(reloaded.CompareIds.SetEquals([12, 7]));
        await reloaded.ToggleCompareAsync(12);
        var again = new SearchState(browser);
        await again.LoadComparisonAsync();
        Assert.Equal([7], again.CompareIds.ToArray());
    }

    [Fact]
    public async Task Restored_ids_are_positive_unique_and_limited_to_three()
    {
        var state = new SearchState(new BrowserStorage { Ids = [-1, 0, 2, 2, 4, 6, 8] });
        await state.LoadComparisonAsync();
        Assert.True(state.CompareIds.SetEquals([2, 4, 6]));
        Assert.False((await state.ToggleCompareAsync(9)).Success);
    }

    [Fact]
    public async Task Storage_failure_keeps_session_usable_and_explains_limit()
    {
        var state = new SearchState(new BrowserStorage { FailWrite = true });
        var result = await state.ToggleCompareAsync(2);
        Assert.True(result.Success);
        Assert.Contains(2, state.CompareIds);
        Assert.Contains("until reload", result.Message);
    }

    private sealed class BrowserStorage : IJSRuntime
    {
        public int[] Ids { get; set; } = [];
        public bool FailWrite { get; set; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args)
        {
            if (identifier == "dwellioComparison.read") return ValueTask.FromResult((TValue)(object)Ids);
            if (!FailWrite) Ids = (int[])args![0]!;
            return ValueTask.FromResult((TValue)(object)!FailWrite);
        }
    }
}
