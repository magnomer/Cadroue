using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LPreferenceStateStore
{
    private const string LPreferenceFileName = "LPreferenceState.json";

    private static LVaultOutcome lPreferenceOutcome = LVaultOutcome.LVaultMissing;

    public static LPreferenceState? LPreferenceStateLoad()
    {
        LVaultResult<LPreferenceState> lPreferenceResult = LVault.LVaultRead<LPreferenceState>(LPreferencePathCreate());
        lPreferenceOutcome = lPreferenceResult.LVaultOutcome;
        return lPreferenceResult.LVaultValue;
    }

    public static bool LPreferenceStateSave(LPreferenceState lPreferenceState)
    {
        if (lPreferenceOutcome == LVaultOutcome.LVaultUnreadable)
        {
            return false;
        }

        return LVault.LVaultSave(LPreferencePathCreate(), lPreferenceState);
    }

    // The subwindow placements the preferences once held, read as walked: an entry whose corner is not a number
    // is skipped; each entry is (key, left, top, width, height).
    public static IEnumerable<(string, double, double, double, double)> LPreferencePlacementRead(
        IReadOnlyList<(string, string)> lEntries)
    {
        string lPath = LPreferencePathCreate();
        if (!File.Exists(lPath))
        {
            yield break;
        }

        using var lDocument = JsonDocument.Parse(File.ReadAllText(lPath));
        JsonElement lPreference = lDocument.RootElement;
        foreach ((string lPrefix, string lKey) in lEntries)
        {
            if (!lPreference.TryGetProperty($"LPreference{lPrefix}Left", out JsonElement lLeft)
                || !lPreference.TryGetProperty($"LPreference{lPrefix}Top", out JsonElement lTop)
                || !lPreference.TryGetProperty($"LPreference{lPrefix}Width", out JsonElement lWidth)
                || !lPreference.TryGetProperty($"LPreference{lPrefix}Height", out JsonElement lHeight)
                || lLeft.ValueKind != JsonValueKind.Number
                || lTop.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            yield return (lKey, lLeft.GetDouble(), lTop.GetDouble(), lWidth.GetDouble(), lHeight.GetDouble());
        }
    }

    private static string LPreferencePathCreate() => Path.Combine(LDepot.LDepotHomeRead(), LPreferenceFileName);
}
