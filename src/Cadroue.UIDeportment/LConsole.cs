using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record LConsoleRun(string LConsoleRunText, bool LConsoleRunAccent);

public sealed record LConsoleStatus(
    IReadOnlyList<LConsoleRun> LConsoleStatusRuns,
    bool LConsoleStatusRunning,
    bool LConsoleStatusLoaded,
    bool LConsoleStatusBusy,
    bool LConsoleStatusActive,
    bool LConsoleStatusRemovable,
    bool LConsoleStatusDone,
    bool LConsoleStatusClearable,
    bool LConsoleStatusBoard,
    bool LConsoleStatusAuto,
    string LConsoleStatusStation);

public sealed class LConsole
{
    private readonly LScheduleContract lSchedule = LProgram.LScheduleCurrent;
    private bool lConsoleSpinning;
    private bool lConsoleProgressPending;
    private bool lConsoleBackward;
    private double lConsoleProgress;
    private LStrip? lStrip;

    public LConsole()
    {
        LConsoleStation = new LConsoleStation(lSchedule);
        LConsoleScene = new LConsoleScene();
        lSchedule.LScheduleChange += LConsoleScheduleHandle;
        lSchedule.LScheduleItemChange += LConsoleItemHandle;
        LStation.LStationChange += LConsoleUpdate;
        LConsoleStation.LConsoleStationChange += LConsoleUpdate;
    }

    public LConsoleStation LConsoleStation { get; }

    public LConsoleScene LConsoleScene { get; }

    public bool LConsoleSpinning => lConsoleSpinning;

    public bool LConsoleProgressPending => lConsoleProgressPending;

    public bool LConsoleBackward => lConsoleBackward;

    public double LConsoleProgress => lConsoleProgress;

    public event Action<LConsoleStatus>? LConsoleStatusApply;

    public event Action<double, bool>? LConsoleProgressApply;

    public event Action<bool>? LConsoleSpinApply;

    public event Action? LConsoleUpdateDefer;

    public event Action<string, string>? LConsoleWarningShow;

    public void LConsoleWindowAttach(LWindow lWindow, LStrip lStripOwner)
    {
        lStrip = lStripOwner;
        LConsoleScene.LConsoleWindowAttach(lWindow, lStripOwner);
    }

    public void LConsoleClose()
    {
        lSchedule.LScheduleChange -= LConsoleScheduleHandle;
        lSchedule.LScheduleItemChange -= LConsoleItemHandle;
        LStation.LStationChange -= LConsoleUpdate;
        LConsoleStation.LConsoleStationChange -= LConsoleUpdate;
        LConsoleStation.LConsoleWatchStop();
    }

    public void LConsoleScheduleLoad() => lSchedule.LScheduleLoad();

    public void LConsoleStationSet(LStation lStation) => LConsoleStation.LConsoleStationSet(lStation);

    private void LConsoleScheduleHandle(LScheduleContract lScheduleChanged) => LConsoleUpdate();

    private void LConsoleItemHandle(LWorkItem lWorkItem, LScheduleNotice lNotice)
    {
        if (lNotice != LScheduleNotice.LScheduleNoticeProgress)
        {
            LConsoleDefer();
            return;
        }

        if (LConsoleStation.LConsoleOwnerCheck(lWorkItem))
        {
            LConsoleProgressSet(lWorkItem.LWorkProgress);
        }
    }

    private void LConsoleDefer()
    {
        if (lConsoleProgressPending)
        {
            return;
        }

        lConsoleProgressPending = true;
        LConsoleUpdateDefer?.Invoke();
    }

    public void LConsolePendingSet(bool lProgressPending) => lConsoleProgressPending = lProgressPending;

    public bool LConsoleSpinSet(bool lSpinning)
    {
        if (lConsoleSpinning == lSpinning)
        {
            return false;
        }

        lConsoleSpinning = lSpinning;
        LConsoleSpinApply?.Invoke(lSpinning);
        return true;
    }

    public bool LConsoleProgressSet(double lTarget)
    {
        double lClamped = Math.Clamp(lTarget, 0, 1);
        if (lClamped.Equals(lConsoleProgress))
        {
            return false;
        }

        lConsoleBackward = lClamped < lConsoleProgress;
        lConsoleProgress = lClamped;
        LConsoleProgressApply?.Invoke(lClamped, lClamped > 0 && !lConsoleBackward);
        return true;
    }

