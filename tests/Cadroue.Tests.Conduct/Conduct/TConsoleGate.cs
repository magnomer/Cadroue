using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Xunit;

namespace Cadroue.Tests;

public sealed class TConsoleGate
{
    private readonly TConsolePort tConsolePort = new();
    private readonly CConsole tConsoleGate;
    private readonly List<string> tConsoleNotices = [];

    public TConsoleGate()
    {
        tConsoleGate = TAtelier.TAtelierCreate(port => port with { LAtelierConsole = tConsolePort })
            .CAtelierConsoleCreate();
        tConsoleGate.CConsoleChanged += () => tConsoleNotices.Add("changed");
        tConsoleGate.CConsoleDeferred += () => tConsoleNotices.Add("deferred");
        CConsoleProgress progress = tConsoleGate.CConsoleProgress;
        progress.CConsoleProgressChanged +=
            () => tConsoleNotices.Add(FormattableString.Invariant(
                $"progress {progress.CConsoleProgressValue} {progress.CConsoleProgressGlide}"));
    }

    private static CConsoleText TConsoleTextCreate(string key, params object[] arguments) =>
        new(key, arguments.Length == 0 ? null : arguments);

    private static void TConsoleTextCheck(CConsoleText expected, object actual)
    {
        CConsoleText text = Assert.IsType<CConsoleText>(actual);
        Assert.Equal(expected.CConsoleTextKey, text.CConsoleTextKey);
        IReadOnlyList<object> arguments = text.CConsoleTextArguments ?? [];
        IReadOnlyList<object> expectedArguments = expected.CConsoleTextArguments ?? [];
        Assert.Equal(expected.CConsoleTextArguments is null, text.CConsoleTextArguments is null);
        Assert.Equal(expectedArguments.Count, arguments.Count);
        for (int index = 0; index < arguments.Count; index++)
        {
            if (expectedArguments[index] is CConsoleText nested)
            {
                TConsoleTextCheck(nested, arguments[index]);
            }
            else
            {
                Assert.Equal(expectedArguments[index], arguments[index]);
            }
        }
    }

    private CConsoleStatus TConsoleUpdate(Func<LConsoleAnswer, LConsoleAnswer> answer)
    {
        tConsolePort.TConsoleAnswer = answer(tConsolePort.TConsoleAnswer);
        return tConsoleGate.CConsoleUpdate();
    }

    [Fact]
    public void ConsoleCreate_AttachesOnce_EngineChangeTold_CloseDetaches()
    {
        tConsolePort.TConsoleChanged!();
        tConsoleGate.CConsoleClose();

        Assert.Equal(["attach", "detach"], tConsolePort.TConsoleCalls);
        Assert.Equal(["changed"], tConsoleNotices);
    }

    [Fact]
    public void ConsoleUpdate_EmptySchedule_EmptyLineWithoutAccent()
    {
        CConsoleStatus status = TConsoleUpdate(answer => answer);

        TConsoleTextCheck(TConsoleTextCreate("Console.Status.Empty"), status.CConsoleStatusLine);
        Assert.Null(status.CConsoleStatusAccent);
        Assert.False(status.CConsoleStatusLoaded);
        Assert.False(status.CConsoleStatusBoard);
        Assert.Equal(string.Empty, status.CConsoleStatusStation);
    }

    [Fact]
    public void ConsoleUpdate_PausedWithRecords_PendingLineAccentsState()
    {
        CConsoleStatus status = TConsoleUpdate(answer => answer with
        {
            LConsoleTotal = 3, LConsoleDone = 1, LConsolePending = 2, LConsoleFinished = true
        });

        CConsoleText paused = TConsoleTextCreate("Console.State.Paused");
        TConsoleTextCheck(
            TConsoleTextCreate("Console.Status.Pending", paused, TConsoleTextCreate("Console.Done.Format", 1, 3), 2),
            status.CConsoleStatusLine);
        TConsoleTextCheck(paused, status.CConsoleStatusAccent!);
        Assert.True(status.CConsoleStatusLoaded);
        Assert.True(status.CConsoleStatusDone);
        Assert.False(status.CConsoleStatusActive);
    }

