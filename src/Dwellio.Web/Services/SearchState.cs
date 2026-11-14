using Dwellio.Core.Models;
using Microsoft.JSInterop;

namespace Dwellio.Web.Services;

// One instance per Blazor circuit. Only comparison IDs go into tab storage.
public class SearchState(IJSRuntime? js = null)
{
    private bool comparisonLoaded;
    public Preferences Preferences { get; private set; } = new();
    public bool Loaded { get; private set; }
    public HashSet<int> CompareIds { get; } = [];
    public void Update(Preferences preferences) { Preferences = preferences; Loaded = true; }

    public async Task LoadComparisonAsync()
    {
        if (comparisonLoaded || js is null) return;
        try
        {
            var ids = await js.InvokeAsync<int[]>("dwellioComparison.read");
            CompareIds.Clear();
            foreach (var id in ids.Where(id => id > 0).Distinct().Take(3)) CompareIds.Add(id);
            comparisonLoaded = true;
        }
        catch (JSException) { comparisonLoaded = true; }
    }

    public OperationResult ToggleCompare(int id)
    {
        if (id <= 0) return OperationResult.Fail("Invalid apartment.");
        if (CompareIds.Remove(id)) return OperationResult.Ok("Removed from comparison");
        if (CompareIds.Count >= 3) return OperationResult.Fail("You can compare up to three homes.");
        CompareIds.Add(id);
        return OperationResult.Ok("Added to comparison");
    }

    public async Task<OperationResult> ToggleCompareAsync(int id)
    {
        await LoadComparisonAsync();
        var result = ToggleCompare(id);
        if (!result.Success || js is null) return result;
        try
        {
            var saved = await js.InvokeAsync<bool>("dwellioComparison.write", new object[] { CompareIds.ToArray() });
            if (!saved) return OperationResult.Ok(result.Message + ". Browser storage unavailable; selection lasts until reload.");
        }
        catch (JSException)
        {
            return OperationResult.Ok(result.Message + ". Browser storage unavailable; selection lasts until reload.");
        }
        return result;
    }
}
