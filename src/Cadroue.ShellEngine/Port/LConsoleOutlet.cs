using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The console's engine side over the station board and the shared schedule. A station handle that is null or left
// the board resolves to the board's first station. The host hands in the renderer's program path, which a runner
// takes with the retry preferences before each start.
public sealed class LConsoleOutlet : LConsolePort
{
    private const string LConsoleInternalLabel = "Background worklist";

    private readonly Func<string> _lConsoleOutletProgram;

    public LConsoleOutlet(Func<string> lProgram)
    {
        ArgumentNullException.ThrowIfNull(lProgram);
        _lConsoleOutletProgram = lProgram;
    }

    private static LScheduleContract LConsoleSchedule => LStation.LStationSchedule!;

    public Action LConsoleAttach(Action lChanged, Action lDeferred, Action<Predicate<LStation>, double> lProgressed)
    {
        LScheduleContract lSchedule = LConsoleSchedule;
        Action<LScheduleContract> lScheduleHandle = _ => lChanged();
        Action<LWorkItem, LScheduleNotice> lItemHandle =
            (lWorkItem, lNotice) => LConsoleItemRaise(lWorkItem, lNotice, lDeferred, lProgressed);
        lSchedule.LScheduleChanged += lScheduleHandle;
        lSchedule.LScheduleItemChanged += lItemHandle;
        LStation.LStationChanged += lChanged;
        return () =>
        {
            lSchedule.LScheduleChanged -= lScheduleHandle;
            lSchedule.LScheduleItemChanged -= lItemHandle;
            LStation.LStationChanged -= lChanged;
        };
    }

    private static void LConsoleItemRaise(
        LWorkItem lWorkItem, LScheduleNotice lNotice, Action lDeferred, Action<Predicate<LStation>, double> lProgressed)
    {
        if (lNotice != LScheduleNotice.LScheduleNoticeProgress)
        {
            lDeferred();
            return;
        }

        lProgressed(lStation => lStation.LStationRunner.LRunnerOwnerCheck(lWorkItem), lWorkItem.LWorkProgress);
    }

    public void LConsoleLoad() => LConsoleSchedule.LScheduleLoad();

