using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LPlacementRecord
{
    public double LPlacementLeft { get; set; }

    public double LPlacementTop { get; set; }

    public double LPlacementWidth { get; set; }

    public double LPlacementHeight { get; set; }
}

public static class LPlacement
{
    private const string LPlacementFileName = "placement.json";

    private static readonly object lPlacementLock = new();
    private static readonly HashSet<string> lPlacementUnreadable = new(StringComparer.OrdinalIgnoreCase);

    public static LPlacementRecord? LPlacementRead(string lPlacementKey)
    {
        lock (lPlacementLock)
        {
            return LPlacementAllRead().LVaultValue?.TryGetValue(lPlacementKey, out LPlacementRecord? lPlacement) == true
                ? lPlacement
                : null;
        }
    }

    public static bool LPlacementSave(string lPlacementKey, double lLeft, double lTop, double lWidth, double lHeight)
    {
        if (double.IsNaN(lLeft) || double.IsNaN(lTop) || lWidth <= 0 || lHeight <= 0)
        {
            return false;
        }

        lock (lPlacementLock)
        {
            LVaultResult<Dictionary<string, LPlacementRecord>> lPlacementResult = LPlacementAllRead();
            if (lPlacementResult.LVaultOutcome == LVaultOutcome.LVaultUnreadable)
            {
                LTraceLog.LTraceErrorRecord($"Subwindow placement not saved because its catalogue is unreadable: {lPlacementKey}");
                return false;
            }

            Dictionary<string, LPlacementRecord> lPlacements = lPlacementResult.LVaultValue
                ?? new Dictionary<string, LPlacementRecord>(StringComparer.Ordinal);
            lPlacements[lPlacementKey] = new LPlacementRecord
            {
                LPlacementLeft = lLeft,
                LPlacementTop = lTop,
                LPlacementWidth = lWidth,
                LPlacementHeight = lHeight
            };

            if (!LVault.LVaultSave(LPlacementPathRead(), lPlacements))
            {
                LTraceLog.LTraceErrorRecord($"Subwindow placement could not be saved: {lPlacementKey}");
                return false;
            }

            return true;
        }
    }

    public static bool LPlacementExist(string lPlacementKey) => LPlacementRead(lPlacementKey) is not null;

    public static void LPlacementImport(
        string lPreferencePath,
        IReadOnlyList<(string LPlacementPrefix, string LPlacementKey)> lPlacementEntries)
    {
        try
        {
            if (!File.Exists(lPreferencePath))
            {
                return;
            }

            using var lPreferenceDocument = JsonDocument.Parse(File.ReadAllText(lPreferencePath));
            JsonElement lPreferenceRoot = lPreferenceDocument.RootElement;
            foreach ((string lPlacementPrefix, string lPlacementKey) in lPlacementEntries)
            {
                LPlacementEntryImport(lPreferenceRoot, lPlacementPrefix, lPlacementKey);
            }
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Subwindow placement could not be carried from preferences", lException);
        }
    }

    private static void LPlacementEntryImport(JsonElement lPreferenceRoot, string lPlacementPrefix, string lPlacementKey)
    {
        if (LPlacementExist(lPlacementKey)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPlacementPrefix}Left", out JsonElement lLeft)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPlacementPrefix}Top", out JsonElement lTop)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPlacementPrefix}Width", out JsonElement lWidth)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPlacementPrefix}Height", out JsonElement lHeight)
            || lLeft.ValueKind != JsonValueKind.Number
            || lTop.ValueKind != JsonValueKind.Number)
        {
            return;
        }

        if (LPlacementSave(lPlacementKey, lLeft.GetDouble(), lTop.GetDouble(), lWidth.GetDouble(), lHeight.GetDouble()))
        {
            LTraceLog.LTraceInfoRecord($"Subwindow placement carried from preferences: {lPlacementKey}");
        }
    }

    private static string LPlacementPathRead() => Path.Combine(LDepot.LDepotRootRead(), LPlacementFileName);

    private static LVaultResult<Dictionary<string, LPlacementRecord>> LPlacementAllRead()
    {
        string lPlacementPath = LPlacementPathRead();
        if (lPlacementUnreadable.Contains(lPlacementPath))
        {
            return new LVaultResult<Dictionary<string, LPlacementRecord>>(LVaultOutcome.LVaultUnreadable, null);
        }

        LVaultResult<Dictionary<string, LPlacementRecord>> lPlacementResult =
            LVault.LVaultRead<Dictionary<string, LPlacementRecord>>(lPlacementPath);
        if (lPlacementResult.LVaultOutcome == LVaultOutcome.LVaultUnreadable)
        {
            lPlacementUnreadable.Add(lPlacementPath);
            LTraceLog.LTraceErrorRecord($"Subwindow placement catalogue is unreadable: {lPlacementPath}");
        }

        return lPlacementResult;
    }
}
