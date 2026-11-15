using Dwellio.Web.Data;
using Dwellio.Core.Models;
using Microsoft.JSInterop;

namespace Dwellio.Web.Services;

// Source URLs are stable identifiers; only known examples may enter the shortlist.
public class MarketShortlist(IJSRuntime js)
{
    public HashSet<string> Selected { get; } = [];
    private bool loaded;
    public async Task LoadAsync()
    {
        if (loaded) return;
        try
        {
            var urls = await js.InvokeAsync<string[]>("dwellioMarket.read");
            foreach (var url in urls.Where(IsKnown).Distinct().Take(3)) Selected.Add(url);
            loaded = true;
        }
        catch (JSException) { loaded = true; }
    }
    public static bool IsKnown(string url) => MarketReferences.All.Any(x => x.SourceUrl == url);
    public OperationResult Toggle(string url)
    {
        if (!IsKnown(url)) return OperationResult.Fail("Listing not found.");
        if (Selected.Remove(url)) return OperationResult.Ok("Removed from source comparison.");
        if (Selected.Count >= 3) return OperationResult.Fail("Compare up to three sourced homes. Remove one first.");
        Selected.Add(url);
        return OperationResult.Ok("Added to source comparison.");
    }
    public async Task<OperationResult> ToggleAsync(string url)
    {
        await LoadAsync();
        var result = Toggle(url);
        if (!result.Success) return result;
        try
        {
            if (await js.InvokeAsync<bool>("dwellioMarket.write", new object[] { Selected.ToArray() })) return result;
        }
        catch (JSException) { }
        return OperationResult.Ok(result.Message + " Browser storage unavailable; selection lasts until reload.");
    }
}
