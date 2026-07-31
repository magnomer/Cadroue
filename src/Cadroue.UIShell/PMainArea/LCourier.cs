using System.IO;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PControlBar;

namespace Cadroue.UIShell.PMainArea;

public sealed record LCourierOption(Guid LCourierTabId, string LCourierTabTitle, ImageSource? LCourierTabIcon);

public static class LCourier
{
    private static readonly Dictionary<Guid, Guid> lCourierTargets = new();
    private static readonly HashSet<Guid> lCourierDelivered = new();
    private static readonly HashSet<Guid> lCourierClearedTargets = new();
    private static bool lCourierWatching;

    public static void LCourierStart()
    {
        if (lCourierWatching)
        {
            return;
        }

        lCourierWatching = true;
        LSchedule.LScheduleCurrent.LScheduleChange += LCourierScheduleHandle;
    }

    public static void LCourierAttach(Guid lCourierSourceTab, PAction pCourierAction)
    {
        LCourierStart();
        pCourierAction.PActionRelaySource = () => LCourierOptionsRead(lCourierSourceTab);
        pCourierAction.PActionRelayChange += lCourierTarget =>
        {
            LCourierTargetSet(lCourierSourceTab, lCourierTarget);
            pCourierAction.PActionRelayApply(LCourierTargetRead(lCourierSourceTab));
        };
        pCourierAction.PActionRelayApply(LCourierTargetRead(lCourierSourceTab));
    }

    public static void LCourierFaceUpdate()
    {
        if (LTabset.LTabsetCurrent is not { } lCourierTabset)
        {
            return;
        }

        foreach (PTabRecord pTabRecord in lCourierTabset.PTabsetRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(
                LCourierTargetRead(pTabRecord.PTabId));
        }
    }

    public static Guid LCourierTargetRead(Guid lCourierSourceTab) =>
        lCourierTargets.TryGetValue(lCourierSourceTab, out Guid lCourierTarget) ? lCourierTarget : Guid.Empty;

    public static void LCourierTargetSet(Guid lCourierSourceTab, Guid lCourierTarget)
    {
        if (lCourierTarget == Guid.Empty)
        {
            lCourierTargets.Remove(lCourierSourceTab);
            return;
        }

        if (lCourierTarget == lCourierSourceTab)
        {
            LTraceLog.LTraceErrorRecord("Relay target refused: a tab cannot relay into itself");
            return;
        }

        lCourierTargets[lCourierSourceTab] = lCourierTarget;
    }

    public static void LCourierTabRemove(Guid lCourierTabId)
    {
        lCourierTargets.Remove(lCourierTabId);
        lCourierClearedTargets.Remove(lCourierTabId);
        foreach (Guid lCourierSourceTab in lCourierTargets
            .Where(lCourierEntry => lCourierEntry.Value == lCourierTabId)
            .Select(lCourierEntry => lCourierEntry.Key)
            .ToArray())
        {
            lCourierTargets.Remove(lCourierSourceTab);
        }
    }

