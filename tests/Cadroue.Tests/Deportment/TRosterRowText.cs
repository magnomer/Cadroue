using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

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
        Assert.Equal(expected, TInterface.TRosterKeyRead(state));
        Assert.NotEmpty(TInterface.TRosterStateFormat(state));
    }

    [Fact]
    public void Progress_ShowsAPercentOnlyWhileRunningOrStarted()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        Assert.Equal("-", TInterface.TRosterProgressFormat(work));
        work.LWorkStateCurrent = LWorkState.LWorkStateRunning;
        Assert.StartsWith("0", TInterface.TRosterProgressFormat(work));
        work.LWorkStateCurrent = LWorkState.LWorkStateFailed;
        work.LWorkProgress = 0.5;
        Assert.StartsWith("50", TInterface.TRosterProgressFormat(work));
    }

    [Fact]
    public void Owner_DistinguishesNoneThisTabOtherTabAndOtherWindow()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        Assert.Equal("-", TInterface.TRosterOwnerFormat(work, true));
        work.LWorkOwnerRunner = Guid.NewGuid();
        work.LWorkOwnerProcess = Environment.ProcessId;
        string thisTab = TInterface.TRosterOwnerFormat(work, true);
        string otherTab = TInterface.TRosterOwnerFormat(work, false);
        work.LWorkOwnerProcess = Environment.ProcessId + 1;
        string otherWindow = TInterface.TRosterOwnerFormat(work, false);
        Assert.NotEqual(thisTab, otherTab);
        Assert.NotEqual(otherTab, otherWindow);
    }

    [Fact]
    public void Priority_Span_AndPhase_Format()
    {
        Assert.NotEqual(
            TInterface.TRosterPriorityFormat(LWorkPriority.LWorkPriorityHigh),
            TInterface.TRosterPriorityFormat(LWorkPriority.LWorkPriorityNormal));
        Assert.Equal("01:02:03", TInterface.TRosterSpanFormat(new TimeSpan(1, 2, 3)));
        Assert.Equal(
            TInterface.TRosterStateFormat(LWorkState.LWorkStateDone),
            TInterface.TRosterPhaseFormat(LWorkState.LWorkStateDone, LWorkPhase.LWorkPhaseEncoding));
        Assert.NotEqual(
            TInterface.TRosterPhaseFormat(LWorkState.LWorkStateRunning, LWorkPhase.LWorkPhaseEncoding),
            TInterface.TRosterPhaseFormat(LWorkState.LWorkStateRunning, LWorkPhase.LWorkPhaseStarted));
    }

    [Theory]
    [InlineData(true, false, false, LRosterRow.LRosterShadeSelected)]
    [InlineData(true, true, true, LRosterRow.LRosterShadeSelected)]
    [InlineData(false, false, true, LRosterRow.LRosterShadeStage)]
    [InlineData(false, true, true, LRosterRow.LRosterShadePlain)]
    [InlineData(false, false, false, LRosterRow.LRosterShadePlain)]
    public void Shade_PrefersSelectionThenStageOutsideASelectedCard(
        bool selected, bool cardSelected, bool stage, string expected) =>
        Assert.Equal(expected, TInterface.TRosterShadeResolve(selected, cardSelected, stage));

    [Fact]
    public void Row_CarriesEveryCellFromTheItem()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        LWorkItem work = schedule.TWorkCreate(batch, "one", LWorkPriority.LWorkPriorityHigh).TWorkItem;
        work.LWorkStateCurrent = LWorkState.LWorkStateDone;
        work.LWorkSourceBytes = 200;
        work.LWorkOutputBytes = 100;
        LLineageEntry lineage = Assert.Single(TInterface.TLineageRead([work], _ => batch));

        LRosterRow row = TInterface.TRosterRowCreate(work, lineage, false, true, true, LRosterRow.LRosterShadeStage);

        Assert.Equal(work.LWorkId, row.LRosterRowId);
        Assert.Equal("Done", row.LRosterRowKey);
        Assert.Equal(TInterface.TRosterPriorityFormat(LWorkPriority.LWorkPriorityHigh), row.LRosterRowPriority);
        Assert.Equal("00:00:01", row.LRosterRowLength);
        Assert.StartsWith("50", row.LRosterRowRatio);
        Assert.Equal("-", row.LRosterRowOwner);
        Assert.False(row.LRosterRowLast);
        Assert.True(row.LRosterRowStage);
        Assert.Equal(LRosterRow.LRosterShadeStage, row.LRosterRowShade);
        Assert.NotEmpty(row.LRosterRowStep);
    }
}
