using Dwellio.Core.Models;
namespace Dwellio.Web.Services;
// Scoped to one Blazor circuit: never shared across users.
public class SearchState
{
    public Preferences Preferences { get; private set; } = new();
    public bool Loaded { get; private set; }
    public HashSet<int> CompareIds { get; } = [];
    public void Update(Preferences preferences) { Preferences = preferences; Loaded = true; }
    public OperationResult ToggleCompare(int id)
    {
        if (CompareIds.Remove(id)) return OperationResult.Ok("Removed from comparison");
        if (CompareIds.Count >= 3) return OperationResult.Fail("You can compare up to three homes.");
        CompareIds.Add(id); return OperationResult.Ok("Added to comparison");
    }
}