    [Theory]
    [InlineData("LWorkStageMux", "Console.Stage.Mux")]
    [InlineData("LWorkStagePassthrough", "Console.Stage.Copy")]
    [InlineData("LWorkStageVerify", "Console.Stage.Encode")]
    public void ConsoleUpdate_OneRunning_NamesOutputAndStage(string stage, string key)
    {
        CConsoleStatus status = TConsoleUpdate(answer => answer with
        {
            LConsoleRunning = true, LConsoleTotal = 2, LConsoleWorking = 1, LConsoleProgress = 0.4,
            LConsoleName = "out.mkv", LConsoleStage = stage, LConsoleFraction = 0.4
        });

        TConsoleTextCheck(
            TConsoleTextCreate(
                "Console.Status.Running",
                "out.mkv",
                TConsoleTextCreate("Console.Done.Format", 0, 2),
                TConsoleTextCreate(key),
                0.4),
            status.CConsoleStatusLine);
        Assert.Null(status.CConsoleStatusAccent);
        Assert.True(status.CConsoleStatusBusy);
        Assert.True(status.CConsoleStatusActive);
    }

    [Fact]
    public void ConsoleUpdate_SeveralRunning_CountsAndGlidesToMean()
    {
        CConsoleStatus status = TConsoleUpdate(answer => answer with
        {
            LConsoleRunning = true, LConsoleTotal = 4, LConsoleDone = 1, LConsoleWorking = 2, LConsoleProgress = 0.3
        });

        TConsoleTextCheck(
            TConsoleTextCreate(
                "Console.Status.MultipleRunning", 2, 0.3, TConsoleTextCreate("Console.Done.Format", 1, 4)),
            status.CConsoleStatusLine);
        Assert.Equal(["progress 0.3 True"], tConsoleNotices);
    }

    [Fact]
    public void ConsoleUpdate_Suspended_ActiveNotPaused()
    {
        CConsoleStatus status = TConsoleUpdate(answer => answer with { LConsoleSuspended = true, LConsoleTotal = 1 });

        TConsoleTextCheck(
            TConsoleTextCreate(
                "Console.Status.Pending",
                TConsoleTextCreate("Console.State.Suspended"),
                TConsoleTextCreate("Console.Done.Format", 0, 1),
                0),
            status.CConsoleStatusLine);
        Assert.Null(status.CConsoleStatusAccent);
        Assert.True(status.CConsoleStatusActive);
    }

    [Fact]
    public void ConsoleUpdate_Board_NumbersStationAndWordsBackground()
    {
        CConsoleStatus single = TConsoleUpdate(answer => answer with { LConsoleBackground = true });
        CConsoleStatus several = TConsoleUpdate(answer => answer with
        {
            LConsoleBackground = false, LConsoleLabel = "Worklist", LConsoleNumber = 2, LConsoleCount = 3
        });

        TConsoleTextCheck(TConsoleTextCreate("Console.Station.Background"), single.CConsoleStatusStation);
        TConsoleTextCheck(
            TConsoleTextCreate("Console.Station.Numbered", "Worklist", 2, 3), several.CConsoleStatusStation);
        Assert.True(several.CConsoleStatusBoard);
    }

    [Fact]
    public void ConsoleUpdate_KeepsResolvedStation()
    {
        LStation other = TConsolePort.TConsoleStationCreate();
        tConsolePort.TConsoleAnswer = TConsolePort.TConsoleAnswerCreate(other);

        tConsoleGate.CConsoleUpdate();
        tConsoleGate.CConsoleStart();

        Assert.Equal([null, other], tConsolePort.TConsoleHandles);
    }

    [Fact]
    public void ConsoleProgressed_OwnedRecord_ClampsAndGlidesForwardOnly()
    {
        Action<Predicate<LStation>, double> progressed = tConsolePort.TConsoleProgressed!;

        progressed(_ => true, 0.5);
        progressed(_ => true, 0.5);
        progressed(_ => true, 2);
        progressed(_ => true, 0.25);
        progressed(_ => false, 0.75);
        progressed(_ => true, 0);

        Assert.Equal(["progress 0.5 True", "progress 1 True", "progress 0.25 False", "progress 0 False"],
            tConsoleNotices);
        Assert.Equal(0, tConsoleGate.CConsoleProgress.CConsoleProgressValue);
    }

