using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QConsoleStation
{
    public const int QConsoleStepBack = -1;
    public const int QConsoleStepForward = 1;
    private const string QConsoleInternalLabel = "Background worklist";

    private readonly LScheduleContract qSchedule;
    private readonly LDepotWatch qDepotWatch = new();
    private LStation? qStation;

    public QConsoleStation(LScheduleContract lScheduleOwner)
    {
        qSchedule = lScheduleOwner;
        qDepotWatch.LDepotChanged += QConsoleWatchHandle;
    }

    public event Action? QConsoleWatchChanged;

    public event Action? QConsoleStationChanged;

    public bool QConsoleAutoActive => QConsoleStationRead().LStationAutoActive;

    public bool QConsoleRunning => QConsoleStationRead().LStationRunner.LRunnerRunning;

    public bool QConsoleSuspended => QConsoleStationRead().LStationRunner.LRunnerProcess.LRunnerProcessSuspended;

    public bool QConsoleSwitchShown => LStation.LStationBoardRead().Length > 1;

    public void QConsoleWatchStart() => qDepotWatch.LDepotWatchStart();

    public void QConsoleWatchStop()
    {
        qDepotWatch.LDepotChanged -= QConsoleWatchHandle;
        qDepotWatch.Dispose();
    }

    private void QConsoleWatchHandle() => QConsoleWatchChanged?.Invoke();

    public LStation QConsoleStationRead()
    {
        LStation[] lBoard = LStation.LStationBoardRead();
        if (qStation is null || !lBoard.Contains(qStation))
        {
            qStation = lBoard[0];
        }

        return qStation;
    }

    public bool QConsoleStationSet(LStation lStationNext)
    {
        if (ReferenceEquals(qStation, lStationNext))
        {
            return false;
        }

        qStation = lStationNext;
        QConsoleStationChanged?.Invoke();
        return true;
    }

    public void QConsoleStationMove(int lStep)
    {
        LStation[] lBoard = LStation.LStationBoardRead();
        if (lBoard.Length <= 1)
        {
            return;
        }

        qStation = lBoard[QConsoleIndexResolve(Array.IndexOf(lBoard, QConsoleStationRead()), lStep, lBoard.Length)];
        QConsoleStationChanged?.Invoke();
    }

    public static int QConsoleIndexResolve(int lIndex, int lStep, int lCount) =>
        ((lIndex + lStep) % lCount + lCount) % lCount;

    public bool QConsoleAutoSet(bool lAutoActive)
    {
        LStation lCurrent = QConsoleStationRead();
        if (lCurrent.LStationAutoActive == lAutoActive)
        {
            return false;
        }

        lCurrent.LStationAutoActive = lAutoActive;
        LPreference.LPreferenceAutoSet(lAutoActive);
        return true;
    }

    public bool QConsoleOwnerCheck(LWorkItem lWorkItem) =>
        QConsoleStationRead().LStationRunner.LRunnerOwnerCheck(lWorkItem);

    public LWorkItem[] QConsoleRunningRead() => qSchedule.LScheduleRecords
        .Where(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning
            && QConsoleOwnerCheck(lWorkItem))
        .ToArray();

    public IReadOnlyList<LWorkItem> QConsoleSelectionRead() => QConsoleStationRead().LStationSelectionRead();

    public string QConsoleLabelFormat()
    {
        LStation[] lBoard = LStation.LStationBoardRead();
        LStation lCurrent = QConsoleStationRead();
        string lLabel = string.Equals(lCurrent.LStationLabel, QConsoleInternalLabel, StringComparison.Ordinal)
            ? QLocalizationCatalog.QLocalizationTextRead("Console.Station.Background")
            : lCurrent.LStationLabel;
        return lBoard.Length > 1
            ? QLocalizationCatalog.QLocalizationFormat(
                "Console.Station.Numbered", lLabel, Array.IndexOf(lBoard, lCurrent) + 1, lBoard.Length)
            : lLabel;
    }

    public void QConsoleStart()
    {
        LRunner lRunner = QConsoleStationRead().LStationRunner;
        QConsoleRunnerApply(lRunner);
        lRunner.LRunnerStart();
    }

    public void QConsolePause() => QConsoleStationRead().LStationRunner.LRunnerPause();

    public void QConsoleStop()
    {
        LStation lCurrent = QConsoleStationRead();
        lCurrent.LStationAutoActive = false;
        _ = lCurrent.LStationRunner.LRunnerCancel();
    }

    public void QConsoleJobsCancel(IReadOnlyList<LWorkItem> lWorkItems)
    {
        LStation lCurrent = QConsoleStationRead();
        lCurrent.LStationAutoActive = false;
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            lCurrent.LStationRunner.LRunnerJobCancel(lWorkItem.LWorkId);
        }
    }

    public static Task QConsoleBoardCancel()
    {
        var lDraining = new List<Task>();
        foreach (LStation lBoardStation in LStation.LStationBoardRead())
        {
            lBoardStation.LStationAutoActive = false;
            lDraining.Add(lBoardStation.LStationRunner.LRunnerCancel());
        }

        return Task.WhenAll(lDraining);
    }

    private static void QConsoleRunnerApply(LRunner lRunner)
    {
        LPreferenceState lPreferenceState = LPreference.LPreferenceStateCurrent;
        lRunner.LRunnerProgramPath = LRenderer.LRendererProgramCurrent;
        lRunner.LRunnerFailurePaused = lPreferenceState.LPreferenceFailurePaused;
        lRunner.LRunnerRetryAllowed = lPreferenceState.LPreferenceRetryAllowed;
        lRunner.LRunnerRetryMaximum = (int)lPreferenceState.LPreferenceRetryMaximum;
    }
}
