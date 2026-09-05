using System.Windows;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PDeck;

public sealed partial class PConsole
{
    private bool pConsoleAutoApplying;
    private LStation? pConsoleStation;

    private void PConsoleAutoHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (pConsoleAutoApplying)
        {
            return;
        }

        bool pConsoleAutoResume = pConsoleAutoBox.IsChecked == true;
        PConsoleStationRead().LStationAutoActive = pConsoleAutoResume;
        LPreference.LPreferenceAutoSet(pConsoleAutoResume);
        PConsoleProgressUpdate();
    }

    private LStation PConsoleStationRead()
    {
        LStation[] pBoard = LStation.LStationBoardRead();
        if (pConsoleStation is null || !pBoard.Contains(pConsoleStation))
        {
            pConsoleStation = pBoard[0];
        }

        return pConsoleStation;
    }

    public void PConsoleStationSet(LStation pStation)
    {
        if (ReferenceEquals(pConsoleStation, pStation))
        {
            return;
        }

        pConsoleStation = pStation;
        PConsoleProgressUpdate();
    }

    private void PConsoleStationHandle() => PConsoleProgressUpdate();

    private void PConsoleStationMove(int pStep)
    {
        LStation[] pBoard = LStation.LStationBoardRead();
        if (pBoard.Length <= 1)
        {
            return;
        }

        int pIndex = Array.IndexOf(pBoard, PConsoleStationRead());
        pConsoleStation = pBoard[((pIndex + pStep) % pBoard.Length + pBoard.Length) % pBoard.Length];
        PConsoleProgressUpdate();
    }

    private void PConsolePreviousHandle(object pSender, RoutedEventArgs pArguments) => PConsoleStationMove(-1);

    private void PConsoleNextHandle(object pSender, RoutedEventArgs pArguments) => PConsoleStationMove(1);

    private LWorkItem[] PConsoleRunningRead() => pConsoleSchedule.LScheduleRecords
        .Where(pWorkItem => pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning
            && PConsoleStationRead().LStationRunner.LRunnerOwnerCheck(pWorkItem))
        .ToArray();
}
