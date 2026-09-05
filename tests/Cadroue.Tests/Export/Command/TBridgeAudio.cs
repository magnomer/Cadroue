using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

/// <summary>
/// Locks the audio side of a bridged plan: the single continuous audio stage, the track
/// selection it carries, and how the final mux maps video and audio exactly once.
/// </summary>
[Collection("EncodeCommand")]
public sealed class TBridgeAudio
{
    [Fact]
    public void CopyableAudio_StaysSingleCopyRegionInItsOwnStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, audioCodec: "pcm_s16le");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        // Video is split into head/middle/tail; none of them touch audio.
        string[] videoLabels = { "Encoding head bridge", "Copying middle", "Encoding tail bridge" };
        foreach (string videoLabel in videoLabels)
        {
            LEncodeStage videoStage = stages[TEncodeCommand.TBridgeLabelFind(stages, videoLabel)];
            IReadOnlyList<string> videoTokens = TEncodeToken.TEncodeTokenRead(videoStage.LEncodeStageArguments);
            Assert.Contains("-an", videoTokens);
            Assert.DoesNotContain("-c:a", videoTokens);
        }

        // Audio is one continuous stream-copy region cut over the whole requested interval.
        // It retains source-relative packet timestamps so delayed tracks stay delayed.
        IReadOnlyList<string> audioTokens = TEncodeToken.TEncodeTokenRead(stages[^2].LEncodeStageArguments);
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(audioTokens, "-c:a"));
        Assert.Equal("10", TEncodeToken.TEncodeOptionRead(audioTokens, "-ss"));
        Assert.Equal("20", TEncodeToken.TEncodeOptionRead(audioTokens, "-t"));
        Assert.DoesNotContain("-avoid_negative_ts", audioTokens);

        IReadOnlyList<string> muxTokens = TEncodeToken.TEncodeTokenRead(stages[^1].LEncodeStageArguments);
        Assert.DoesNotContain("-c:a", muxTokens);
    }

    [Fact]
    public void EncodeAudioMode_ReEncodesTheContinuousIntervalInItsOwnStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput,
            audioCodec: "aac", audioMode: "Encode");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> audioTokens = TEncodeToken.TEncodeTokenRead(stages[^2].LEncodeStageArguments);
        Assert.NotEqual("copy", TEncodeToken.TEncodeOptionRead(audioTokens, "-c:a"));
        Assert.Equal("aac", TEncodeToken.TEncodeOptionRead(audioTokens, "-c:a"));
        // Still one continuous audio region over the requested interval, not per-region pieces.
        Assert.Equal("0:a:0", TEncodeToken.TEncodeOptionRead(audioTokens, "-map"));
        Assert.Equal("20", TEncodeToken.TEncodeOptionRead(audioTokens, "-t"));
    }

    [Fact]
    public void AllAudioTracks_ExtractsEveryTrackAndCarriesThemThroughTheJoin()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput,
            audioStream: "Include all audio tracks");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> audioTokens = TEncodeToken.TEncodeTokenRead(stages[^2].LEncodeStageArguments);
        Assert.Equal("0:a", TEncodeToken.TEncodeOptionRead(audioTokens, "-map"));

        IReadOnlyList<string> muxTokens = TEncodeToken.TEncodeTokenRead(stages[^1].LEncodeStageArguments);
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(muxTokens, "1:a"));
    }

    [Fact]
    public void FinalMux_MapsVideoAndAudioTracksOnce()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> muxTokens = TEncodeToken.TEncodeTokenRead(stages[^1].LEncodeStageArguments);
        Assert.Equal(2, TEncodeToken.TEncodeCountRead(muxTokens, "-map"));
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(muxTokens, "0:v:0"));
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(muxTokens, "1:a"));
    }

    [Fact]
    public void SilentSource_JoinsVideoWithoutAnyAudioTrack()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput,
            audioCodec: "", sampleRate: 0);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        // head, middle, tail (each with its join piece), join — no audio stage.
        Assert.Equal(7, stages.Count);
        IReadOnlyList<string> muxTokens = TEncodeToken.TEncodeTokenRead(stages[^1].LEncodeStageArguments);
        Assert.Contains("-an", muxTokens);
        Assert.Equal(0, TEncodeToken.TEncodeCountRead(muxTokens, "1:a"));
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(muxTokens, "-c"));
    }
}
