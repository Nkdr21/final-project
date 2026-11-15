using System.Collections.Immutable;
using System.Text.Json;
using Dwellio.Core.Matching;
using Dwellio.Core.Models;
using Microsoft.JSInterop;

namespace Dwellio.Web.Services;

/// <summary>
/// State container for one Blazor circuit (scoped). Holds the user's plan and comparison list,
/// notifies subscribed components through <see cref="Changed"/>, and persists:
///  • plan → browser localStorage (everyone) and the user profile (signed-in users, on request);
///  • comparison → sessionStorage (per tab).
/// State changes go through <see cref="Update"/>, a pure function from old to new preferences.
/// </summary>
public sealed class PlannerState(IJSRuntime? js = null, HousingService? housing = null)
{
    public const string PlanKey = "dwellio.plan.v2";
    public const string CompareKey = "dwellio.compare.v2";
    public const int MaxCompare = 3;

    private Task? loading;

    public Preferences Preferences { get; private set; } = new();
    public ImmutableArray<int> CompareIds { get; private set; } = [];
    public bool Loaded { get; private set; }

    /// <summary>True until the user changes anything — used to show the first-visit welcome.</summary>
    public bool IsFirstVisit { get; private set; } = true;

    public event Action? Changed;

    /// <summary>Loads saved state once per circuit. Safe to call from every page.</summary>
    public Task EnsureLoadedAsync() => loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        Preferences? restored = housing is null ? null : await housing.LoadPreferences();
        restored ??= Deserialize<Preferences>(await ReadAsync(PlanKey, session: false));
        if (restored is not null) { Preferences = HomeMatcher.Sanitize(restored); IsFirstVisit = false; }
        var ids = Deserialize<int[]>(await ReadAsync(CompareKey, session: true)) ?? [];
        CompareIds = ids.Where(id => id > 0).Distinct().Take(MaxCompare).ToImmutableArray();
        Loaded = true;
        Changed?.Invoke();
    }

    /// <summary>Applies a change. Invalid results are rejected and the previous state is kept.</summary>
    public async Task<OperationResult> Update(Func<Preferences, Preferences> change)
    {
        var next = change(Preferences);
        var valid = HomeMatcher.Validate(next);
        if (!valid.Success) return valid;
        Preferences = next;
        IsFirstVisit = false;
        Changed?.Invoke();
        var stored = await WriteAsync(PlanKey, JsonSerializer.Serialize(next), session: false);
        return stored ? OperationResult.Ok("Plan updated") : OperationResult.Ok("Plan updated. Browser storage is blocked, so it lasts until you close this tab.");
    }

    public async Task<OperationResult> SaveToProfileAsync() =>
        housing is null ? OperationResult.Fail("Profile storage unavailable.") : await housing.SavePreferences(Preferences);

    public async Task<OperationResult> ToggleCompareAsync(int id)
    {
        if (id <= 0) return OperationResult.Fail("Invalid home.");
        string message;
        if (CompareIds.Contains(id)) { CompareIds = CompareIds.Remove(id); message = "Removed from comparison"; }
        else if (CompareIds.Length >= MaxCompare) return OperationResult.Fail($"You can compare up to {MaxCompare} homes. Remove one first.");
        else { CompareIds = CompareIds.Add(id); message = "Added to comparison"; }
        Changed?.Invoke();
        var stored = await WriteAsync(CompareKey, JsonSerializer.Serialize(CompareIds), session: true);
        return OperationResult.Ok(stored ? message : message + ". Browser storage is blocked; the list lasts until reload.");
    }

    private async Task<string?> ReadAsync(string key, bool session)
    {
        if (js is null) return null;
        try { return await js.InvokeAsync<string?>("dwellioStore.get", key, session); }
        catch (Exception e) when (e is JSException or InvalidOperationException or TaskCanceledException) { return null; }
    }

    private async Task<bool> WriteAsync(string key, string value, bool session)
    {
        if (js is null) return false;
        try { return await js.InvokeAsync<bool>("dwellioStore.set", key, value, session); }
        catch (Exception e) when (e is JSException or InvalidOperationException or TaskCanceledException) { return false; }
    }

    private static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return default; } // tampered or outdated storage is ignored, not fatal
    }
}
