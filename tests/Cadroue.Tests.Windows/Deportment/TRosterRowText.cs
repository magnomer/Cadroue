using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterRowText
{
    [Theory]
    [InlineData(LWorkState.LWorkStatePending, "Pending")]
    [InlineData(LWorkState.LWorkStateRunning, "Running")]
    [InlineData(LWorkState.LWorkStateDone, "Done")]
    [InlineData(LWorkState.LWorkStateFailed, "Failed")]
    [InlineData(LWorkState.LWorkStateUnresolved, "Unresolved")]
    [InlineData(LWorkState.LWorkStatePartial, "Partial")]
    [InlineData(LWorkState.LWorkStateBlocked, "Blocked")]
    [InlineData(LWorkState.LWorkStateCancelled, "Cancelled")]
    public void Key_NamesEveryState(LWorkState state, string expected)
    {
        Assert.Equal(expected, TInterfaceRoster.TRosterKeyRead(state));
        Assert.NotEmpty(TInterfaceRoster.TRosterStateFormat(state));
    }

    [Fact]
    public void Progress_ShowsAPercentOnlyWhileRunningOrStarted()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        Assert.Equal("-", TInterfaceRoster.TRosterProgressFormat(work));
        work.LWorkStateCurrent = LWorkState.LWorkStateRunning;
        Assert.StartsWith("0", TInterfaceRoster.TRosterProgressFormat(work));
        work.LWorkStateCurrent = LWorkState.LWorkStateFailed;
        work.LWorkProgress = 0.5;
        Assert.StartsWith("50", TInterfaceRoster.TRosterProgressFormat(work));
    }

    [Fact]
    public void Owner_DistinguishesNoneThisTabOtherTabAndOtherWindow()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        Assert.Equal("-", TInterfaceRoster.TRosterOwnerFormat(work, true));
        work.LWorkOwnerRunner = Guid.NewGuid();
        work.LWorkOwnerProcess = Environment.ProcessId;
        string thisTab = TInterfaceRoster.TRosterOwnerFormat(work, true);
        string otherTab = TInterfaceRoster.TRosterOwnerFormat(work, false);
        work.LWorkOwnerProcess = Environment.ProcessId + 1;
        string otherWindow = TInterfaceRoster.TRosterOwnerFormat(work, false);
        Assert.NotEqual(thisTab, otherTab);
        Assert.NotEqual(otherTab, otherWindow);
    }

    [Fact]
    public void Priority_Span_AndPhase_Format()
    {
        Assert.NotEqual(
            TInterfaceRoster.TRosterPriorityFormat(LWorkPriority.LWorkPriorityHigh),
            TInterfaceRoster.TRosterPriorityFormat(LWorkPriority.LWorkPriorityNormal));
        Assert.Equal("01:02:03", TInterfaceRoster.TRosterSpanFormat(new TimeSpan(1, 2, 3)));
        Assert.Equal(
            TInterfaceRoster.TRosterStateFormat(LWorkState.LWorkStateDone),
            TInterfaceRoster.TRosterPhaseFormat(LWorkState.LWorkStateDone, LWorkPhase.LWorkPhaseEncoding));
        Assert.NotEqual(
            TInterfaceRoster.TRosterPhaseFormat(LWorkState.LWorkStateRunning, LWorkPhase.LWorkPhaseEncoding),
            TInterfaceRoster.TRosterPhaseFormat(LWorkState.LWorkStateRunning, LWorkPhase.LWorkPhaseStarted));
    }

    [Theory]
    [InlineData(true, false, false, LRosterRow.LRosterShadeSelected)]
    [InlineData(true, true, true, LRosterRow.LRosterShadeSelected)]
    [InlineData(false, false, true, LRosterRow.LRosterShadeStage)]
    [InlineData(false, true, true, LRosterRow.LRosterShadePlain)]
    [InlineData(false, false, false, LRosterRow.LRosterShadePlain)]
    public void Shade_PrefersSelectionThenStageOutsideASelectedCard(
        bool selected, bool cardSelected, bool stage, string expected) =>
        Assert.Equal(expected, TInterfaceRoster.TRosterShadeResolve(selected, cardSelected, stage));

    [Fact]
    public void Row_CarriesEveryCellFromTheItem()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork created = schedule.TWorkCreate(batch, "one", LWorkPriority.LWorkPriorityHigh);
        LWorkItem work = created.TWorkItem;
        work.LWorkStateCurrent = LWorkState.LWorkStateDone;
        work.LWorkSourceBytes = 200;
        work.LWorkOutputBytes = 100;
        schedule.TScheduleAdd(created);
        LRosterRow row = TInterfaceRoster.TRosterRowCreate(
            TInterfaceRoster.TRosterLineageRead(schedule.TScheduleRead(), [work]),
            work,
            false,
            true,
            true,
            LRosterRow.LRosterShadeStage);

        Assert.Equal(work.LWorkId, row.LRosterRowId);
        Assert.Equal("Done", row.LRosterRowKey);
        Assert.Equal(TInterfaceRoster.TRosterPriorityFormat(LWorkPriority.LWorkPriorityHigh), row.LRosterRowPriority);
        Assert.Equal("00:00:01", row.LRosterRowLength);
        Assert.StartsWith("50", row.LRosterRowRatio);
        Assert.Equal("-", row.LRosterRowOwner);
        Assert.False(row.LRosterRowLast);
        Assert.True(row.LRosterRowStage);
        Assert.Equal(LRosterRow.LRosterShadeStage, row.LRosterRowShade);
        Assert.NotEmpty(row.LRosterRowStep);
    }
}
