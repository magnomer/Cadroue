using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LConsoleStation
{
    public const int LConsoleStepBack = -1;
    public const int LConsoleStepForward = 1;
    private const string LConsoleInternalLabel = "Background worklist";

    private readonly LScheduleContract lSchedule;
    private readonly LDepotWatch lDepotWatch = new();
    private LStation? lStation;

    public LConsoleStation(LScheduleContract lScheduleOwner)
    {
        lSchedule = lScheduleOwner;
        lDepotWatch.LDepotChange += LConsoleWatchHandle;
    }

    public event Action? LConsoleWatchChange;

    public event Action? LConsoleStationChange;

    public bool LConsoleAutoActive => LConsoleStationRead().LStationAutoActive;

    public bool LConsoleRunning => LConsoleStationRead().LStationRunner.LRunnerRunning;

    public bool LConsoleSuspended => LConsoleStationRead().LStationRunner.LRunnerSuspended;

    public bool LConsoleSwitchShown => LStation.LStationBoardRead().Length > 1;

    public void LConsoleWatchStart() => lDepotWatch.LDepotWatchStart();

    public void LConsoleWatchStop()
    {
        lDepotWatch.LDepotChange -= LConsoleWatchHandle;
        lDepotWatch.Dispose();
    }

    private void LConsoleWatchHandle() => LConsoleWatchChange?.Invoke();

    public LStation LConsoleStationRead()
    {
        LStation[] lBoard = LStation.LStationBoardRead();
        if (lStation is null || !lBoard.Contains(lStation))
        {
            lStation = lBoard[0];
        }

        return lStation;
    }

    public bool LConsoleStationSet(LStation lStationNext)
    {
        if (ReferenceEquals(lStation, lStationNext))
        {
            return false;
        }

        lStation = lStationNext;
        LConsoleStationChange?.Invoke();
        return true;
    }

    public void LConsoleStationMove(int lStep)
    {
        LStation[] lBoard = LStation.LStationBoardRead();
        if (lBoard.Length <= 1)
        {
            return;
        }

        lStation = lBoard[LConsoleIndexResolve(Array.IndexOf(lBoard, LConsoleStationRead()), lStep, lBoard.Length)];
        LConsoleStationChange?.Invoke();
    }

    public static int LConsoleIndexResolve(int lIndex, int lStep, int lCount) =>
        ((lIndex + lStep) % lCount + lCount) % lCount;

    public bool LConsoleAutoSet(bool lAutoActive)
    {
        LStation lCurrent = LConsoleStationRead();
        if (lCurrent.LStationAutoActive == lAutoActive)
        {
            return false;
        }

        lCurrent.LStationAutoActive = lAutoActive;
        LPreference.LPreferenceAutoSet(lAutoActive);
        return true;
    }

    public bool LConsoleOwnerCheck(LWorkItem lWorkItem) =>
        LConsoleStationRead().LStationRunner.LRunnerOwnerCheck(lWorkItem);

    public LWorkItem[] LConsoleRunningRead() => lSchedule.LScheduleRecords
        .Where(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning
            && LConsoleOwnerCheck(lWorkItem))
        .ToArray();

    public IReadOnlyList<LWorkItem> LConsoleSelectionRead() => LConsoleStationRead().LStationSelectionRead();

    public string LConsoleLabelFormat()
    {
        LStation[] lBoard = LStation.LStationBoardRead();
        LStation lCurrent = LConsoleStationRead();
        string lLabel = string.Equals(lCurrent.LStationLabel, LConsoleInternalLabel, StringComparison.Ordinal)
            ? LLocalization.LLocalizationTextRead("Console.Station.Background")
            : lCurrent.LStationLabel;
        return lBoard.Length > 1
            ? LLocalization.LLocalizationFormat(
                "Console.Station.Numbered", lLabel, Array.IndexOf(lBoard, lCurrent) + 1, lBoard.Length)
            : lLabel;
    }

    public void LConsoleStart()
    {
        LRunner lRunner = LConsoleStationRead().LStationRunner;
        LConsoleRunnerApply(lRunner);
        lRunner.LRunnerStart();
    }

    public void LConsolePause() => LConsoleStationRead().LStationRunner.LRunnerPause();

    public void LConsoleStop()
    {
        LStation lCurrent = LConsoleStationRead();
        lCurrent.LStationAutoActive = false;
        _ = lCurrent.LStationRunner.LRunnerCancel();
    }

    public void LConsoleJobsCancel(IReadOnlyList<LWorkItem> lWorkItems)
    {
        LStation lCurrent = LConsoleStationRead();
        lCurrent.LStationAutoActive = false;
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            lCurrent.LStationRunner.LRunnerJobCancel(lWorkItem.LWorkId);
        }
    }

    public static Task LConsoleBoardCancel()
    {
        var lDraining = new List<Task>();
        foreach (LStation lBoardStation in LStation.LStationBoardRead())
        {
            lBoardStation.LStationAutoActive = false;
            lDraining.Add(lBoardStation.LStationRunner.LRunnerCancel());
        }

        return Task.WhenAll(lDraining);
    }

    private static void LConsoleRunnerApply(LRunner lRunner)
    {
        LPreferenceState lPreferenceState = LPreference.LPreferenceStateCurrent;
        lRunner.LRunnerProgramPath = LRenderer.LRendererProgramCurrent;
        lRunner.LRunnerFailurePaused = lPreferenceState.LPreferenceFailurePaused;
        lRunner.LRunnerRetryAllowed = lPreferenceState.LPreferenceRetryAllowed;
        lRunner.LRunnerRetryMaximum = (int)lPreferenceState.LPreferenceRetryMaximum;
    }
}
