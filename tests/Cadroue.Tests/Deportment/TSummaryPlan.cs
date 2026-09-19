using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TSummaryPlan
{
    [Fact]
    public void Files_ReadOneOrMany()
    {
        Assert.NotEqual(TInterface.TSummaryFilesFormat(1), TInterface.TSummaryFilesFormat(2));
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

        LSummary summary = TInterface.TSummaryRead([one, two]);

        Assert.Empty(summary.LSummaryMeters);
        Assert.Empty(summary.LSummaryBars);
        Assert.Equal(2, summary.LSummaryCompares.Count);
        Assert.Equal(LRosterDetail.LRosterLineHead, summary.LSummaryCompares[0].LRosterCompareSource.LRosterLineKey);
        Assert.Equal("3 MiB", summary.LSummaryCompares[1].LRosterCompareSource.LRosterLineText);
        Assert.Equal(
            TInterface.TRosterMebiFormat(null), summary.LSummaryCompares[1].LRosterCompareOutput.LRosterLineText);
        Assert.Equal(new[] { one.LWorkSourcePath, two.LWorkSourcePath }, summary.LSummarySources);
        Assert.Equal(new[] { one.LWorkOutputPath, two.LWorkOutputPath }, summary.LSummaryOutputs);
        Assert.Equal(TInterface.TSummaryFilesFormat(2), summary.LSummarySourceCount);
        Assert.Equal(TInterface.TSummaryFilesFormat(2), summary.LSummaryOutputCount);
    }

    [Fact]
    public void Read_WithDoneOutputs_SumsSpentSizesAndSkipsConsumedStages()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        first.TWorkItem.LWorkSourceBytes = 4_194_304;
        first.TWorkItem.LWorkOutputBytes = 3_145_728;
        first.TWorkItem.LWorkStateCurrent = LWorkState.LWorkStateDone;
        first.TWorkItem.LWorkStartTime = DateTimeOffset.UnixEpoch;
        first.TWorkItem.LWorkFinishTime = DateTimeOffset.UnixEpoch.AddSeconds(2);
        second.TWorkItem.LWorkSourceBytes = 3_145_728;
        second.TWorkItem.LWorkOutputBytes = 1_048_576;
        second.TWorkItem.LWorkStateCurrent = LWorkState.LWorkStateDone;
        second.TWorkItem.LWorkStartTime = DateTimeOffset.UnixEpoch;
        second.TWorkItem.LWorkFinishTime = DateTimeOffset.UnixEpoch.AddSeconds(2);
        LWorkItem[] items = [first.TWorkItem, second.TWorkItem];

        (long? source, long? output) = TInterface.TSummarySizeRead(items);
        Assert.Equal(4_194_304, source);
        Assert.Equal(1_048_576, output);

        (IReadOnlyList<string> sources, IReadOnlyList<string> outputs) = TInterface.TSummaryPathsRead(items);
        Assert.Equal(new[] { first.TWorkItem.LWorkSourcePath }, sources);
        Assert.Equal(new[] { second.TWorkItem.LWorkOutputPath }, outputs);

        LSummary summary = TInterface.TSummaryRead(items);
        Assert.Equal("00:04 / 0.25 MiB/s", Assert.Single(summary.LSummaryMeters));
        LRosterBar bar = Assert.Single(summary.LSummaryBars);
        Assert.Equal("25%", bar.LRosterBarPercent);
        Assert.Equal("4 MiB", summary.LSummaryCompares[1].LRosterCompareSource.LRosterLineText);
        Assert.Equal("1 MiB", summary.LSummaryCompares[1].LRosterCompareOutput.LRosterLineText);
    }

    [Fact]
    public void Read_IgnoresAnUnfinishedOutput_AndReportsUnknownSourceWhenAnyIsUnmeasured()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        LWorkItem one = schedule.TWorkCreate(batch, "one").TWorkItem;
        LWorkItem two = schedule.TWorkCreate(batch, "two").TWorkItem;
        one.LWorkSourceBytes = 1_048_576;
        one.LWorkOutputBytes = 512;
        one.LWorkStateCurrent = LWorkState.LWorkStateRunning;

        (long? source, long? output) = TInterface.TSummarySizeRead([one, two]);
        Assert.Null(source);
        Assert.Null(output);
        Assert.Null(TInterface.TSummaryMeterFormat([one, two], null));
    }
}
