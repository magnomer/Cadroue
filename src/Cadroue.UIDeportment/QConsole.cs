using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QConsole
{
    private readonly LScheduleContract qSchedule = QProgram.QScheduleCurrent;
    private bool qConsoleProgressPending;
    private bool qConsoleBackward;
    private double qConsoleProgress;
    private QStrip? qStrip;

    public QConsole()
    {
        QConsoleStation = new QConsoleStation(qSchedule);
        QConsoleScene = new QConsoleScene();
        qSchedule.LScheduleChanged += QConsoleScheduleHandle;
        qSchedule.LScheduleItemChanged += QConsoleItemHandle;
        LStation.LStationChanged += QConsoleUpdate;
        QConsoleStation.QConsoleStationChanged += QConsoleUpdate;
    }

    public QConsoleStation QConsoleStation { get; }

    public QConsoleScene QConsoleScene { get; }

    public bool QConsoleProgressPending => qConsoleProgressPending;

    public bool QConsoleBackward => qConsoleBackward;

    public double QConsoleProgress => qConsoleProgress;

    public event Action<QConsoleStatus>? QConsoleStatusApplied;

    public event Action<double, bool>? QConsoleProgressApplied;

    public event Action? QConsoleUpdateDeferred;

    public event Action<string, string>? QConsoleWarningShown;

    public void QConsoleWindowAttach(QWindowScene lSceneOwner, QStrip lStripOwner)
    {
        qStrip = lStripOwner;
        lStripOwner.QStripSelectChanged += QConsoleTabHandle;
        QConsoleScene.QConsoleWindowAttach(lSceneOwner, lStripOwner);
    }

    private void QConsoleTabHandle(QStripTab? lTab)
    {
        if (lTab?.QStripTabWorkspace.QWorkspaceStation is { } lStation)
        {
            QConsoleStation.QConsoleStationSet(lStation);
        }
    }

    public void QConsoleClose()
    {
        qSchedule.LScheduleChanged -= QConsoleScheduleHandle;
        qSchedule.LScheduleItemChanged -= QConsoleItemHandle;
        LStation.LStationChanged -= QConsoleUpdate;
        QConsoleStation.QConsoleStationChanged -= QConsoleUpdate;
        QConsoleStation.QConsoleWatchStop();
        if (qStrip is not null)
        {
            qStrip.QStripSelectChanged -= QConsoleTabHandle;
        }
    }

    public void QConsoleScheduleLoad() => qSchedule.LScheduleLoad();

    public void QConsoleStationSet(LStation lStation) => QConsoleStation.QConsoleStationSet(lStation);

    private void QConsoleScheduleHandle(LScheduleContract lScheduleChanged) => QConsoleUpdate();

    private void QConsoleItemHandle(LWorkItem lWorkItem, LScheduleNotice lNotice)
    {
        if (lNotice != LScheduleNotice.LScheduleNoticeProgress)
        {
            QConsoleDefer();
            return;
        }

        if (QConsoleStation.QConsoleOwnerCheck(lWorkItem))
        {
            QConsoleProgressSet(lWorkItem.LWorkProgress);
        }
    }

    private void QConsoleDefer()
    {
        if (qConsoleProgressPending)
        {
            return;
        }

        QConsolePendingSet(true);
        QConsoleUpdateDeferred?.Invoke();
    }

    public void QConsolePendingSet(bool lProgressPending) => qConsoleProgressPending = lProgressPending;

    public bool QConsoleProgressSet(double lTarget)
    {
        double lClamped = QConsoleProgressClamp(lTarget);
        if (!QConsoleFractionSet(lClamped))
        {
            return false;
        }

        QConsoleProgressRaise();
        return true;
    }

    private static double QConsoleProgressClamp(double lTarget) => Math.Clamp(lTarget, 0, 1);

    private bool QConsoleFractionSet(double lClamped)
    {
        if (lClamped.Equals(qConsoleProgress))
        {
            return false;
        }

        qConsoleBackward = lClamped < qConsoleProgress;
        qConsoleProgress = lClamped;
        return true;
    }

    private void QConsoleProgressRaise() =>
        QConsoleProgressApplied?.Invoke(qConsoleProgress, qConsoleProgress > 0 && !qConsoleBackward);

    public void QConsoleUpdate()
    {
        QConsolePendingSet(false);
        LWorkItem[] lRunningItems = QConsoleStation.QConsoleRunningRead();
        QConsoleProgressSet(QConsoleProgressResolve(lRunningItems));
        bool lRunning = QConsoleStation.QConsoleRunning;
        QConsoleStatusApplied?.Invoke(QConsoleStatusRead(lRunningItems, lRunning));
    }

    private static double QConsoleProgressResolve(LWorkItem[] lRunningItems) =>
        lRunningItems.Length == 0 ? 0 : lRunningItems.Average(lWorkItem => lWorkItem.LWorkProgress);

    private QConsoleStatus QConsoleStatusRead(LWorkItem[] lRunningItems, bool lRunning) =>
        QConsoleLine.QConsoleStatusResolve(
            lRunningItems,
            lRunning,
            QConsoleStation.QConsoleSuspended,
            qSchedule.LScheduleRecords,
            qSchedule.LScheduleDoneCount,
            qSchedule.LSchedulePendingRead().Count,
            QConsoleStation.QConsoleSelectionRead(),
            QConsoleStation.QConsoleSwitchShown,
            QConsoleStation.QConsoleAutoActive,
            QConsoleStation.QConsoleLabelFormat());

    public void QConsoleStart() => QConsoleStation.QConsoleStart();

    public void QConsolePause() => QConsoleStation.QConsolePause();

    public void QConsoleStop() => QConsoleStation.QConsoleStop();

    public void QConsoleAutoSet(bool? lChecked)
    {
        if (QConsoleStation.QConsoleAutoSet(QConsoleCheckedResolve(lChecked)))
        {
            QConsoleUpdate();
        }
    }

    private static bool QConsoleCheckedResolve(bool? lChecked) => lChecked == true;

    public void QConsoleCancel()
    {
        LWorkItem[] lRunningItems = QConsoleStation.QConsoleRunningRead();
        if (lRunningItems.Length == 0)
        {
            return;
        }

        QAskNotice.QAskPublish(
            QConsoleCancelCreate(lRunningItems), lAnswer => QConsoleCancelRun(lAnswer, lRunningItems));
    }

    private static QAsk QConsoleCancelCreate(LWorkItem[] lRunningItems)
    {
        string lSubject = lRunningItems.Length > 1
            ? QLocalizationCatalog.QLocalizationFormat("Console.Cancel.MultipleSubject", lRunningItems.Length)
            : QLocalizationCatalog.QLocalizationFormat("Console.Cancel.SingleSubject",
                lRunningItems[0].LWorkOutputName);
        return new QAsk(
            QLocalizationCatalog.QLocalizationFormat("Console.Cancel.Confirm", lSubject),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Stop"))
        {
            QAskTitle = QLocalizationCatalog.QLocalizationTextRead("Console.Cancel.Title"),
            QAskDismiss = QLocalizationCatalog.QLocalizationTextRead("Console.Cancel.Keep")
        };
    }

    private void QConsoleCancelRun(bool lAnswer, LWorkItem[] lRunningItems)
    {
        if (lAnswer)
        {
            QConsoleStation.QConsoleJobsCancel(lRunningItems);
        }
    }

    public void QConsoleRemove()
    {
        IReadOnlyList<Guid> lRemovable = qSchedule.LScheduleRemovableRead(
            QConsoleStation.QConsoleSelectionRead().Select(lWorkItem => lWorkItem.LWorkId));
        if (lRemovable.Count == 0)
        {
            return;
        }

        QConsoleRemovalRaise(qSchedule.LScheduleBatchRemove(lRemovable));
    }

    public void QConsoleRemovalRaise(IReadOnlyDictionary<Guid, LScheduleRemoval> lOutcomes)
    {
        if (QConsoleRemovalFormat(lOutcomes) is { } lMessage)
        {
            QConsoleWarningShown?.Invoke(QLocalizationCatalog.QLocalizationTextRead("Console.Remove.Title"), lMessage);
        }
    }

    public static string? QConsoleRemovalFormat(IReadOnlyDictionary<Guid, LScheduleRemoval> lOutcomes)
    {
        int lHeld = lOutcomes.Values.Count(lOutcome => lOutcome == LScheduleRemoval.LScheduleRemovalHeld);
        int lBlocked = lOutcomes.Values.Count(lOutcome => lOutcome == LScheduleRemoval.LScheduleRemovalBlocked);
        if (lHeld == 0 && lBlocked == 0)
        {
            return null;
        }

        string lDetail = lHeld > 0 && lBlocked > 0
            ? QLocalizationCatalog.QLocalizationFormat("Console.Remove.HeldAndBlocked", lHeld, lBlocked)
            : lHeld > 0
                ? QLocalizationCatalog.QLocalizationFormat("Console.Remove.Held", lHeld)
                : QLocalizationCatalog.QLocalizationFormat("Console.Remove.Blocked", lBlocked);
        return QLocalizationCatalog.QLocalizationFormat(
            "Console.Remove.Partial", lOutcomes.Count - lHeld - lBlocked, lOutcomes.Count, lDetail);
    }

    public void QConsoleDoneClear() =>
        QAskNotice.QAskPublish(
            QAskNotice.QAskCreate("Console.ClearDone.Confirm", "Terms.Remove", "Console.Confirm.Title"),
            lAnswer => QConsoleDoneRun(lAnswer));

    private void QConsoleDoneRun(bool lAnswer)
    {
        if (lAnswer)
        {
            qSchedule.LScheduleDoneClear();
        }
    }

    public void QConsoleAllClear()
    {
        QAsk? lAsk = LStation.LStationActiveCheck()
            ? QConsoleAskCreate("Console.ClearAll.RunningConfirm", "Terms.Stop")
            : QAskNotice.QAskCreate("Console.ClearAll.Confirm", "Terms.Remove", "Console.Confirm.Title");
        QAskNotice.QAskPublish(lAsk, lAnswer => _ = QConsoleAllRun(lAnswer));
    }

    private async Task QConsoleAllRun(bool lAnswer)
    {
        if (!lAnswer)
        {
            return;
        }

        await QConsoleStation.QConsoleBoardCancel();
        LMessenger.LMessengerMeasureCancel();
        qSchedule.LScheduleAllClear();
    }

    public void QConsoleTabsClear() =>
        QAskNotice.QAskPublish(
            QAskNotice.QAskCreate("Console.ClearTabs.Confirm", "Terms.Remove", "Console.Confirm.Title"),
            lAnswer => QConsoleTabsRun(lAnswer));

    private void QConsoleTabsRun(bool lAnswer)
    {
        if (lAnswer)
        {
            qStrip?.QStripContentClear();
        }
    }

    private static QAsk QConsoleAskCreate(string lQuestionKey, string lActionKey) =>
        new(QLocalizationCatalog.QLocalizationTextRead(lQuestionKey), QLocalizationCatalog.QLocalizationTextRead(
            lActionKey))
        {
            QAskTitle = QLocalizationCatalog.QLocalizationTextRead("Console.Confirm.Title")
        };
}
