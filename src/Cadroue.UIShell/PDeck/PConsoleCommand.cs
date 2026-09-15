using System.Windows;
using Cadroue.UIShell.PSCasement;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PDeck;

public sealed partial class PConsole
{
    private void PConsoleStartHandle(object pSender, RoutedEventArgs pArguments)
    {
        LRunner pRunner = PConsoleStationRead().LStationRunner;
        PConsoleRunnerApply(pRunner);
        pRunner.LRunnerStart();
    }

    private void PConsolePauseHandle(object pSender, RoutedEventArgs pArguments)
        => PConsoleStationRead().LStationRunner.LRunnerPause();

    private void PConsoleCancelHandle(object pSender, RoutedEventArgs pArguments)
    {
        LWorkItem[] pRunningItems = PConsoleRunningRead();
        if (pRunningItems.Length == 0)
        {
            return;
        }

        string pCancelSubject = pRunningItems.Length > 1
            ? LLocalization.LLocalizationFormat("Console.Cancel.MultipleSubject", pRunningItems.Length)
            : LLocalization.LLocalizationFormat("Console.Cancel.SingleSubject", pRunningItems[0].LWorkOutputName);
        bool pAnswer = PSAlert.PSAlertConfirm(
            Window.GetWindow(this),
            LLocalization.LLocalizationTextRead("Console.Cancel.Title"),
            LLocalization.LLocalizationFormat("Console.Cancel.Confirm", pCancelSubject),
            LLocalization.LLocalizationTextRead("Terms.Stop"),
            LLocalization.LLocalizationTextRead("Console.Cancel.Keep"));

        if (pAnswer)
        {
            LStation pCancelStation = PConsoleStationRead();
            pCancelStation.LStationAutoActive = false;
            LRunner pCancelRunner = pCancelStation.LStationRunner;
            foreach (LWorkItem pCancelItem in pRunningItems)
            {
                pCancelRunner.LRunnerJobCancel(pCancelItem.LWorkId);
            }
        }
    }

    private void PConsoleStopHandle(object pSender, RoutedEventArgs pArguments)
    {
        LStation pStopStation = PConsoleStationRead();
        pStopStation.LStationAutoActive = false;
        _ = pStopStation.LStationRunner.LRunnerCancel();
    }

    private void PConsoleRemoveHandle(object pSender, RoutedEventArgs pArguments)
    {
        IReadOnlyList<Guid> pConsoleRemovable = pConsoleSchedule.LScheduleRemovableRead(
            PConsoleStationRead().LStationSelectionRead().Select(pWorkItem => pWorkItem.LWorkId));
        if (pConsoleRemovable.Count == 0)
        {
            return;
        }

        PConsoleRemovalShow(
            Window.GetWindow(this), pConsoleSchedule.LScheduleBatchRemove(pConsoleRemovable));
    }

    internal static void PConsoleRemovalShow(Window? pOwner, IReadOnlyDictionary<Guid, LScheduleRemoval> pOutcomes)
    {
        int pHeld = pOutcomes.Values.Count(pOutcome => pOutcome == LScheduleRemoval.LScheduleRemovalHeld);
        int pBlocked = pOutcomes.Values.Count(pOutcome => pOutcome == LScheduleRemoval.LScheduleRemovalBlocked);
        if (pHeld == 0 && pBlocked == 0)
        {
            return;
        }

        string pDetail = pHeld > 0 && pBlocked > 0
            ? LLocalization.LLocalizationFormat("Console.Remove.HeldAndBlocked", pHeld, pBlocked)
            : pHeld > 0
                ? LLocalization.LLocalizationFormat("Console.Remove.Held", pHeld)
                : LLocalization.LLocalizationFormat("Console.Remove.Blocked", pBlocked);
        PSWarning.PSWarningShow(
            pOwner,
            LLocalization.LLocalizationTextRead("Console.Remove.Title"),
            LLocalization.LLocalizationFormat(
                "Console.Remove.Partial", pOutcomes.Count - pHeld - pBlocked, pOutcomes.Count, pDetail));
    }

    private void PConsoleDoneHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (!PConsoleDestructiveConfirm(LLocalization.LLocalizationTextRead("Console.ClearDone.Confirm"))) return;
        pConsoleSchedule.LScheduleDoneClear();
    }

    private async void PConsoleAllHandle(object pSender, RoutedEventArgs pArguments)
    {
        bool pConsoleProcessing = LStation.LStationActiveCheck();
        bool pConsoleConfirmed = pConsoleProcessing
            ? PConsoleProcessingConfirm(LLocalization.LLocalizationTextRead("Console.ClearAll.RunningConfirm"))
            : PConsoleDestructiveConfirm(LLocalization.LLocalizationTextRead("Console.ClearAll.Confirm"));
        if (!pConsoleConfirmed)
        {
            return;
        }

        var pConsoleDraining = new List<Task>();
        foreach (LStation pConsoleClearStation in LStation.LStationBoardRead())
        {
            pConsoleClearStation.LStationAutoActive = false;
            pConsoleDraining.Add(pConsoleClearStation.LStationRunner.LRunnerCancel());
        }

        await Task.WhenAll(pConsoleDraining);
        LMessenger.LMessengerMeasureCancel();
        pConsoleSchedule.LScheduleAllClear();
    }

    private void PConsoleTabsHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (!PConsoleDestructiveConfirm(LLocalization.LLocalizationTextRead("Console.ClearTabs.Confirm"))) return;
        PToolbar.PStrip.PStripCurrent?.PStripContentClear();
    }

    private bool PConsoleProcessingConfirm(string pConsoleQuestion) =>
        PSAlert.PSAlertConfirm(
            Window.GetWindow(this),
            LLocalization.LLocalizationTextRead("Console.Confirm.Title"),
            pConsoleQuestion,
            LLocalization.LLocalizationTextRead("Terms.Stop"));

    private bool PConsoleDestructiveConfirm(string pConsoleQuestion)
    {
        if (!LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive)
        {
            return true;
        }

        return PSAlert.PSAlertConfirm(
            Window.GetWindow(this),
            LLocalization.LLocalizationTextRead("Console.Confirm.Title"),
            pConsoleQuestion,
            LLocalization.LLocalizationTextRead("Terms.Remove"));
    }

    private static void PConsoleRunnerApply(LRunner pRunner)
    {
        LPreferenceState lPreferenceState = LPreference.LPreferenceStateCurrent;
        pRunner.LRunnerProgramPath = Cadroue.Infrastructure.LRenderer.LRendererProgramCurrent;
        pRunner.LRunnerFailurePaused = lPreferenceState.LPreferenceFailurePaused;
        pRunner.LRunnerRetryAllowed = lPreferenceState.LPreferenceRetryAllowed;
        pRunner.LRunnerRetryMaximum = (int)lPreferenceState.LPreferenceRetryMaximum;
    }
}