    [Fact]
    public void ConsoleProgressed_AsksOwnerOfResolvedStation()
    {
        LStation? asked = null;

        tConsolePort.TConsoleProgressed!(station => (asked = station) is not null, 0.5);

        Assert.Same(tConsolePort.TConsoleResolved, asked);
        Assert.Equal(["attach", "resolve"], tConsolePort.TConsoleCalls);
    }

    [Fact]
    public void ConsoleDeferred_TellsOnceUntilUpdate()
    {
        tConsolePort.TConsoleDeferred!();
        tConsolePort.TConsoleDeferred!();
        tConsoleGate.CConsoleUpdate();
        tConsolePort.TConsoleDeferred!();

        Assert.Equal(["deferred", "deferred"], tConsoleNotices);
    }

    [Fact]
    public void ConsoleStationSet_SameStation_IsSilent()
    {
        LStation station = TConsolePort.TConsoleStationCreate();

        tConsoleGate.CConsoleStationSet(station);
        tConsoleGate.CConsoleStationSet(station);
        tConsoleGate.CConsolePause();

        Assert.Equal(["changed"], tConsoleNotices);
        Assert.Same(station, tConsolePort.TConsoleHandles[0]);
    }

    [Fact]
    public void ConsoleStationMove_SingleBoard_IsSilent_OtherwiseHoldsNext()
    {
        tConsoleGate.CConsoleStationMove(1);
        LStation next = TConsolePort.TConsoleStationCreate();
        tConsolePort.TConsoleMoved = next;
        tConsoleGate.CConsoleStationMove(-1);
        tConsoleGate.CConsoleStop();

        Assert.Equal(["changed"], tConsoleNotices);
        Assert.Equal(["attach", "move 1", "move -1", "stop"], tConsolePort.TConsoleCalls);
        Assert.Same(next, tConsolePort.TConsoleHandles[2]);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ConsoleAutoSet_UnsetReadsUnchecked_OnlyChangeTold(bool? chosen, bool auto)
    {
        tConsoleGate.CConsoleAutoSet(chosen);
        tConsolePort.TConsoleAutoChanged = true;
        tConsoleGate.CConsoleAutoSet(chosen);

        Assert.Equal(["attach", "auto " + auto, "auto " + auto], tConsolePort.TConsoleCalls);
        Assert.Equal(["changed"], tConsoleNotices);
    }

    [Fact]
    public void ConsoleCancel_NoneRunning_AsksNothing()
    {
        tConsoleGate.CConsoleCancel(tConsolePort);

        Assert.Equal(["attach", "cancel"], tConsolePort.TConsoleCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConsoleCancel_OneRunning_NamesOutput_CancelsOnlyWhenConfirmed(bool confirmed)
    {
        tConsolePort.TConsoleIds = [Guid.NewGuid()];
        tConsolePort.TConsoleName = "out.mkv";
        tConsolePort.TConsoleConfirmed = confirmed;

        tConsoleGate.CConsoleCancel(tConsolePort);

        Assert.Equal(
            confirmed ? new[] { "attach", "cancel", "confirm", "cancel 1" } : new[] { "attach", "cancel", "confirm" },
            tConsolePort.TConsoleCalls);
        Assert.Equal(
            ["confirm", "Console.Cancel.Confirm", "Console.Cancel.SingleSubject", "out.mkv", "Terms.Stop",
                "Console.Cancel.Title", "Console.Cancel.Keep"],
            tConsolePort.TConsoleAsks[0]);
    }

    [Fact]
    public void ConsoleCancel_SeveralRunning_CountsThem()
    {
        tConsolePort.TConsoleIds = [Guid.NewGuid(), Guid.NewGuid()];

        tConsoleGate.CConsoleCancel(tConsolePort);

        Assert.Equal("Console.Cancel.MultipleSubject", tConsolePort.TConsoleAsks[0][2]);
        Assert.Equal(2, tConsolePort.TConsoleAsks[0][3]);
    }

    [Fact]
    public void ConsoleRemove_EveryRecordGoneIsSilent_HeldAndBlockedWordPartial()
    {
        tConsolePort.TConsoleRemoval = (2, 0, 0);
        Assert.Null(tConsoleGate.CConsoleRemove());
        tConsolePort.TConsoleRemoval = (5, 1, 2);
        CConsoleText both = tConsoleGate.CConsoleRemove()!;
        tConsolePort.TConsoleRemoval = (3, 0, 1);
        CConsoleText blocked = tConsoleGate.CConsoleRemove()!;
        tConsolePort.TConsoleRemoval = (3, 2, 0);
        CConsoleText held = tConsoleGate.CConsoleRemove()!;

        TConsoleTextCheck(
            TConsoleTextCreate(
                "Console.Remove.Partial", 2, 5, TConsoleTextCreate("Console.Remove.HeldAndBlocked", 1, 2)),
            both);
        TConsoleTextCheck(
            TConsoleTextCreate("Console.Remove.Partial", 2, 3, TConsoleTextCreate("Console.Remove.Blocked", 1)),
            blocked);
        TConsoleTextCheck(
            TConsoleTextCreate("Console.Remove.Partial", 1, 3, TConsoleTextCreate("Console.Remove.Held", 2)), held);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConsoleDoneClear_DestructiveAsk_ClearsWhenConfirmed(bool confirmed)
    {
        tConsolePort.TConsoleConfirmed = confirmed;

        tConsoleGate.CConsoleDoneClear(tConsolePort);

        Assert.Equal(confirmed ? new[] { "attach", "destructive", "done" } : new[] { "attach", "destructive" },
            tConsolePort.TConsoleCalls);
        Assert.Equal(
            ["destructive", "Console.ClearDone.Confirm", "Terms.Remove", "Console.Confirm.Title"],
            tConsolePort.TConsoleAsks[0]);
    }

    [Theory]
    [InlineData(true, "confirm", "Console.ClearAll.RunningConfirm", "Terms.Stop")]
    [InlineData(false, "destructive", "Console.ClearAll.Confirm", "Terms.Remove")]
    public async Task ConsoleAllClear_RunningAsksAlways_IdleAsksDestructive(
        bool active, string ask, string question, string action)
    {
        tConsolePort.TConsoleActive = active;
        tConsolePort.TConsoleConfirmed = true;

        await tConsoleGate.CConsoleAllClear(tConsolePort);

        Assert.Equal(["attach", "active", ask, "all"], tConsolePort.TConsoleCalls);
        Assert.Equal([ask, question, action, "Console.Confirm.Title"], tConsolePort.TConsoleAsks[0]);
    }

    [Fact]
    public async Task ConsoleAllClear_Declined_ClearsNothing()
    {
        await tConsoleGate.CConsoleAllClear(tConsolePort);

        Assert.Equal(["attach", "active", "destructive"], tConsolePort.TConsoleCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConsoleTabsClear_AnswersDestructiveAsk(bool confirmed)
    {
        tConsolePort.TConsoleConfirmed = confirmed;

        Assert.Equal(confirmed, tConsoleGate.CConsoleTabsClear(tConsolePort));
        Assert.Equal(
            ["destructive", "Console.ClearTabs.Confirm", "Terms.Remove", "Console.Confirm.Title"],
            tConsolePort.TConsoleAsks[0]);
    }

    [Fact]
    public void ConsoleLoadAndPeriod_ReachPort()
    {
        tConsoleGate.CConsoleLoad();

        Assert.Equal(TimeSpan.FromSeconds(0.5), tConsoleGate.CConsolePeriod);
        Assert.Equal(["attach", "load"], tConsolePort.TConsoleCalls);
    }

    [Fact]
    public void ConsoleCommands_NoEnvoy_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => tConsoleGate.CConsoleCancel(null!));
        Assert.Throws<ArgumentNullException>(() => tConsoleGate.CConsoleDoneClear(null!));
        Assert.Throws<ArgumentNullException>(() => { _ = tConsoleGate.CConsoleAllClear(null!); });
        Assert.Throws<ArgumentNullException>(() => tConsoleGate.CConsoleTabsClear(null!));
    }

    [Fact]
    public void AtelierCreate_NoConsolePort_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => TAtelier.TAtelierCreate(port => port with { LAtelierConsole = null! }));
}
