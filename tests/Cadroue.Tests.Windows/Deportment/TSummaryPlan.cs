using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TSummaryPlan
{
    [Fact]
    public void Files_ReadOneOrMany()
    {
        Assert.NotEqual(TInterfaceRoster.TSummaryFilesFormat(1), TInterfaceRoster.TSummaryFilesFormat(2));
    }

    [Fact]
    public void Read_WithoutDoneOutputs_HasNoMeterNoBar_AndListsEveryPath()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        LWorkItem one = schedule.TWorkCreate(batch, "one").TWorkItem;
        LWorkItem two = schedule.TWorkCreate(batch, "two").TWorkItem;
        one.LWorkSourceBytes = 1_048_576;
        two.LWorkSourceBytes = 2_097_152;
        schedule.TScheduleAdd(new TScheduleWork(one), new TScheduleWork(two));

        QSummary summary = TInterfaceRoster.TSummaryRead(schedule.TScheduleRead(), batch);

        Assert.Empty(summary.QSummaryMeters);
        Assert.Empty(summary.QSummaryBars);
        Assert.Equal(2, summary.QSummaryCompares.Count);
        Assert.Equal(QRosterDetail.QRosterLineHead, summary.QSummaryCompares[0].QRosterCompareSource.QRosterLineKey);
        Assert.Equal("3 MiB", summary.QSummaryCompares[1].QRosterCompareSource.QRosterLineText);
        Assert.Equal(
            TInterfaceRoster.TRosterMebiFormat(null), summary.QSummaryCompares[1].QRosterCompareOutput.QRosterLineText);
        Assert.Equal(new[] { one.LWorkSourcePath, two.LWorkSourcePath }, summary.QSummarySources);
        Assert.Equal(new[] { one.LWorkOutputPath, two.LWorkOutputPath }, summary.QSummaryOutputs);
        Assert.Equal(TInterfaceRoster.TSummaryFilesFormat(2), summary.QSummarySourceCount);
        Assert.Equal(TInterfaceRoster.TSummaryFilesFormat(2), summary.QSummaryOutputCount);
    }

    [Fact]
    public void Read_WithDoneOutputs_SumsSpentSizesAndSkipsConsumedStages()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        first.TWorkItem.LWorkSourceBytes = 4_194_304;
        first.TWorkItem.LWorkRunState.LWorkOutputBytes = 3_145_728;
        first.TWorkItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateDone;
        first.TWorkItem.LWorkRunState.LWorkStartTime = DateTimeOffset.UnixEpoch;
        first.TWorkItem.LWorkRunState.LWorkFinishTime = DateTimeOffset.UnixEpoch.AddSeconds(2);
        second.TWorkItem.LWorkSourceBytes = 3_145_728;
        second.TWorkItem.LWorkRunState.LWorkOutputBytes = 1_048_576;
        second.TWorkItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateDone;
        second.TWorkItem.LWorkRunState.LWorkStartTime = DateTimeOffset.UnixEpoch;
        second.TWorkItem.LWorkRunState.LWorkFinishTime = DateTimeOffset.UnixEpoch.AddSeconds(2);
        schedule.TScheduleAdd(first, second);

        QSummary summary = TInterfaceRoster.TSummaryRead(schedule.TScheduleRead(), batch);
        Assert.Equal(new[] { first.TWorkItem.LWorkSourcePath }, summary.QSummarySources);
        Assert.Equal(new[] { second.TWorkItem.LWorkOutputPath }, summary.QSummaryOutputs);
        Assert.Equal("00:04 / 0.25 MiB/s", Assert.Single(summary.QSummaryMeters));
        QRosterBar bar = Assert.Single(summary.QSummaryBars);
        Assert.Equal("25%", bar.QRosterBarPercent);
        Assert.Equal("4 MiB", summary.QSummaryCompares[1].QRosterCompareSource.QRosterLineText);
        Assert.Equal("1 MiB", summary.QSummaryCompares[1].QRosterCompareOutput.QRosterLineText);
    }

    [Fact]
    public void Read_IgnoresAnUnfinishedOutput_AndReportsUnknownSourceWhenAnyIsUnmeasured()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        LWorkItem one = schedule.TWorkCreate(batch, "one").TWorkItem;
        LWorkItem two = schedule.TWorkCreate(batch, "two").TWorkItem;
        one.LWorkSourceBytes = 1_048_576;
        one.LWorkRunState.LWorkOutputBytes = 512;
        one.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateRunning;
        schedule.TScheduleAdd(new TScheduleWork(one), new TScheduleWork(two));

        QSummary summary = TInterfaceRoster.TSummaryRead(schedule.TScheduleRead(), batch);
        QRosterCompareRow sizes = summary.QSummaryCompares[1];
        Assert.Equal(TInterfaceRoster.TRosterMebiFormat(null), sizes.QRosterCompareSource.QRosterLineText);
        Assert.Equal(TInterfaceRoster.TRosterMebiFormat(null), sizes.QRosterCompareOutput.QRosterLineText);
        Assert.Empty(summary.QSummaryMeters);
        Assert.Null(TInterfaceRoster.TSummaryMeterFormat(null, null));
    }

    [Fact]
    public void Read_KeepsAnItemHiddenFromTheRoster_InTheBatchSourcesAndSizes()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(batch, "one");
        TScheduleWork two = schedule.TWorkCreate(batch, "two");
        one.TWorkItem.LWorkSourceBytes = 1_048_576;
        two.TWorkItem.LWorkSourceBytes = 2_097_152;
        var other = TInterfaceRoster.TStationCreate(schedule.TScheduleRead());
        two.TWorkItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateRunning;
        two.TWorkItem.LWorkRunState.LWorkOwnerRunner = TInterfaceRoster.TStationRunnerRead(other);
        two.TWorkItem.LWorkRunState.LWorkOwnerProcess = Environment.ProcessId;
        schedule.TScheduleAdd(one, two);

        QSummary summary = TInterfaceRoster.TSummaryRead(schedule.TScheduleRead(), batch);
        IReadOnlyList<Guid> shown = TInterfaceRoster.TRosterIdsRead(schedule.TScheduleRead());
        TInterfaceRoster.TStationClose(other);

        Assert.Equal(new[] { one.TWorkId }, shown);
        Assert.Equal(new[] { one.TWorkItem.LWorkSourcePath, two.TWorkItem.LWorkSourcePath }, summary.QSummarySources);
        Assert.Equal(new[] { one.TWorkItem.LWorkOutputPath, two.TWorkItem.LWorkOutputPath }, summary.QSummaryOutputs);
        Assert.Equal("3 MiB", summary.QSummaryCompares[1].QRosterCompareSource.QRosterLineText);
    }

    [Fact]
    public void Read_SharedSourcePath_ListsItOnce_AndTheFirstItemBytesWin()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TScheduleWork second = schedule.TWorkDeriveCreate(first, "two");
        first.TWorkItem.LWorkSourceBytes = 1_048_576;
        second.TWorkItem.LWorkSourceBytes = 2_097_152;
        schedule.TScheduleAdd(first, second);

        QSummary summary = TInterfaceRoster.TSummaryRead(schedule.TScheduleRead(), batch);

        Assert.Equal(new[] { first.TWorkItem.LWorkSourcePath }, summary.QSummarySources);
        Assert.Equal(
            new[] { first.TWorkItem.LWorkOutputPath, second.TWorkItem.LWorkOutputPath }, summary.QSummaryOutputs);
        Assert.Equal("1 MiB", summary.QSummaryCompares[1].QRosterCompareSource.QRosterLineText);
        Assert.Equal(
            TInterfaceRoster.TRosterMebiFormat(123), summary.QSummaryCompares[1].QRosterCompareOutput.QRosterLineText);
    }
}
