using Cadroue.Core;
using Cadroue.UIShell.PFlow;
using Cadroue.UIShell.PPanels;
using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PControlBar;

internal sealed record LHistoryEntry(
    IReadOnlyList<LSegment> LHistorySections,
    int? LHistorySectionSelect,
    LPresetRecord LHistoryExport);

internal sealed class LHistory
{
    private readonly List<LHistoryEntry> lHistoryPast = new();
    private readonly List<LHistoryEntry> lHistoryFuture = new();
    private LHistoryEntry? lHistoryPresent;

    internal bool LHistoryApplying { get; set; }

    internal bool LHistoryUndoReady => lHistoryPast.Count > 0;

    internal bool LHistoryRedoReady => lHistoryFuture.Count > 0;

    internal void LHistoryReset(LHistoryEntry lHistoryEntry)
    {
        lHistoryPast.Clear();
        lHistoryFuture.Clear();
        lHistoryPresent = lHistoryEntry;
    }

    internal void LHistoryAdd(LHistoryEntry lHistoryEntry, int lHistoryMaximum)
    {
        if (LHistoryApplying)
        {
            return;
        }

        if (lHistoryPresent is null)
        {
            lHistoryPresent = lHistoryEntry;
            return;
        }

        if (LHistoryMatch(lHistoryPresent, lHistoryEntry))
        {
            return;
        }

        lHistoryPast.Add(lHistoryPresent);
        lHistoryFuture.Clear();
        lHistoryPresent = lHistoryEntry;
        while (lHistoryMaximum > 0 && lHistoryPast.Count > lHistoryMaximum)
        {
            lHistoryPast.RemoveAt(0);
        }
    }

    internal LHistoryEntry? LHistoryUndo()
    {
        if (lHistoryPast.Count == 0 || lHistoryPresent is null)
        {
            return null;
        }

        lHistoryFuture.Add(lHistoryPresent);
        lHistoryPresent = lHistoryPast[^1];
        lHistoryPast.RemoveAt(lHistoryPast.Count - 1);
        return lHistoryPresent;
    }

    internal LHistoryEntry? LHistoryRedo()
    {
        if (lHistoryFuture.Count == 0 || lHistoryPresent is null)
        {
            return null;
        }

        lHistoryPast.Add(lHistoryPresent);
        lHistoryPresent = lHistoryFuture[^1];
        lHistoryFuture.RemoveAt(lHistoryFuture.Count - 1);
        return lHistoryPresent;
    }

    private static bool LHistoryMatch(LHistoryEntry lHistoryFirst, LHistoryEntry lHistorySecond)
    {
        if (lHistoryFirst.LHistorySectionSelect != lHistorySecond.LHistorySectionSelect
            || lHistoryFirst.LHistorySections.Count != lHistorySecond.LHistorySections.Count
            || !lHistoryFirst.LHistoryExport.Equals(lHistorySecond.LHistoryExport))
        {
            return false;
        }

        for (int lHistoryIndex = 0; lHistoryIndex < lHistoryFirst.LHistorySections.Count; lHistoryIndex++)
        {
            if (!lHistoryFirst.LHistorySections[lHistoryIndex].Equals(lHistorySecond.LHistorySections[lHistoryIndex]))
            {
                return false;
            }
        }

        return true;
    }
}
