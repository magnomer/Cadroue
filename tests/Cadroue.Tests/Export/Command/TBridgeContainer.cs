using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

/// <summary>
/// Locks what a bridged cut does to awkward container geometry: B-frame reordering,
/// sub-millisecond and rounded keyframe times, non-zero start timelines, odd frame rates
/// and source time bases that later merges depend on.
/// </summary>
[Collection("EncodeCommand")]
public sealed class TBridgeContainer
{
    [Fact]
    public void MatroskaBFramesWithFourSecondGops_DoNotLengthenVideoPastAudio()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeReorderCreate("four-second-gops.mkv", 24, 96);
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 1.1, 22.5, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TBridgeMetric.TBridgeFormatRead(work.LWorkOutputPath), 21.35, 21.47);
        Assert.InRange(TBridgeMetric.TBridgeCountRead(work.LWorkOutputPath), 510, 516);
    }

    [Fact]
    public void SubMillisecondMp4Keyframe_PreservesFirstCopiedGop()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeReorderCreate("submillisecond-keyframes.mp4", 20, 49);
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 1, 18, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TBridgeMetric.TBridgeFormatRead(work.LWorkOutputPath), 16.95, 17.07);
        Assert.InRange(TBridgeMetric.TBridgeCountRead(work.LWorkOutputPath), 405, 411);
        Assert.True(string.IsNullOrWhiteSpace(TBridgeMetric.TBridgeErrorRead(work.LWorkOutputPath)));
    }

    [Fact]
    public void RoundedKeyframeAlignedMp4Cut_UsesOnePassAndSurvivesFullTranscode()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeReorderCreate("rounded-keyframe-aligned.mp4", 20, 49, 44_100);
        using var environment = new TEncodeCommand();
        // The actual packet boundaries are 2.043708s and 18.393375s. UI and
        // sidecar times are millisecond-based, so both ends arrive rounded.
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 2.044, 18.393, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);
        Assert.Single(stages);
        Assert.Equal("Copying", stages[0].LEncodeStageLabel);

        foreach (LEncodeStage stage in stages)
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        double videoStart = TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "v:0");
        double audioStart = TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "a:0");
        Assert.InRange(Math.Abs(videoStart - audioStart), 0, 0.11);
        // Simultaneous stream copy retains codec preroll just like ordinary Copy;
        // the later decode must preserve it instead of silently dropping audio.
        Assert.InRange(TBridgeMetric.TBridgeFormatRead(work.LWorkOutputPath), 16.45, 16.60);
        Assert.InRange(TBridgeMetric.TBridgeCountRead(work.LWorkOutputPath), 388, 395);
        Assert.True(string.IsNullOrWhiteSpace(TBridgeMetric.TBridgeErrorRead(work.LWorkOutputPath)));

        string converted = Path.Combine(fixture.TBridgeFixtureRoot, "smart-output-converted.mp4");
        TBridgeMetric.TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -i {TBridgeMetric.TBridgePathFormat(work.LWorkOutputPath)} "
            + $"-map 0:v:0 -map 0:a:0 -c:v libx264 -preset ultrafast -c:a aac -y {TBridgeMetric.TBridgePathFormat(converted)}");
        double smartAudioDuration = TBridgeMetric.TBridgeDecodeRead(
            work.LWorkOutputPath, Path.Combine(fixture.TBridgeFixtureRoot, "smart-decoded.pcm"), 44_100, 1);
        double convertedAudioDuration = TBridgeMetric.TBridgeDecodeRead(
            converted, Path.Combine(fixture.TBridgeFixtureRoot, "converted-decoded.pcm"), 44_100, 1);
        Assert.InRange(Math.Abs(smartAudioDuration - convertedAudioDuration), 0, 0.05);
    }

    [Fact]
    public void Mp4NonZeroTimeline_KeepsAudioAndVideoAtRequestedDuration()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeOffsetCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 10.1, 22.5, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TBridgeMetric.TBridgeFormatRead(work.LWorkOutputPath), 12.35, 12.47);
        Assert.InRange(TBridgeMetric.TBridgeCountRead(work.LWorkOutputPath), 294, 301);
        Assert.InRange(TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.True(string.IsNullOrWhiteSpace(TBridgeMetric.TBridgeErrorRead(work.LWorkOutputPath)));
    }

    [Fact]
    public void Mp4NonZeroTimeline_KeyframeScanCoversRequestedEnd()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeOffsetCreate();

        IReadOnlyList<LKeyframeEntry> keyframes = TEncodeCommand.TKeyframeRead(source, 7.9, 20.1);

        Assert.Equal(4, keyframes.Count);
        Assert.InRange(keyframes[0].LKeyframePresentationTime.TotalSeconds, 8, 8.1);
        Assert.InRange(keyframes[^1].LKeyframePresentationTime.TotalSeconds, 20, 20.1);
    }

    [Fact]
    public void ThirtyFpsMp4Hybrid_PreservesFrameRateAndAudioTimeline()
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeReorderCreate(
            "thirty-fps-hybrid.mp4",
            12,
            60,
            videoTimescale: 90_000,
            videoRate: "30");
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 1.1, 10.5, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        foreach (LEncodeStage stage in stages)
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.Equal("30/1", TBridgeMetric.TBridgeFramerateRead(work.LWorkOutputPath));
        Assert.Equal("1/90000", TBridgeMetric.TBridgeTimebaseRead(work.LWorkOutputPath));
        Assert.InRange(TBridgeMetric.TBridgeFormatRead(work.LWorkOutputPath), 9.35, 9.47);
        Assert.InRange(TBridgeMetric.TBridgeStreamRead(work.LWorkOutputPath, "v:0"), 9.35, 9.47);
        Assert.InRange(TBridgeMetric.TBridgeStreamRead(work.LWorkOutputPath, "a:0"), 9.35, 9.47);
        Assert.InRange(TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(TBridgeMetric.TBridgeFirstRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.True(string.IsNullOrWhiteSpace(TBridgeMetric.TBridgeErrorRead(work.LWorkOutputPath)));
    }

    [Theory]
    [InlineData(16_000)]
    [InlineData(24_000)]
    public void Mp4SmartRoutes_PreserveSourceVideoTimeBaseAndRemainMergeable(int sourceTimescale)
    {
        using var fixture = new TBridgeFixture();
        string source = fixture.TBridgeReorderCreate(
            $"mixed-smart-routes-{sourceTimescale}.mp4",
            12,
            48,
            48_000,
            sourceTimescale);
        using var environment = new TEncodeCommand();
        LWorkItem shortEncoded = TEncodeCommand.TBridgeIntervalCreate(
            source,
            Path.Combine(fixture.TBridgeFixtureRoot, $"short-smart-{sourceTimescale}.mp4"),
            0.1,
            1.8,
            "Include",
            "mp4",
            "mp4");
        LWorkItem hybrid = TEncodeCommand.TBridgeIntervalCreate(
            source,
            Path.Combine(fixture.TBridgeFixtureRoot, $"hybrid-smart-{sourceTimescale}.mp4"),
            2.1,
            10.5,
            "Include",
            "mp4",
            "mp4");

        IReadOnlyList<LEncodeStage> shortStages = TEncodeCommand.TBridgeSourceBuild(shortEncoded);
        IReadOnlyList<LEncodeStage> hybridStages = TEncodeCommand.TBridgeSourceBuild(hybrid);
        Assert.Single(shortStages);
        Assert.Contains(hybridStages, stage => stage.LEncodeStageLabel == "Copying middle");

        foreach (LEncodeStage stage in shortStages.Concat(hybridStages))
        {
            TBridgeMetric.TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        string sourceTimeBase = TBridgeMetric.TBridgeTimebaseRead(source);
        Assert.Equal($"1/{sourceTimescale}", sourceTimeBase);
        Assert.Equal(sourceTimeBase, TBridgeMetric.TBridgeTimebaseRead(shortEncoded.LWorkOutputPath));
        Assert.Equal(sourceTimeBase, TBridgeMetric.TBridgeTimebaseRead(hybrid.LWorkOutputPath));

        string mergeList = Path.Combine(fixture.TBridgeFixtureRoot, $"smart-merge-{sourceTimescale}.txt");
        string merged = Path.Combine(fixture.TBridgeFixtureRoot, $"smart-merged-{sourceTimescale}.mp4");
        File.WriteAllLines(mergeList,
        [
            $"file '{shortEncoded.LWorkOutputPath.Replace("'", "'\\''", StringComparison.Ordinal)}'",
            $"file '{hybrid.LWorkOutputPath.Replace("'", "'\\''", StringComparison.Ordinal)}'"
        ]);
        TBridgeMetric.TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -f concat -safe 0 -i {TBridgeMetric.TBridgePathFormat(mergeList)} -c copy -y {TBridgeMetric.TBridgePathFormat(merged)}");

        double mergedVideoDuration = TBridgeMetric.TBridgeStreamRead(merged, "v:0");
        double mergedAudioDuration = TBridgeMetric.TBridgeStreamRead(merged, "a:0");
        Assert.InRange(Math.Abs(mergedVideoDuration - mergedAudioDuration), 0, 0.12);
    }
}
