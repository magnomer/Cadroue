using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRosterDetailRows
{
    private static LRoster TRosterOpen(TSchedule schedule)
    {
        LRoster roster = TInterface.TRosterCreate(
            schedule.TScheduleRead(), TInterface.TStationCreate(schedule.TScheduleRead()));
        TInterface.TRosterRebuild(roster);
        return roster;
    }

    [Fact]
    public void Kind_FollowsCardThenSelectionThenNothing()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(batch, "one");
        schedule.TScheduleAdd(one);
        LRoster roster = TRosterOpen(schedule);

        Assert.Equal(LRosterDetailKind.LRosterDetailNone, TInterface.TRosterKindRead(roster));
        Assert.Empty(TInterface.TRosterDetailRead(roster).LRosterDetailRecords);
        TInterface.TRosterStepSelect(roster, one.TWorkId, false, false);
        Assert.Equal(LRosterDetailKind.LRosterDetailJob, TInterface.TRosterKindRead(roster));
        Assert.NotEmpty(TInterface.TRosterDetailRead(roster).LRosterDetailRecords);
        TInterface.TRosterCardSelect(roster, batch);
        Assert.Equal(LRosterDetailKind.LRosterDetailCard, TInterface.TRosterKindRead(roster));
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Pending_HasNoFailureNoMeterNoBar_AndSevenRecordRows()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        LRosterDetail detail = TInterface.TRosterDetailCreate(work, "-");

        Assert.Empty(detail.LRosterDetailFailures);
        Assert.Empty(detail.LRosterDetailMeters);
        Assert.Empty(detail.LRosterDetailBars);
        Assert.Equal(7, detail.LRosterDetailRecords.Count);
        Assert.All(
            detail.LRosterDetailRecords, row => Assert.Equal(LRosterDetail.LRosterValuePlain, row.LRosterDetailKey));
        Assert.Equal("-", detail.LRosterDetailRecords[4].LRosterDetailValue);
        Assert.Equal(TInterface.TRosterStampFormat(null), detail.LRosterDetailRecords[1].LRosterDetailValue);
        Assert.Equal(work.LWorkSourcePath, detail.LRosterDetailSource);
        Assert.Equal(new[] { work.LWorkSourcePath }, detail.LRosterDetailSources);
        Assert.Equal(new[] { work.LWorkOutputPath }, detail.LRosterDetailOutputs);
        Assert.False(detail.LRosterDetailSound);
        Assert.Empty(detail.LRosterDetailSounds);
    }

    [Fact]
    public void Running_ShowsMeterAndBarOnceBothSizesAreKnown()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;
        work.LWorkStateCurrent = LWorkState.LWorkStateRunning;
        work.LWorkStartTime = DateTimeOffset.Now.AddSeconds(-10);
        work.LWorkSourceBytes = 4_000_000;

        LRosterDetail before = TInterface.TRosterDetailCreate(work, "-");
        Assert.Empty(before.LRosterDetailBars);
        Assert.Empty(before.LRosterDetailMeters);

        work.LWorkOutputBytes = 1_000_000;
        LRosterDetail after = TInterface.TRosterDetailCreate(work, "-");
        LRosterBar bar = Assert.Single(after.LRosterDetailBars);
        Assert.False(bar.LRosterBarOver);
        Assert.Equal("25%", bar.LRosterBarPercent);
        Assert.Equal(LRosterDetail.LRosterPercentUnder, bar.LRosterBarKey);
        string meter = Assert.Single(after.LRosterDetailMeters);
        Assert.Contains(" / ", meter);
        Assert.Contains("MiB/s", meter);
    }

    [Fact]
    public void Done_WithLargerOutput_MarksBarOver()
    {
        IReadOnlyList<LRosterBar> bars = TInterface.TRosterBarsCreate(1000, 2000);
        LRosterBar bar = Assert.Single(bars);
        Assert.True(bar.LRosterBarOver);
        Assert.Equal("200%", bar.LRosterBarPercent);
        Assert.Equal(LRosterDetail.LRosterPercentOver, bar.LRosterBarKey);
        Assert.Equal(1000, bar.LRosterBarRest);
        Assert.Equal(1000, bar.LRosterBarMark);
        Assert.Equal(
            LRosterDetail.LRosterPercentEqual, Assert.Single(TInterface.TRosterBarsCreate(5, 5)).LRosterBarKey);
        Assert.Empty(TInterface.TRosterBarsCreate(0, 5));
        Assert.Empty(TInterface.TRosterBarsCreate(5, null));
    }

    [Fact]
    public void Failed_PutsTheReasonFirst_AndKeepsItOutOfTheMessageRow()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;
        work.LWorkStateCurrent = LWorkState.LWorkStateFailed;
        work.LWorkMessage = "boom";

        LRosterDetail detail = TInterface.TRosterDetailCreate(work, "-");
        LRosterDetailRow failure = Assert.Single(detail.LRosterDetailFailures);
        Assert.Equal("boom", failure.LRosterDetailValue);
        Assert.Equal(LRosterDetail.LRosterValueFail, failure.LRosterDetailKey);
        Assert.Equal(7, detail.LRosterDetailRecords.Count);

        work.LWorkStateCurrent = LWorkState.LWorkStateDone;
        LRosterDetail done = TInterface.TRosterDetailCreate(work, "-");
        Assert.Empty(done.LRosterDetailFailures);
        Assert.Equal(8, done.LRosterDetailRecords.Count);
        Assert.Equal("boom", done.LRosterDetailRecords[7].LRosterDetailValue);
    }

    [Fact]
    public void Encoding_ListsEncoderRowsOnlyWhenReencoding()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        LRosterDetail detail = TInterface.TRosterDetailCreate(work, "-");
        Assert.Equal(7, detail.LRosterDetailVideos.Count);
        Assert.Equal("Encode (Include)", detail.LRosterDetailVideos[0].LRosterDetailValue);
        Assert.Equal("-profile:v high", detail.LRosterDetailVideos[6].LRosterDetailValue);
        Assert.Equal(8, detail.LRosterDetailAudios.Count);
        Assert.Equal("Stereo", detail.LRosterDetailAudios[7].LRosterDetailValue);

        Assert.False(TInterface.TRosterReencodeCheck("copy"));
        Assert.False(TInterface.TRosterReencodeCheck("Exclude"));
        Assert.True(TInterface.TRosterReencodeCheck("Encode"));
    }

    [Fact]
    public void Formats_RenderSizesClocksStampsAndContainers()
    {
        Assert.Equal("1 MiB", TInterface.TRosterMebiFormat(1_048_576));
        Assert.Equal("2 GiB", TInterface.TRosterMebiFormat(2L * 1024 * 1_048_576));
        Assert.NotEqual("0 MiB", TInterface.TRosterMebiFormat(null));
        Assert.Equal("0:05", TInterface.TRosterClockFormat(TimeSpan.FromSeconds(5)));
        Assert.Equal("1:02:03", TInterface.TRosterClockFormat(new TimeSpan(1, 2, 3)));
        Assert.Equal("00:01", TInterface.TRosterElapsedFormat(TimeSpan.FromMilliseconds(10)));
        Assert.Equal("1:00:00", TInterface.TRosterElapsedFormat(TimeSpan.FromHours(1)));
        Assert.Equal(
            "2024-01-02 03:04:05",
            TInterface.TRosterStampFormat(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        Assert.Equal("MP4", TInterface.TRosterContainerFormat(@"C:\clip.mp4"));
        Assert.NotEqual(string.Empty, TInterface.TRosterContainerFormat("noext"));
    }
}
