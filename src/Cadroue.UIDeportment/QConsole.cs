using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record QConsoleSpan(string QConsoleRunText, bool QConsoleRunAccent);

public sealed record QConsoleStatus(
    IReadOnlyList<QConsoleSpan> QConsoleStatusRuns,
    bool QConsoleStatusRunning,
    bool QConsoleStatusLoaded,
    bool QConsoleStatusBusy,
    bool QConsoleStatusActive,
    bool QConsoleStatusRemovable,
    bool QConsoleStatusDone,
    bool QConsoleStatusClearable,
    bool QConsoleStatusBoard,
    bool QConsoleStatusAuto,
    string QConsoleStatusStation);

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

    public void QConsoleWindowAttach(QWindow lWindow, QStrip lStripOwner)
    {
        qStrip = lStripOwner;
        lStripOwner.QStripSelectChanged += QConsoleTabHandle;
        QConsoleScene.QConsoleWindowAttach(lWindow, lStripOwner);
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

        qConsoleProgressPending = true;
        QConsoleUpdateDeferred?.Invoke();
    }

    public void QConsolePendingSet(bool lProgressPending) => qConsoleProgressPending = lProgressPending;

    public bool QConsoleProgressSet(double lTarget)
    {
        double lClamped = Math.Clamp(lTarget, 0, 1);
        if (lClamped.Equals(qConsoleProgress))
        {
            return false;
        }

        qConsoleBackward = lClamped < qConsoleProgress;
        qConsoleProgress = lClamped;
        QConsoleProgressApplied?.Invoke(lClamped, lClamped > 0 && !qConsoleBackward);
        return true;
    }

    public void QConsoleUpdate()
    {
        qConsoleProgressPending = false;
        LWorkItem[] lRunningItems = QConsoleStation.QConsoleRunningRead();
        QConsoleProgressSet(
            lRunningItems.Length == 0 ? 0 : lRunningItems.Average(lWorkItem => lWorkItem.LWorkProgress));
        bool lRunning = QConsoleStation.QConsoleRunning;
        QConsoleStatusApplied?.Invoke(QConsoleStatusResolve(lRunningItems, lRunning));
    }

    private QConsoleStatus QConsoleStatusResolve(LWorkItem[] lRunningItems, bool lRunning)
    {
        int lTotal = qSchedule.LScheduleRecords.Count;
        bool lSuspended = QConsoleStation.QConsoleSuspended;
        bool lPaused = !lRunning && !lSuspended;
        string lRunState = LLocalization.LLocalizationTextRead(
            lSuspended ? "Console.State.Suspended" : lRunning ? "Console.State.Running" : "Console.State.Paused");
        return new QConsoleStatus(
            QConsoleRunsResolve(
                QConsoleLineFormat(lRunningItems, lTotal, lRunState),
                lTotal > 0 && lPaused ? lRunState : null),
            lRunning,
            lTotal > 0,
            lRunningItems.Length > 0,
            lRunning || lSuspended,
            QConsoleStation.QConsoleSelectionRead()
                .Any(lWorkItem => lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning),
            qSchedule.LScheduleRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone),
            qSchedule.LScheduleRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning),
            QConsoleStation.QConsoleSwitchShown,
            QConsoleStation.QConsoleAutoActive,
            QConsoleStation.QConsoleLabelFormat());
    }

    private string QConsoleLineFormat(LWorkItem[] lRunningItems, int lTotal, string lRunState)
    {
        if (lTotal == 0)
        {
            return LLocalization.LLocalizationTextRead("Console.Status.Empty");
        }

        string lDoneText = LLocalization.LLocalizationFormat(
            "Console.Done.Format", qSchedule.LScheduleDoneCount, lTotal);
        if (lRunningItems.Length == 0)
        {
            return LLocalization.LLocalizationFormat(
                "Console.Status.Pending", lRunState, lDoneText, qSchedule.LSchedulePendingRead().Count);
        }

        if (lRunningItems.Length > 1)
        {
            return LLocalization.LLocalizationFormat(
                "Console.Status.MultipleRunning",
                lRunningItems.Length,
                lRunningItems.Average(lWorkItem => lWorkItem.LWorkProgress),
                lDoneText);
        }

        LWorkItem lRunningItem = lRunningItems[0];
        return LLocalization.LLocalizationFormat(
            "Console.Status.Running",
            lRunningItem.LWorkOutputName,
            lDoneText,
            QConsoleStageFormat(lRunningItem.LWorkStageCurrent),
            lRunningItem.LWorkProgress);
    }

    public static IReadOnlyList<QConsoleSpan> QConsoleRunsResolve(string lLine, string? lAccent)
    {
        int lAccentIndex = string.IsNullOrEmpty(lAccent) ? -1 : lLine.IndexOf(lAccent, StringComparison.Ordinal);
        if (lAccentIndex < 0)
        {
            return [new QConsoleSpan(lLine, false)];
        }

        var lRuns = new List<QConsoleSpan>();
        if (lAccentIndex > 0)
        {
            lRuns.Add(new QConsoleSpan(lLine[..lAccentIndex], false));
        }

        lRuns.Add(new QConsoleSpan(lAccent!, true));
        int lAccentEnd = lAccentIndex + lAccent!.Length;
        if (lAccentEnd < lLine.Length)
        {
            lRuns.Add(new QConsoleSpan(lLine[lAccentEnd..], false));
        }

        return lRuns;
    }

    private static string QConsoleStageFormat(LWorkStage lWorkStage) =>
        LLocalization.LLocalizationTextRead(lWorkStage switch
        {
            LWorkStage.LWorkStageExtract => "Console.Stage.Extract",
            LWorkStage.LWorkStageAnalyze => "Console.Stage.Analyze",
            LWorkStage.LWorkStageProcess => "Console.Stage.Process",
            LWorkStage.LWorkStageMux => "Console.Stage.Mux",
            LWorkStage.LWorkStagePassthrough => "Console.Stage.Copy",
            _ => "Console.Stage.Encode"
        });

    public void QConsoleStart() => QConsoleStation.QConsoleStart();

    public void QConsolePause() => QConsoleStation.QConsolePause();

    public void QConsoleStop() => QConsoleStation.QConsoleStop();

    public void QConsoleAutoSet(bool? lChecked)
    {
        if (QConsoleStation.QConsoleAutoSet(lChecked == true))
        {
            QConsoleUpdate();
        }
    }

    public void QConsoleCancel()
    {
        LWorkItem[] lRunningItems = QConsoleStation.QConsoleRunningRead();
        if (lRunningItems.Length == 0)
        {
            return;
        }

        string lSubject = lRunningItems.Length > 1
            ? LLocalization.LLocalizationFormat("Console.Cancel.MultipleSubject", lRunningItems.Length)
            : LLocalization.LLocalizationFormat("Console.Cancel.SingleSubject", lRunningItems[0].LWorkOutputName);
        var lAsk = new QAsk(
            LLocalization.LLocalizationFormat("Console.Cancel.Confirm", lSubject),
            LLocalization.LLocalizationTextRead("Terms.Stop"))
        {
            QAskTitle = LLocalization.LLocalizationTextRead("Console.Cancel.Title"),
            QAskDismiss = LLocalization.LLocalizationTextRead("Console.Cancel.Keep")
        };
        QAskNotice.QAskPublish(lAsk, lAnswer => QConsoleCancelRun(lAnswer, lRunningItems));
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
            QConsoleWarningShown?.Invoke(LLocalization.LLocalizationTextRead("Console.Remove.Title"), lMessage);
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
            ? LLocalization.LLocalizationFormat("Console.Remove.HeldAndBlocked", lHeld, lBlocked)
            : lHeld > 0
                ? LLocalization.LLocalizationFormat("Console.Remove.Held", lHeld)
                : LLocalization.LLocalizationFormat("Console.Remove.Blocked", lBlocked);
        return LLocalization.LLocalizationFormat(
            "Console.Remove.Partial", lOutcomes.Count - lHeld - lBlocked, lOutcomes.Count, lDetail);
    }

    public void QConsoleDoneClear() =>
        QAskNotice.QAskPublish(
            QConsoleAskResolve("Console.ClearDone.Confirm", "Terms.Remove"),
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
            : QConsoleAskResolve("Console.ClearAll.Confirm", "Terms.Remove");
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
            QConsoleAskResolve("Console.ClearTabs.Confirm", "Terms.Remove"),
            lAnswer => QConsoleTabsRun(lAnswer));

    private void QConsoleTabsRun(bool lAnswer)
    {
        if (lAnswer)
        {
            qStrip?.QStripContentClear();
        }
    }

    private static QAsk? QConsoleAskResolve(string lQuestionKey, string lActionKey) =>
        LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive
            ? QConsoleAskCreate(lQuestionKey, lActionKey)
            : null;

    private static QAsk QConsoleAskCreate(string lQuestionKey, string lActionKey) =>
        new(LLocalization.LLocalizationTextRead(lQuestionKey), LLocalization.LLocalizationTextRead(lActionKey))
        {
            QAskTitle = LLocalization.LLocalizationTextRead("Console.Confirm.Title")
        };
}
