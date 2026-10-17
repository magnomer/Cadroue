using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LBinding
{
    public static List<LBindingEntry> LBindingCurrent { get; private set; } = LBindingNormalize(null);

    public static void LBindingLoad() =>
        LBindingCurrent = LBindingNormalize(LBindingStore.LBindingLoad());

    public static void LBindingSet(List<LBindingEntry> lBindingRecords)
    {
        List<LBindingEntry> lBindingPrevious = LBindingCurrent;
        LBindingCurrent = LBindingNormalize(lBindingRecords);
        LBindingStore.LBindingSave(LBindingCurrent);
        LBindingChangeRecord(lBindingPrevious, LBindingCurrent);
    }

    private static void LBindingChangeRecord(List<LBindingEntry> lBindingPrevious, List<LBindingEntry> lBindingNext)
    {
        int lBindingChanged = lBindingNext.Count(lBindingEntry =>
            !string.Equals(
                LBindingGestureRead(lBindingPrevious, lBindingEntry.LBindingRecordToken),
                lBindingEntry.LBindingRecordGesture,
                StringComparison.Ordinal));
        if (lBindingChanged > 0)
        {
            LTraceLog.LTraceInfoRecord($"Shortcuts: {lBindingChanged} binding(s) changed");
        }
    }

    public static string LBindingGestureFormat(
        string lBindingKeyName,
        bool lBindingCtrl,
        bool lBindingAlt,
        bool lBindingShift,
        bool lBindingWin)
    {
        if (string.IsNullOrEmpty(lBindingKeyName))
        {
            return string.Empty;
        }

        string lBindingPrefix = string.Empty;
        if (lBindingCtrl) lBindingPrefix += "Ctrl+";
        if (lBindingAlt) lBindingPrefix += "Alt+";
        if (lBindingShift) lBindingPrefix += "Shift+";
        if (lBindingWin) lBindingPrefix += "Win+";
        return lBindingPrefix + lBindingKeyName;
    }

    public static List<LBindingEntry> LBindingNormalize(List<LBindingEntry>? lBindingRecords)
    {
        var lBindingResult = new List<LBindingEntry>();
        var lBindingTaken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (LBindingCommand lBindingCommand in LBindingCatalog.LBindingCatalogRead())
        {
            LBindingEntry? lBindingStored = lBindingRecords?.FirstOrDefault(lBindingEntry =>
                string.Equals(
                    lBindingEntry.LBindingRecordToken,
                    lBindingCommand.LBindingCommandToken,
                    StringComparison.Ordinal));

            string lBindingGesture = (lBindingStored?.LBindingRecordGesture
                ?? lBindingCommand.LBindingCommandGesture
                ?? string.Empty).Trim();

            if (lBindingGesture.Length > 0 && !lBindingTaken.Add(lBindingGesture))
            {
                lBindingGesture = string.Empty;
            }

            lBindingResult.Add(new LBindingEntry
            {
                LBindingRecordToken = lBindingCommand.LBindingCommandToken,
                LBindingRecordGesture = lBindingGesture
            });
        }

        return lBindingResult;
    }

    public static string LBindingGestureRead(List<LBindingEntry>? lBindingRecords, string lBindingToken) =>
        lBindingRecords?
            .FirstOrDefault(lBindingEntry =>
                string.Equals(lBindingEntry.LBindingRecordToken, lBindingToken, StringComparison.Ordinal))
            ?.LBindingRecordGesture ?? string.Empty;

    public static string? LBindingTokenFind(List<LBindingEntry>? lBindingRecords, string lBindingGesture)
    {
        if (string.IsNullOrEmpty(lBindingGesture) || lBindingRecords is null)
        {
            return null;
        }

        return lBindingRecords
            .FirstOrDefault(lBindingEntry =>
                string.Equals(lBindingEntry.LBindingRecordGesture, lBindingGesture, StringComparison.OrdinalIgnoreCase))
            ?.LBindingRecordToken;
    }
}