    public static IReadOnlyList<LCourierOption> LCourierOptionsRead(Guid lCourierSourceTab)
    {
        if (LTabset.LTabsetCurrent is not { } lCourierTabset)
        {
            return Array.Empty<LCourierOption>();
        }

        var lCourierOptions = new List<LCourierOption>();
        foreach (PTabRecord pTabRecord in lCourierTabset.PTabsetRecords)
        {
            if (pTabRecord.PTabId == lCourierSourceTab
                || pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            lCourierOptions.Add(new LCourierOption(
                pTabRecord.PTabId, pTabRecord.PTabTitle, pTabRecord.PTabIconSource));
        }

        return lCourierOptions;
    }

    public static IReadOnlyList<int> LCourierSlotsRead(IReadOnlyList<PTabRecord> pCourierTabRecords)
    {
        var lCourierSlots = new List<int>(pCourierTabRecords.Count);
        foreach (PTabRecord pTabRecord in pCourierTabRecords)
        {
            Guid lCourierTarget = LCourierTargetRead(pTabRecord.PTabId);
            int lCourierSlot = -1;
            for (int lCourierIndex = 0; lCourierIndex < pCourierTabRecords.Count; lCourierIndex++)
            {
                if (pCourierTabRecords[lCourierIndex].PTabId == lCourierTarget)
                {
                    lCourierSlot = lCourierIndex;
                    break;
                }
            }

            lCourierSlots.Add(lCourierSlot);
        }

        return lCourierSlots;
    }

    public static void LCourierSlotsApply(
        IReadOnlyList<PTabRecord> pCourierTabRecords,
        IReadOnlyList<int> lCourierSlots)
    {
        for (int lCourierIndex = 0; lCourierIndex < pCourierTabRecords.Count; lCourierIndex++)
        {
            if (lCourierIndex >= lCourierSlots.Count)
            {
                break;
            }

            int lCourierSlot = lCourierSlots[lCourierIndex];
            if (lCourierSlot < 0 || lCourierSlot >= pCourierTabRecords.Count || lCourierSlot == lCourierIndex)
            {
                continue;
            }

            PTabRecord pCourierSource = pCourierTabRecords[lCourierIndex];
            PTabRecord pCourierTarget = pCourierTabRecords[lCourierSlot];
            if (pCourierTarget.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            LCourierTargetSet(pCourierSource.PTabId, pCourierTarget.PTabId);
            pCourierSource.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(pCourierTarget.PTabId);
        }
    }

    private static void LCourierScheduleHandle(LSchedule lCourierSchedule) => LCourierDispatch(lCourierSchedule);

    private static void LCourierDispatch(LSchedule lCourierSchedule)
    {
        foreach (LWorkItem lWorkItem in lCourierSchedule.LScheduleRecords.ToArray())
        {
            if (lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateDone
                || lWorkItem.LWorkRelayTarget == Guid.Empty
                || lWorkItem.LWorkOwnerProcess != Environment.ProcessId
                || lCourierDelivered.Contains(lWorkItem.LWorkId))
            {
                continue;
            }

            lCourierDelivered.Add(lWorkItem.LWorkId);
            LCourierOutputAdd(lWorkItem);
        }

        LCourierBatchUpdate(lCourierSchedule);
    }

    private static void LCourierBatchUpdate(LSchedule lCourierSchedule)
    {
        foreach (Guid lCourierTarget in lCourierClearedTargets.ToArray())
        {
            bool lCourierPending = lCourierSchedule.LScheduleRecords.Any(lWorkItem =>
                lWorkItem.LWorkRelayTarget == lCourierTarget
                && lWorkItem.LWorkStateCurrent is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning);
            if (!lCourierPending)
            {
                lCourierClearedTargets.Remove(lCourierTarget);
            }
        }
    }

    private static void LCourierOutputAdd(LWorkItem lWorkItem)
    {
        if (string.IsNullOrWhiteSpace(lWorkItem.LWorkOutputPath) || !File.Exists(lWorkItem.LWorkOutputPath))
        {
            LTraceLog.LTraceErrorRecord($"Relay skipped '{lWorkItem.LWorkOutputName}': the output file is missing");
            return;
        }

        if (LCourierTabFind(lWorkItem.LWorkRelayTarget) is not { } pCourierTarget
            || pCourierTarget.PTabWorkspace.PWorkspaceSurface.PTabList is not { } pCourierList)
        {
            LTraceLog.LTraceInfoRecord($"Relay skipped '{lWorkItem.LWorkOutputName}': the destination tab is gone");
            return;
        }

        if (PProgram.LPreferenceStateCurrent.LPreferenceRelayEmpty
            && lCourierClearedTargets.Add(lWorkItem.LWorkRelayTarget))
        {
            pCourierList.PListClear();
            LTraceLog.LTraceInfoRecord($"Relay cleared the files in tab '{pCourierTarget.PTabTitle}' before delivery");
        }

        int lCourierAdded = pCourierList.PListPathsAdd(new[] { lWorkItem.LWorkOutputPath });
        LTraceLog.LTraceInfoRecord(
            lCourierAdded > 0
                ? $"Relay added '{lWorkItem.LWorkOutputName}' to tab '{pCourierTarget.PTabTitle}'"
                : $"Relay left '{lWorkItem.LWorkOutputName}' out of tab '{pCourierTarget.PTabTitle}': already listed");
    }

    private static PTabRecord? LCourierTabFind(Guid lCourierTabId) =>
        LTabset.LTabsetCurrent?.PTabsetRecords.FirstOrDefault(pTabRecord => pTabRecord.PTabId == lCourierTabId);
}
