using Cadroue.Application;
using Cadroue.Core;

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

public static class QConsoleLine
{
    internal static QConsoleStatus QConsoleStatusResolve(
        LWorkItem[] lRunningItems,
        bool lRunning,
        bool lSuspended,
        IReadOnlyList<LWorkItem> lRecords,
        int lDoneCount,
        int lPendingCount,
        IReadOnlyList<LWorkItem> lSelection,
        bool lBoard,
        bool lAuto,
        string lStation)
    {
        int lTotal = lRecords.Count;
        bool lPaused = !lRunning && !lSuspended;
        string lRunState = LLocalization.LLocalizationTextRead(
            lSuspended ? "Console.State.Suspended" : lRunning ? "Console.State.Running" : "Console.State.Paused");
        return new QConsoleStatus(
            QConsoleRunsResolve(
                QConsoleLineFormat(lRunningItems, lTotal, lRunState, lDoneCount, lPendingCount),
                lTotal > 0 && lPaused ? lRunState : null),
            lRunning,
            lTotal > 0,
            lRunningItems.Length > 0,
            lRunning || lSuspended,
            lSelection.Any(lWorkItem => lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning),
            lRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone),
            lRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning),
            lBoard,
            lAuto,
            lStation);
    }

    private static string QConsoleLineFormat(
        LWorkItem[] lRunningItems, int lTotal, string lRunState, int lDoneCount, int lPendingCount)
    {
        if (lTotal == 0)
        {
            return LLocalization.LLocalizationTextRead("Console.Status.Empty");
        }

        string lDoneText = LLocalization.LLocalizationFormat("Console.Done.Format", lDoneCount, lTotal);
        if (lRunningItems.Length == 0)
        {
            return LLocalization.LLocalizationFormat("Console.Status.Pending", lRunState, lDoneText, lPendingCount);
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
}
