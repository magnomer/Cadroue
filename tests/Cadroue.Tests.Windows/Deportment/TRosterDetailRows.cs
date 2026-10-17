using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterDetailRows
{
    private static QRoster TRosterOpen(TSchedule schedule)
    {
        QRoster roster = TInterfaceRoster.TRosterCreate(
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
        QRoster roster = TRosterOpen(schedule);

        Assert.Equal(QRosterDetailKind.QRosterDetailNone, TInterfaceRoster.TRosterKindRead(roster));
        Assert.Empty(TInterfaceRoster.TRosterDetailRead(roster).QRosterDetailRecords);
        TInterfaceRoster.TRosterStepSelect(roster, one.TWorkId, false, false);
        Assert.Equal(QRosterDetailKind.QRosterDetailJob, TInterfaceRoster.TRosterKindRead(roster));
        Assert.NotEmpty(TInterfaceRoster.TRosterDetailRead(roster).QRosterDetailRecords);
        TInterfaceRoster.TRosterCardSelect(roster, batch);
        Assert.Equal(QRosterDetailKind.QRosterDetailCard, TInterfaceRoster.TRosterKindRead(roster));
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Pending_HasNoFailureNoMeterNoBar_AndSevenRecordRows()
    {
        using var schedule = new TSchedule();
        TScheduleWork added = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(added);
        LWorkItem work = added.TWorkItem;

        QRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);

        Assert.Empty(detail.QRosterDetailFailures);
        Assert.Empty(detail.QRosterDetailMeters);
        Assert.Empty(detail.QRosterDetailBars);
        Assert.Equal(7, detail.QRosterDetailRecords.Count);
        Assert.All(
            detail.QRosterDetailRecords, row => Assert.Equal(QRosterDetail.QRosterValuePlain, row.QRosterDetailKey));
        Assert.Equal("-", detail.QRosterDetailRecords[4].QRosterDetailValue);
        Assert.Equal(TInterfaceRoster.TRosterStampFormat(null), detail.QRosterDetailRecords[1].QRosterDetailValue);
        Assert.Equal(work.LWorkSourcePath, detail.QRosterDetailSource);
        Assert.Equal(new[] { work.LWorkSourcePath }, detail.QRosterDetailSources);
        Assert.Equal(new[] { work.LWorkOutputPath }, detail.QRosterDetailOutputs);
        Assert.False(detail.QRosterDetailSound);
        Assert.Empty(detail.QRosterDetailSounds);
    }

    [Fact]
    public void Running_ShowsMeterAndBarOnceBothSizesAreKnown()
    {
        using var schedule = new TSchedule();
        TScheduleWork added = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(added);
        LWorkItem work = added.TWorkItem;
        work.LWorkStateCurrent = LWorkState.LWorkStateRunning;
        work.LWorkStartTime = DateTimeOffset.Now.AddSeconds(-10);
        work.LWorkSourceBytes = 4_000_000;

        QRosterDetail before = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        Assert.Empty(before.QRosterDetailBars);
        Assert.Empty(before.QRosterDetailMeters);

        work.LWorkOutputBytes = 1_000_000;
        QRosterDetail after = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        QRosterBar bar = Assert.Single(after.QRosterDetailBars);
        Assert.False(bar.QRosterBarOver);
        Assert.Equal("25%", bar.QRosterBarPercent);
        Assert.Equal(QRosterDetail.QRosterPercentUnder, bar.QRosterBarKey);
        string meter = Assert.Single(after.QRosterDetailMeters);
        Assert.Contains(" / ", meter);
        Assert.Contains("MiB/s", meter);
    }

    [Fact]
    public void Done_WithLargerOutput_MarksBarOver()
    {
        IReadOnlyList<QRosterBar> bars = TInterfaceRoster.TRosterBarsCreate(1000, 2000);
        QRosterBar bar = Assert.Single(bars);
        Assert.True(bar.QRosterBarOver);
        Assert.Equal("200%", bar.QRosterBarPercent);
        Assert.Equal(QRosterDetail.QRosterPercentOver, bar.QRosterBarKey);
        Assert.Equal(1000, bar.QRosterBarRest);
        Assert.Equal(1000, bar.QRosterBarMark);
        Assert.Equal(
            QRosterDetail.QRosterPercentEqual, Assert.Single(TInterfaceRoster.TRosterBarsCreate(5, 5)).QRosterBarKey);
        Assert.Empty(TInterfaceRoster.TRosterBarsCreate(0, 5));
        Assert.Empty(TInterfaceRoster.TRosterBarsCreate(5, null));
    }

    [Fact]
    public void Failed_PutsTheReasonFirst_AndKeepsItOutOfTheMessageRow()
    {
        using var schedule = new TSchedule();
        TScheduleWork added = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(added);
        LWorkItem work = added.TWorkItem;
        work.LWorkStateCurrent = LWorkState.LWorkStateFailed;
        work.LWorkMessage = "boom";

        QRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        QRosterDetailRow failure = Assert.Single(detail.QRosterDetailFailures);
        Assert.Equal("boom", failure.QRosterDetailValue);
        Assert.Equal(QRosterDetail.QRosterValueFail, failure.QRosterDetailKey);
        Assert.Equal(7, detail.QRosterDetailRecords.Count);

        work.LWorkStateCurrent = LWorkState.LWorkStateDone;
        QRosterDetail done = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        Assert.Empty(done.QRosterDetailFailures);
        Assert.Equal(8, done.QRosterDetailRecords.Count);
        Assert.Equal("boom", done.QRosterDetailRecords[7].QRosterDetailValue);
    }

    [Fact]
    public void Encoding_ListsEncoderRowsOnlyWhenReencoding()
    {
        using var schedule = new TSchedule();
        TScheduleWork added = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(added);
        LWorkItem work = added.TWorkItem;

        QRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        Assert.Equal(7, detail.QRosterDetailVideos.Count);
        Assert.Equal("Encode (Include)", detail.QRosterDetailVideos[0].QRosterDetailValue);
        Assert.Equal("-profile:v high", detail.QRosterDetailVideos[6].QRosterDetailValue);
        Assert.Equal(8, detail.QRosterDetailAudios.Count);
        Assert.Equal("Stereo", detail.QRosterDetailAudios[7].QRosterDetailValue);

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
    }

    [Fact]
    public void DetailCreate_ContainerRow_ShowsEachExtensionOrUnknown()
    {
        using var schedule = new TSchedule();
        TScheduleWork added = schedule.TWorkCreate(Guid.NewGuid(), "clip");
        schedule.TScheduleAdd(added);
        LWorkItem work = added.TWorkItem;
        TInterfaceRoster.TWorkOutputSet(work, @"C:\clip.mp4");

        QRosterCompareRow container =
            TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work).QRosterDetailCompares[6];
        Assert.Equal("SOURCE", container.QRosterCompareSource.QRosterLineText);
        Assert.Equal("MP4", container.QRosterCompareOutput.QRosterLineText);

        TInterfaceRoster.TWorkOutputSet(work, @"C:\noext");
        QRosterCompareRow bare =
            TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work).QRosterDetailCompares[6];
        Assert.NotEqual(string.Empty, bare.QRosterCompareOutput.QRosterLineText);
        Assert.NotEqual("NOEXT", bare.QRosterCompareOutput.QRosterLineText);
    }

    [Fact]
    public void Detail_TabTitle_FollowsATabRenameAfterTheRebuild()
    {
        using var schedule = new TSchedule();
        QStrip strip = TInterface.TStripCreate(key => key, (title, number) => $"{title} {number}");
        QStripTab tab = TInterface.TStripTabCreate("Split");
        TInterface.TStripAdd(strip, tab);
        TInterfaceRoster.TStripTitleAttach(strip);
        try
        {
            TScheduleWork one = schedule.TWorkCreate(Guid.NewGuid(), "one");
            one.TWorkItem.LWorkRelaySource = tab.QStripTabId;
            schedule.TScheduleAdd(one);
            QRoster roster = TRosterOpen(schedule);
            TInterfaceRoster.TRosterStepSelect(roster, one.TWorkId, false, false);
            Assert.Equal(new[] { "Split" }, TInterfaceRoster.TRosterDetailRead(roster).QRosterDetailTabs);

            Assert.True(TInterface.TStripNameSet(strip, tab, "Renamed"));
            Assert.Equal(new[] { "Renamed" }, TInterfaceRoster.TRosterDetailRead(roster).QRosterDetailTabs);
            TInterfaceRoster.TRosterClose(roster);
        }
        finally
        {
            TInterfaceRoster.TStripTitleAttach(null);
        }
    }
}
