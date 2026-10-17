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
        work.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateRunning;
        Assert.StartsWith("0", TInterfaceRoster.TRosterProgressFormat(work));
        work.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateFailed;
        work.LWorkRunState.LWorkProgress = 0.5;
        Assert.StartsWith("50", TInterfaceRoster.TRosterProgressFormat(work));
    }

    [Fact]
    public void Owner_DistinguishesNoneThisTabOtherTabAndOtherWindow()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        Assert.Equal("-", TInterfaceRoster.TRosterOwnerFormat(work, true));
        work.LWorkRunState.LWorkOwnerRunner = Guid.NewGuid();
        work.LWorkRunState.LWorkOwnerProcess = Environment.ProcessId;
        string thisTab = TInterfaceRoster.TRosterOwnerFormat(work, true);
        string otherTab = TInterfaceRoster.TRosterOwnerFormat(work, false);
        work.LWorkRunState.LWorkOwnerProcess = Environment.ProcessId + 1;
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
    [InlineData(true, false, false, QRosterRow.QRosterShadeSelected)]
    [InlineData(true, true, true, QRosterRow.QRosterShadeSelected)]
    [InlineData(false, false, true, QRosterRow.QRosterShadeStage)]
    [InlineData(false, true, true, QRosterRow.QRosterShadePlain)]
    [InlineData(false, false, false, QRosterRow.QRosterShadePlain)]
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
        work.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateDone;
        work.LWorkSourceBytes = 200;
        work.LWorkRunState.LWorkOutputBytes = 100;
        schedule.TScheduleAdd(created);
        QRosterRow row = TInterfaceRoster.TRosterRowCreate(
            TInterfaceRoster.TRosterLineageRead(schedule.TScheduleRead(), [work]),
            work,
            false,
            true,
            true,
            QRosterRow.QRosterShadeStage);

        Assert.Equal(work.LWorkId, row.QRosterRowId);
        Assert.Equal("Done", row.QRosterRowKey);
        Assert.Equal(TInterfaceRoster.TRosterPriorityFormat(LWorkPriority.LWorkPriorityHigh), row.QRosterRowPriority);
        Assert.Equal("00:00:01", row.QRosterRowLength);
        Assert.StartsWith("50", row.QRosterRowRatio);
        Assert.Equal("-", row.QRosterRowOwner);
        Assert.False(row.QRosterRowLast);
        Assert.True(row.QRosterRowStage);
        Assert.Equal(QRosterRow.QRosterShadeStage, row.QRosterRowShade);
        Assert.NotEmpty(row.QRosterRowStep);
    }
}
