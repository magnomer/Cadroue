using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterDetailRows
{
    private static LRoster TRosterOpen(TSchedule schedule)
    {
        LRoster roster = TInterfaceRoster.TRosterCreate(
            schedule.TScheduleRead(), TInterfaceRoster.TStationCreate(schedule.TScheduleRead()));
        TInterfaceRoster.TRosterRebuild(roster);
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

        Assert.Equal(LRosterDetailKind.LRosterDetailNone, TInterfaceRoster.TRosterKindRead(roster));
        Assert.Empty(TInterfaceRoster.TRosterDetailRead(roster).LRosterDetailRecords);
        TInterfaceRoster.TRosterStepSelect(roster, one.TWorkId, false, false);
        Assert.Equal(LRosterDetailKind.LRosterDetailJob, TInterfaceRoster.TRosterKindRead(roster));
        Assert.NotEmpty(TInterfaceRoster.TRosterDetailRead(roster).LRosterDetailRecords);
        TInterfaceRoster.TRosterCardSelect(roster, batch);
        Assert.Equal(LRosterDetailKind.LRosterDetailCard, TInterfaceRoster.TRosterKindRead(roster));
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Pending_HasNoFailureNoMeterNoBar_AndSevenRecordRows()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        LRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(work, "-");

        Assert.Empty(detail.LRosterDetailFailures);
        Assert.Empty(detail.LRosterDetailMeters);
        Assert.Empty(detail.LRosterDetailBars);
        Assert.Equal(7, detail.LRosterDetailRecords.Count);
        Assert.All(
            detail.LRosterDetailRecords, row => Assert.Equal(LRosterDetail.LRosterValuePlain, row.LRosterDetailKey));
        Assert.Equal("-", detail.LRosterDetailRecords[4].LRosterDetailValue);
        Assert.Equal(TInterfaceRoster.TRosterStampFormat(null), detail.LRosterDetailRecords[1].LRosterDetailValue);
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

        LRosterDetail before = TInterfaceRoster.TRosterDetailCreate(work, "-");
        Assert.Empty(before.LRosterDetailBars);
        Assert.Empty(before.LRosterDetailMeters);

        work.LWorkOutputBytes = 1_000_000;
        LRosterDetail after = TInterfaceRoster.TRosterDetailCreate(work, "-");
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
        IReadOnlyList<LRosterBar> bars = TInterfaceRoster.TRosterBarsCreate(1000, 2000);
        LRosterBar bar = Assert.Single(bars);
        Assert.True(bar.LRosterBarOver);
        Assert.Equal("200%", bar.LRosterBarPercent);
        Assert.Equal(LRosterDetail.LRosterPercentOver, bar.LRosterBarKey);
        Assert.Equal(1000, bar.LRosterBarRest);
        Assert.Equal(1000, bar.LRosterBarMark);
        Assert.Equal(
            LRosterDetail.LRosterPercentEqual, Assert.Single(TInterfaceRoster.TRosterBarsCreate(5, 5)).LRosterBarKey);
        Assert.Empty(TInterfaceRoster.TRosterBarsCreate(0, 5));
        Assert.Empty(TInterfaceRoster.TRosterBarsCreate(5, null));
    }

    [Fact]
    public void Failed_PutsTheReasonFirst_AndKeepsItOutOfTheMessageRow()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;
        work.LWorkStateCurrent = LWorkState.LWorkStateFailed;
        work.LWorkMessage = "boom";

        LRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(work, "-");
        LRosterDetailRow failure = Assert.Single(detail.LRosterDetailFailures);
        Assert.Equal("boom", failure.LRosterDetailValue);
        Assert.Equal(LRosterDetail.LRosterValueFail, failure.LRosterDetailKey);
        Assert.Equal(7, detail.LRosterDetailRecords.Count);

        work.LWorkStateCurrent = LWorkState.LWorkStateDone;
        LRosterDetail done = TInterfaceRoster.TRosterDetailCreate(work, "-");
        Assert.Empty(done.LRosterDetailFailures);
        Assert.Equal(8, done.LRosterDetailRecords.Count);
        Assert.Equal("boom", done.LRosterDetailRecords[7].LRosterDetailValue);
    }

    [Fact]
    public void Encoding_ListsEncoderRowsOnlyWhenReencoding()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        LRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(work, "-");
        Assert.Equal(7, detail.LRosterDetailVideos.Count);
        Assert.Equal("Encode (Include)", detail.LRosterDetailVideos[0].LRosterDetailValue);
        Assert.Equal("-profile:v high", detail.LRosterDetailVideos[6].LRosterDetailValue);
        Assert.Equal(8, detail.LRosterDetailAudios.Count);
        Assert.Equal("Stereo", detail.LRosterDetailAudios[7].LRosterDetailValue);

        Assert.False(TInterfaceRoster.TRosterReencodeCheck("copy"));
        Assert.False(TInterfaceRoster.TRosterReencodeCheck("Exclude"));
        Assert.True(TInterfaceRoster.TRosterReencodeCheck("Encode"));
    }

    [Fact]
    public void Formats_RenderSizesClocksStampsAndContainers()
    {
        Assert.Equal("1 MiB", TInterfaceRoster.TRosterMebiFormat(1_048_576));
        Assert.Equal("2 GiB", TInterfaceRoster.TRosterMebiFormat(2L * 1024 * 1_048_576));
        Assert.NotEqual("0 MiB", TInterfaceRoster.TRosterMebiFormat(null));
        Assert.Equal("0:05", TInterfaceRoster.TRosterClockFormat(TimeSpan.FromSeconds(5)));
        Assert.Equal("1:02:03", TInterfaceRoster.TRosterClockFormat(new TimeSpan(1, 2, 3)));
        Assert.Equal("00:01", TInterfaceRoster.TRosterElapsedFormat(TimeSpan.FromMilliseconds(10)));
        Assert.Equal("1:00:00", TInterfaceRoster.TRosterElapsedFormat(TimeSpan.FromHours(1)));
        Assert.Equal(
            "2024-01-02 03:04:05",
            TInterfaceRoster.TRosterStampFormat(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        Assert.Equal("MP4", TInterfaceRoster.TRosterContainerFormat(@"C:\clip.mp4"));
        Assert.NotEqual(string.Empty, TInterfaceRoster.TRosterContainerFormat("noext"));
    }
}
