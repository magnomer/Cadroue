using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LMerge
{
    public static int LMergeDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<LWorkGroup> lMergeGroups,
        LPreset lExportSpecificState,
        Guid lMergeRelayTarget = default,
        Guid lMergeRelaySource = default,
        IReadOnlyDictionary<string, Guid>? lMergeRelays = null)
    {
        string lMergeTab = PControlBar.LTabset.LTabsetTitleRead(lMergeRelaySource);
        IReadOnlyList<LWorkItem> lMergeItems = Cadroue.Application.LMerge.LMergeItemsCreate(
            lWorkPriority,
            lMergeGroups,
            lExportSpecificState.LPresetOutputCreate(),
            lMergeTab,
            lMergeMessage => LTraceLog.LTraceInfoRecord(lMergeMessage),
            lMergeMessage => LTraceLog.LTraceErrorRecord(lMergeMessage),
            lMergeRelays);
        if (lMergeItems.Count == 0)
        {
            return 0;
        }

        int lMergeAdded = PProgram.LScheduleCurrent.LScheduleAdd(
            lMergeItems, lMergeRelayTarget, lMergeRelaySource);
        LTraceLog.LTraceInfoRecord($"Merge queued {lMergeAdded} group(s) at {lWorkPriority}");
        return lMergeAdded;
    }
}