    public LConsoleAnswer LConsoleRead(LStation? lStation)
    {
        LStation lCurrent = LConsoleStationResolve(lStation);
        LScheduleContract lSchedule = LConsoleSchedule;
        LWorkItem[] lRunningItems = LConsoleRunningRead(lCurrent, lSchedule);
        LWorkItem? lFirst = lRunningItems.FirstOrDefault();
        IReadOnlyList<LWorkItem> lRecords = lSchedule.LScheduleRecords;
        LStation[] lBoard = LStation.LStationBoardRead();
        return new LConsoleAnswer(
            lCurrent,
            lCurrent.LStationRunner.LRunnerRunning,
            lCurrent.LStationRunner.LRunnerProcess.LRunnerProcessSuspended,
            lCurrent.LStationAutoActive,
            lRecords.Count,
            lSchedule.LScheduleDoneCount,
            lSchedule.LSchedulePendingRead().Count,
            lRunningItems.Length,
            lRunningItems.Length == 0 ? 0 : lRunningItems.Average(lWorkItem => lWorkItem.LWorkProgress),
            lFirst?.LWorkOutputName ?? string.Empty,
            lFirst?.LWorkStageCurrent.ToString() ?? string.Empty,
            lFirst?.LWorkProgress ?? 0,
            lCurrent.LStationSelectionRead()
                .Any(lWorkItem => lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning),
            lRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone),
            lRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning),
            lCurrent.LStationLabel,
            string.Equals(lCurrent.LStationLabel, LConsoleInternalLabel, StringComparison.Ordinal),
            Array.IndexOf(lBoard, lCurrent) + 1,
            lBoard.Length);
    }

    private static LWorkItem[] LConsoleRunningRead(LStation lStation, LScheduleContract lSchedule) =>
        lSchedule.LScheduleRecords
            .Where(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning
                && lStation.LStationRunner.LRunnerOwnerCheck(lWorkItem))
            .ToArray();

    public LStation LConsoleStationResolve(LStation? lStation)
    {
        LStation[] lBoard = LStation.LStationBoardRead();
        return lStation is null || !lBoard.Contains(lStation) ? lBoard[0] : lStation;
    }

    public LStation? LConsoleStationMove(LStation? lStation, int lStep)
    {
        LStation[] lBoard = LStation.LStationBoardRead();
        if (lBoard.Length <= 1)
        {
            return null;
        }

        int lIndex = Array.IndexOf(lBoard, LConsoleStationResolve(lStation));
        return lBoard[LConsoleIndexResolve(lIndex, lStep, lBoard.Length)];
    }

    public static int LConsoleIndexResolve(int lIndex, int lStep, int lCount) =>
        ((lIndex + lStep) % lCount + lCount) % lCount;

    public LStation LConsoleStart(LStation? lStation)
    {
        LStation lCurrent = LConsoleStationResolve(lStation);
        LRunner lRunner = lCurrent.LStationRunner;
        LConsoleRunnerApply(lRunner);
        lRunner.LRunnerStart();
        return lCurrent;
    }

    private void LConsoleRunnerApply(LRunner lRunner)
    {
        LPreferenceState lPreferenceState = LPreference.LPreferenceStateCurrent;
        lRunner.LRunnerProgramPath = _lConsoleOutletProgram();
        lRunner.LRunnerFailurePaused = lPreferenceState.LPreferenceFailurePaused;
        lRunner.LRunnerRetryAllowed = lPreferenceState.LPreferenceRetryAllowed;
        lRunner.LRunnerRetryMaximum = (int)lPreferenceState.LPreferenceRetryMaximum;
    }

    public LStation LConsolePause(LStation? lStation)
    {
        LStation lCurrent = LConsoleStationResolve(lStation);
        lCurrent.LStationRunner.LRunnerPause();
        return lCurrent;
    }

    public LStation LConsoleStop(LStation? lStation)
    {
        LStation lCurrent = LConsoleStationResolve(lStation);
        lCurrent.LStationAutoActive = false;
        _ = lCurrent.LStationRunner.LRunnerCancel();
        return lCurrent;
    }

    public (LStation LConsoleStation, bool LConsoleChanged) LConsoleAutoSave(LStation? lStation, bool lAuto)
    {
        LStation lCurrent = LConsoleStationResolve(lStation);
        if (lCurrent.LStationAutoActive == lAuto)
        {
            return (lCurrent, false);
        }

        lCurrent.LStationAutoActive = lAuto;
        LPreference.LPreferenceAutoSet(lAuto);
        return (lCurrent, true);
    }

    public LStation LConsoleCancel(LStation? lStation, Func<int, string, bool> lConfirm)
    {
        ArgumentNullException.ThrowIfNull(lConfirm);
        LStation lCurrent = LConsoleStationResolve(lStation);
        LWorkItem[] lRunningItems = LConsoleRunningRead(lCurrent, LConsoleSchedule);
        if (lRunningItems.Length == 0
            || !lConfirm(lRunningItems.Length, lRunningItems.FirstOrDefault()?.LWorkOutputName ?? string.Empty))
        {
            return lCurrent;
        }

        lCurrent.LStationAutoActive = false;
        foreach (LWorkItem lWorkItem in lRunningItems)
        {
            lCurrent.LStationRunner.LRunnerJobCancel(lWorkItem.LWorkId);
        }

        return lCurrent;
    }

    public (LStation LConsoleStation, int LConsoleTotal, int LConsoleHeld, int LConsoleBlocked) LConsoleRemove(
        LStation? lStation)
    {
        LStation lCurrent = LConsoleStationResolve(lStation);
        LScheduleContract lSchedule = LConsoleSchedule;
        IReadOnlyList<Guid> lRemovable = lSchedule.LScheduleRemovableRead(
            lCurrent.LStationSelectionRead().Select(lWorkItem => lWorkItem.LWorkId));
        if (lRemovable.Count == 0)
        {
            return (lCurrent, 0, 0, 0);
        }

        IReadOnlyDictionary<Guid, LScheduleRemoval> lOutcomes = lSchedule.LScheduleBatchRemove(lRemovable);
        return (
            lCurrent,
            lOutcomes.Count,
            lOutcomes.Values.Count(lOutcome => lOutcome == LScheduleRemoval.LScheduleRemovalHeld),
            lOutcomes.Values.Count(lOutcome => lOutcome == LScheduleRemoval.LScheduleRemovalBlocked));
    }

    public void LConsoleDoneClear() => LConsoleSchedule.LScheduleDoneClear();

    public bool LConsoleActiveCheck() => LStation.LStationActiveCheck();

    public async Task LConsoleAllClear()
    {
        await LConsoleBoardCancel();
        LMessenger.LMessengerMeasureCancel();
        LConsoleSchedule.LScheduleAllClear();
    }

    private static Task LConsoleBoardCancel()
    {
        var lDraining = new List<Task>();
        foreach (LStation lBoardStation in LStation.LStationBoardRead())
        {
            lBoardStation.LStationAutoActive = false;
            lDraining.Add(lBoardStation.LStationRunner.LRunnerCancel());
        }

        return Task.WhenAll(lDraining);
    }

    public double LConsolePeriodRead() => LEncode.LEncodeStatsPeriod;
}
