using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

/// <summary>
/// Locks where audio lands relative to video after a bridged cut: delayed tracks keep their
/// cut-relative offsets, ordinary tracks start together, and audio lying outside the cut
/// leaves the video mux intact.
/// </summary>
[Collection("EncodeCommand")]
public sealed class TBridgeAlignment
{
    [Fact]
    public void DelayedMultipleAudioTracks_PreserveCutRelativeOffsets()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeDelayedCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 1.1, 6.4, "Include all audio tracks");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work, (1.1, 6.4), (1.1, 2), (2, 6, 5.933), (6, 6.4));
        Assert.Single(stages, stage => stage.LEncodeStageLabel == "Copying audio");

        foreach (LEncodeStage stage in stages)
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        IReadOnlyDictionary<int, double> starts = TBridgeMetric.TBridgePacketRead(work.LWorkOutputPath);

        Assert.Equal(2, starts.Count);
        Assert.InRange(TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(starts.Values.Min(), 1.85, 1.95);
        Assert.InRange(starts.Values.Max() - starts.Values.Min(), 0.20, 0.30);
    }

    [Fact]
    public void OrdinaryAudioAndVideo_StartTogetherAfterAccurateCut()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeSyncCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 1.1, 6.4, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work, (1.1, 6.4), (1.1, 2), (2, 6, 5.933), (6, 6.4));

        foreach (LEncodeStage stage in stages)
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.InRange(TBridgeMetric.TBridgeFormatRead(work.LWorkOutputPath), 5.25, 5.37);
    }

    [Fact]
    public void AudioOutsideCut_OmitsInvalidIntermediateAndStillBuildsVideoMux()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeDelayedCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 0, 0.5, "Include all audio tracks");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work, (0, 0.5), null, (0, 0.5, 0.5), null);

        Assert.False(TEncodeCommand.TAudioIntervalRead(source, 0, 0.5));
        LEncodeStage copy = Assert.Single(stages);
        Assert.Equal("Copying", copy.LEncodeStageLabel);
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(TEncodeToken.TEncodeTokenRead(copy.LEncodeStageArguments), "-i"));

        foreach (LEncodeStage stage in stages)
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.True(File.Exists(work.LWorkOutputPath));
        Assert.InRange(TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
    }
}
