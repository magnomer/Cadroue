using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Xunit;

namespace Cadroue.Tests;

public sealed class TRosterDetailGate
{
    private static readonly Guid TRosterWork = Guid.NewGuid();
    private static readonly Guid TRosterBatch = Guid.NewGuid();
    private static readonly Guid TRosterRelay = Guid.NewGuid();

    private static readonly Func<Guid, CLineageStep> TRosterStepUnread =
        _ => throw new InvalidOperationException("Step read.");

    private readonly TRosterDetailPort tRosterDetailPort = new();
    private readonly CRoster tRosterGate;

    public TRosterDetailGate()
    {
        CAtelier atelier = TAtelier.TAtelierCreate(port => port with { LAtelierRosterDetail = tRosterDetailPort });
        tRosterGate = atelier.CAtelierRosterCreate();
    }

    [Fact]
    public void Atelier_NoRosterDetailPort_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TAtelier.TAtelierCreate(port => port with { LAtelierRosterDetail = null! }));
    }

    [Fact]
    public void RosterDetailRead_NoStep_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => tRosterGate.CRosterDetail.CRosterDetailRead(TRosterWork, null!));
    }

    [Fact]
    public void RosterDetailRead_NoStation_AnswersNullWithoutAnEngineCall()
    {
        tRosterDetailPort.TRosterDetailJob = TRosterJobCreate(10);

        Assert.Null(tRosterGate.CRosterDetail.CRosterDetailRead(TRosterWork, TRosterStepUnread));
        Assert.Empty(tRosterDetailPort.TRosterDetailCalls);
    }

    [Fact]
    public void RosterDetailRead_NoItem_AnswersNullWithoutReadingTheStep()
    {
        LStation station = TRosterStationCreate();
        tRosterGate.CRosterAttach(station);

        Assert.Null(tRosterGate.CRosterDetail.CRosterDetailRead(TRosterWork, TRosterStepUnread));
        Assert.Equal([$"{TRosterWork}"], tRosterDetailPort.TRosterDetailCalls);
        Assert.Same(station, tRosterDetailPort.TRosterDetailStation);
    }

    [Fact]
    public void RosterDetailRead_Item_PlacesItsTabFromTheStepInOneEngineCall()
    {
        tRosterGate.CRosterAttach(TRosterStationCreate());
        tRosterDetailPort.TRosterDetailJob = TRosterJobCreate(10);
        var asked = new List<Guid>();

        CRosterJob? job = tRosterGate.CRosterDetail.CRosterDetailRead(TRosterWork, id =>
        {
            asked.Add(id);
            return TRosterStepCreate(40);
        });

        Assert.Equal([TRosterWork], asked);
        Assert.Equal([$"{TRosterWork}:{TRosterBatch}:{TRosterRelay}:Tab"], tRosterDetailPort.TRosterDetailCalls);
        Assert.NotNull(job);
        Assert.Equal(40, job.CRosterJobSource.CRosterSideBytes);
        Assert.Equal(10, job.CRosterJobOutput.CRosterSideBytes);
        Assert.Equal("mkv", job.CRosterJobSource.CRosterSideExtension);
        Assert.Equal("mp4", job.CRosterJobOutput.CRosterSideExtension);
        Assert.Equal(TimeSpan.FromSeconds(3), job.CRosterJobSpent);
    }

    [Fact]
    public void RosterDetailRead_Item_MapsEveryFactByName()
    {
        tRosterGate.CRosterAttach(TRosterStationCreate());
        tRosterDetailPort.TRosterDetailJob = TRosterJobCreate(10);

        CRosterJob job = tRosterGate.CRosterDetail.CRosterDetailRead(TRosterWork, _ => TRosterStepCreate(40))!;

        Assert.Equal("Broke", job.CRosterJobFailure);
        Assert.Null(job.CRosterJobMessage);
        Assert.Equal("Title", job.CRosterJobTab);
        Assert.True(job.CRosterJobMeasured);
        Assert.True(job.CRosterJobSound);
        Assert.Equal(["a.mkv"], job.CRosterJobSources);
        Assert.Equal(2, job.CRosterJobAttempts);
        Assert.Equal("OtherTab", job.CRosterJobOwner);
        Assert.Equal("Processing", job.CRosterJobState);
        Assert.True(job.CRosterJobHigh);
        Assert.True(job.CRosterJobSource.CRosterSideVideo);
        Assert.Equal(1920, job.CRosterJobSource.CRosterSideWidth);
        Assert.Equal(2.5, job.CRosterJobSource.CRosterSideKeyframe);
        Assert.Equal("aac", job.CRosterJobSource.CRosterSideCodec);
        Assert.Equal(128, job.CRosterJobSource.CRosterSideKilobits);
        Assert.Equal(48000, job.CRosterJobSource.CRosterSideSamplerate);
        Assert.Equal("a.mkv", job.CRosterJobSource.CRosterSidePath);
        Assert.False(job.CRosterJobOutput.CRosterSideMedia);
        Assert.Equal("b.mp4", job.CRosterJobOutput.CRosterSidePath);
        Assert.Equal("x264", job.CRosterJobVideo.CRosterEncodingEncoder);
        Assert.True(job.CRosterJobVideo.CRosterEncodingReencode);
        Assert.Equal("1920x1080", job.CRosterJobVideo.CRosterEncodingSize);
        Assert.False(job.CRosterJobAudio.CRosterEncodingReencode);
        Assert.Null(job.CRosterJobAudio.CRosterEncodingSpeed);
    }

    [Theory]
    [InlineData(-1L, 10L, null)]
    [InlineData(0L, 10L, 0L)]
    [InlineData(40L, null, 40L)]
    public void RosterDetailRead_NoSizeBar_HidesTheSpentTime(long source, long? output, long? shown)
    {
        tRosterGate.CRosterAttach(TRosterStationCreate());
        tRosterDetailPort.TRosterDetailJob = TRosterJobCreate(output);

        CRosterJob job = tRosterGate.CRosterDetail.CRosterDetailRead(TRosterWork, _ => TRosterStepCreate(source))!;

        Assert.Null(job.CRosterJobSpent);
        Assert.Equal(shown, job.CRosterJobSource.CRosterSideBytes);
    }

    private static LStation TRosterStationCreate() => (LStation)RuntimeHelpers.GetUninitializedObject(typeof(LStation));

    private static CLineageStep TRosterStepCreate(long source) =>
        new(TRosterWork, TRosterBatch, TRosterRelay, "Tab", "Edit", true, null, source, null, "mkv", "mp4");

    private static LRosterJob TRosterJobCreate(long? output) =>
        new(
            "Broke",
            null,
            "Title",
            TimeSpan.FromSeconds(3),
            true,
            output,
            new LRosterSide(true, true, 1920, 1080, 25, 2.5, TimeSpan.FromMinutes(1), "aac", 128, 48000, -14, "a.mkv"),
            new LRosterSide(false, false, 0, 0, 0, null, TimeSpan.Zero, null, null, null, null, "b.mp4"),
            true,
            ["a.mkv"],
            DateTimeOffset.UnixEpoch,
            null,
            null,
            2,
            "OtherTab",
            "Processing",
            true,
            new LRosterEncoding(
                "Encode", "0", true, "x264", "crf", "23", "medium", "yuv420p", "1920x1080", "25", null, "", ""),
            new LRosterEncoding("Copy", "1", false, "", "", "", null, "", "", "", null, "48000", "2"));
}