    public void LConsoleUpdate()
    {
        lConsoleProgressPending = false;
        LWorkItem[] lRunningItems = LConsoleStation.LConsoleRunningRead();
        LConsoleProgressSet(
            lRunningItems.Length == 0 ? 0 : lRunningItems.Average(lWorkItem => lWorkItem.LWorkProgress));
        bool lRunning = LConsoleStation.LConsoleRunning;
        LConsoleSpinSet(lRunning);
        LConsoleStatusApply?.Invoke(LConsoleStatusResolve(lRunningItems, lRunning));
    }

    private LConsoleStatus LConsoleStatusResolve(LWorkItem[] lRunningItems, bool lRunning)
    {
        int lTotal = lSchedule.LScheduleRecords.Count;
        bool lSuspended = LConsoleStation.LConsoleSuspended;
        bool lPaused = !lRunning && !lSuspended;
        string lRunState = LLocalization.LLocalizationTextRead(
            lSuspended ? "Console.State.Suspended" : lRunning ? "Console.State.Running" : "Console.State.Paused");
        return new LConsoleStatus(
            LConsoleRunsResolve(
                LConsoleLineFormat(lRunningItems, lTotal, lRunState),
                lTotal > 0 && lPaused ? lRunState : null),
            lRunning,
            lTotal > 0,
            lRunningItems.Length > 0,
            lRunning || lSuspended,
            LConsoleStation.LConsoleSelectionRead()
                .Any(lWorkItem => lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning),
            lSchedule.LScheduleRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone),
            lSchedule.LScheduleRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning),
            LConsoleStation.LConsoleSwitchShown,
            LConsoleStation.LConsoleAutoActive,
            LConsoleStation.LConsoleLabelFormat());
    }

    private string LConsoleLineFormat(LWorkItem[] lRunningItems, int lTotal, string lRunState)
    {
        if (lTotal == 0)
        {
            return LLocalization.LLocalizationTextRead("Console.Status.Empty");
        }

        string lDoneText = LLocalization.LLocalizationFormat(
            "Console.Done.Format", lSchedule.LScheduleDoneCount, lTotal);
        if (lRunningItems.Length == 0)
        {
            return LLocalization.LLocalizationFormat(
                "Console.Status.Pending", lRunState, lDoneText, lSchedule.LSchedulePendingRead().Count);
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
            LConsoleStageFormat(lRunningItem.LWorkStageCurrent),
            lRunningItem.LWorkProgress);
    }

    public static IReadOnlyList<LConsoleRun> LConsoleRunsResolve(string lLine, string? lAccent)
    {
        int lAccentIndex = string.IsNullOrEmpty(lAccent) ? -1 : lLine.IndexOf(lAccent, StringComparison.Ordinal);
        if (lAccentIndex < 0)
        {
            return [new LConsoleRun(lLine, false)];
        }

        var lRuns = new List<LConsoleRun>();
        if (lAccentIndex > 0)
        {
            lRuns.Add(new LConsoleRun(lLine[..lAccentIndex], false));
        }

        lRuns.Add(new LConsoleRun(lAccent!, true));
        int lAccentEnd = lAccentIndex + lAccent!.Length;
        if (lAccentEnd < lLine.Length)
        {
            lRuns.Add(new LConsoleRun(lLine[lAccentEnd..], false));
        }

        return lRuns;
    }

    private static string LConsoleStageFormat(LWorkStage lWorkStage) =>
        LLocalization.LLocalizationTextRead(lWorkStage switch
        {
            LWorkStage.LWorkStageExtract => "Console.Stage.Extract",
            LWorkStage.LWorkStageAnalyze => "Console.Stage.Analyze",
            LWorkStage.LWorkStageProcess => "Console.Stage.Process",
            LWorkStage.LWorkStageMux => "Console.Stage.Mux",
            LWorkStage.LWorkStagePassthrough => "Console.Stage.Copy",
            _ => "Console.Stage.Encode"
        });

    public void LConsoleStart() => LConsoleStation.LConsoleStart();

    public void LConsolePause() => LConsoleStation.LConsolePause();

    public void LConsoleStop() => LConsoleStation.LConsoleStop();

    public void LConsoleAutoSet(bool? lChecked)
    {
        if (LConsoleStation.LConsoleAutoSet(lChecked == true))
        {
            LConsoleUpdate();
        }
    }

    public void LConsoleCancel()
    {
        LWorkItem[] lRunningItems = LConsoleStation.LConsoleRunningRead();
        if (lRunningItems.Length == 0)
        {
            return;
        }

        string lSubject = lRunningItems.Length > 1
            ? LLocalization.LLocalizationFormat("Console.Cancel.MultipleSubject", lRunningItems.Length)
            : LLocalization.LLocalizationFormat("Console.Cancel.SingleSubject", lRunningItems[0].LWorkOutputName);
        var lAsk = new LAsk(
            LLocalization.LLocalizationFormat("Console.Cancel.Confirm", lSubject),
            LLocalization.LLocalizationTextRead("Terms.Stop"))
        {
            LAskTitle = LLocalization.LLocalizationTextRead("Console.Cancel.Title"),
            LAskDismiss = LLocalization.LLocalizationTextRead("Console.Cancel.Keep")
        };
        LAskNotice.LAskPublish(lAsk, lAnswer => LConsoleCancelRun(lAnswer, lRunningItems));
    }

    private void LConsoleCancelRun(bool lAnswer, LWorkItem[] lRunningItems)
    {
        if (lAnswer)
        {
            LConsoleStation.LConsoleJobsCancel(lRunningItems);
        }
    }

    public void LConsoleRemove()
    {
        IReadOnlyList<Guid> lRemovable = lSchedule.LScheduleRemovableRead(
            LConsoleStation.LConsoleSelectionRead().Select(lWorkItem => lWorkItem.LWorkId));
        if (lRemovable.Count == 0)
        {
            return;
        }

        LConsoleRemovalRaise(lSchedule.LScheduleBatchRemove(lRemovable));
    }

    public void LConsoleRemovalRaise(IReadOnlyDictionary<Guid, LScheduleRemoval> lOutcomes)
    {
        if (LConsoleRemovalFormat(lOutcomes) is { } lMessage)
        {
            LConsoleWarningShow?.Invoke(LLocalization.LLocalizationTextRead("Console.Remove.Title"), lMessage);
        }
    }

    public static string? LConsoleRemovalFormat(IReadOnlyDictionary<Guid, LScheduleRemoval> lOutcomes)
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

    public void LConsoleDoneClear() =>
        LAskNotice.LAskPublish(
            LConsoleAskResolve("Console.ClearDone.Confirm", "Terms.Remove"),
            lAnswer => LConsoleDoneRun(lAnswer));

    private void LConsoleDoneRun(bool lAnswer)
    {
        if (lAnswer)
        {
            lSchedule.LScheduleDoneClear();
        }
    }

    public void LConsoleAllClear()
    {
        LAsk? lAsk = LStation.LStationActiveCheck()
            ? LConsoleAskCreate("Console.ClearAll.RunningConfirm", "Terms.Stop")
            : LConsoleAskResolve("Console.ClearAll.Confirm", "Terms.Remove");
        LAskNotice.LAskPublish(lAsk, lAnswer => _ = LConsoleAllRun(lAnswer));
    }

    private async Task LConsoleAllRun(bool lAnswer)
    {
        if (!lAnswer)
        {
            return;
        }

        await LConsoleStation.LConsoleBoardCancel();
        LMessenger.LMessengerMeasureCancel();
        lSchedule.LScheduleAllClear();
    }

    public void LConsoleTabsClear() =>
        LAskNotice.LAskPublish(
            LConsoleAskResolve("Console.ClearTabs.Confirm", "Terms.Remove"),
            lAnswer => LConsoleTabsRun(lAnswer));

    private void LConsoleTabsRun(bool lAnswer)
    {
        if (lAnswer)
        {
            lStrip?.LStripContentClear();
        }
    }

    private static LAsk? LConsoleAskResolve(string lQuestionKey, string lActionKey) =>
        LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive
            ? LConsoleAskCreate(lQuestionKey, lActionKey)
            : null;

    private static LAsk LConsoleAskCreate(string lQuestionKey, string lActionKey) =>
        new(LLocalization.LLocalizationTextRead(lQuestionKey), LLocalization.LLocalizationTextRead(lActionKey))
        {
            LAskTitle = LLocalization.LLocalizationTextRead("Console.Confirm.Title")
        };
}
