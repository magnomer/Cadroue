using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class SmartEncodingCommandTests
{
    private static readonly string SmartSource = Path.Combine("input media", "source clip.mov");
    private static readonly string SmartOutput = Path.Combine("output media", "smart clip.mp4");

    [Fact]
    public void SmartPlan_EmitsVideoOnlyBridgesSeparateAudioAndCopyJoin()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        // head, middle, tail (video only) + audio + join.
        Assert.Equal(5, stages.Count);

        LEncodeStage head = stages[0];
        IReadOnlyList<string> headTokens = CommandTokens.Read(head.LEncodeStageArguments);
        Assert.True(head.LEncodeStageTemporary);
        Assert.Equal("libx264", CommandTokens.ValueAfter(headTokens, "-c:v"));
        Assert.DoesNotContain("-qp", headTokens);
        Assert.Equal("high", CommandTokens.ValueAfter(headTokens, "-profile:v"));
        Assert.Equal("yuv420p", CommandTokens.ValueAfter(headTokens, "-pix_fmt"));
        Assert.Equal("5000000", CommandTokens.ValueAfter(headTokens, "-b:v"));
        Assert.Contains("-an", headTokens);
        Assert.DoesNotContain("-c:a", headTokens);
        Assert.Equal("passthrough", CommandTokens.ValueAfter(headTokens, "-fps_mode"));
        Assert.DoesNotContain("-r", headTokens);
        Assert.Equal("10", CommandTokens.ValueAfter(headTokens, "-ss"));
        Assert.Equal("2", CommandTokens.ValueAfter(headTokens, "-t"));

        LEncodeStage middle = stages[1];
        IReadOnlyList<string> middleTokens = CommandTokens.Read(middle.LEncodeStageArguments);
        Assert.True(middle.LEncodeStageTemporary);
        Assert.Equal("copy", CommandTokens.ValueAfter(middleTokens, "-c:v"));
        Assert.Contains("-an", middleTokens);
        Assert.DoesNotContain("-c:a", middleTokens);
        Assert.Equal("12.002", CommandTokens.ValueAfter(middleTokens, "-ss"));
        Assert.Equal("16", CommandTokens.ValueAfter(middleTokens, "-t"));

        LEncodeStage tail = stages[2];
        IReadOnlyList<string> tailTokens = CommandTokens.Read(tail.LEncodeStageArguments);
        Assert.True(tail.LEncodeStageTemporary);
        Assert.Equal("libx264", CommandTokens.ValueAfter(tailTokens, "-c:v"));
        Assert.Equal("high", CommandTokens.ValueAfter(tailTokens, "-profile:v"));
        Assert.Equal("yuv420p", CommandTokens.ValueAfter(tailTokens, "-pix_fmt"));
        Assert.DoesNotContain("-qp", tailTokens);
        Assert.Contains("-an", tailTokens);

        LEncodeStage audio = stages[3];
        IReadOnlyList<string> audioTokens = CommandTokens.Read(audio.LEncodeStageArguments);
        Assert.True(audio.LEncodeStageTemporary);
        Assert.Equal("Copying audio", audio.LEncodeStageLabel);
        Assert.Contains("-vn", audioTokens);
        Assert.Equal("0:a:0", CommandTokens.ValueAfter(audioTokens, "-map"));
        Assert.Equal("copy", CommandTokens.ValueAfter(audioTokens, "-c:a"));
        Assert.Equal("10", CommandTokens.ValueAfter(audioTokens, "-ss"));
        Assert.Equal("20", CommandTokens.ValueAfter(audioTokens, "-t"));

        LEncodeStage mux = stages[4];
        IReadOnlyList<string> muxTokens = CommandTokens.Read(mux.LEncodeStageArguments);
        Assert.False(mux.LEncodeStageTemporary);
        Assert.Equal(work.LWorkOutputPath, mux.LEncodeStagePath);
        Assert.Equal(LWorkStage.LWorkStageMux, mux.LEncodeStageKind);
        Assert.Contains("concat", muxTokens);
        Assert.Equal("copy", CommandTokens.ValueAfter(muxTokens, "-c"));
        Assert.DoesNotContain("-avoid_negative_ts", muxTokens);
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a"));
        Assert.Equal(work.LWorkOutputPath, muxTokens[^1]);

        string joinPath = CommandTokens.ValueAfter(muxTokens, "-i");
        string joinList = File.ReadAllText(joinPath);
        int headOrder = joinList.IndexOf(".head", StringComparison.Ordinal);
        int middleOrder = joinList.IndexOf(".middle", StringComparison.Ordinal);
        int tailOrder = joinList.IndexOf(".tail", StringComparison.Ordinal);
        Assert.True(headOrder >= 0 && headOrder < middleOrder && middleOrder < tailOrder);
    }

    [Fact]
    public void KeyedOriginPlan_OmitsHeadBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 28), (28, 30));

        // middle, tail, audio, join (no head).
        Assert.Equal(4, stages.Count);
        Assert.Equal("Copying middle", stages[0].LEncodeStageLabel);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
    }

    [Fact]
    public void CopiedMiddle_StopsBeforeFollowingKeyframeDecodeTimestamp()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartDecodedStagesBuild(
            work,
            (10, 30),
            (10, 12),
            (12, 28, 27.933),
            (28, 30),
            TEncodeCommand.SourceStreamCreate("h264"));
        IReadOnlyList<string> middleTokens = CommandTokens.Read(stages[1].LEncodeStageArguments);

        Assert.Equal("12.002", CommandTokens.ValueAfter(middleTokens, "-ss"));
        Assert.Equal("15.929", CommandTokens.ValueAfter(middleTokens, "-t"));
    }

    [Fact]
    public void WholeCopyableVideoWithCopyAudio_UsesIndependentTrimmedStreams()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        // Keep copied audio preroll from shifting copied video by trimming each stream
        // independently before the final mux.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 30), null);

        Assert.Equal(3, stages.Count);
        Assert.Equal("Copying middle", stages[0].LEncodeStageLabel);
        Assert.Equal("Copying audio", stages[1].LEncodeStageLabel);

        IReadOnlyList<string> audioTokens = CommandTokens.Read(stages[1].LEncodeStageArguments);
        Assert.Equal(2, CommandTokens.Count(audioTokens, "-ss"));
        Assert.Equal("make_zero", CommandTokens.ValueAfter(audioTokens, "-avoid_negative_ts"));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[2].LEncodeStageArguments);
        Assert.Contains("concat", muxTokens);
        Assert.DoesNotContain("-avoid_negative_ts", muxTokens);
    }

    [Fact]
    public void WholeCopyableVideoWithEncodeAudio_SplitsVideoCopyAudioEncodeThenJoins()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "aac", audioMode: "Encode");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 30), null);

        // middle copy, audio encode, join. No single simultaneous pass because audio is processed.
        Assert.Equal(3, stages.Count);
        Assert.Equal("Copying middle", stages[0].LEncodeStageLabel);

        IReadOnlyList<string> audioTokens = CommandTokens.Read(stages[1].LEncodeStageArguments);
        Assert.Equal("Encoding audio", stages[1].LEncodeStageLabel);
        Assert.Equal("aac", CommandTokens.ValueAfter(audioTokens, "-c:a"));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[2].LEncodeStageArguments);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[2].LEncodeStageKind);
        Assert.Contains("concat", muxTokens);
        Assert.Equal("copy", CommandTokens.ValueAfter(muxTokens, "-c"));
    }

    [Fact]
    public void HevcSource_UsesMatchedLibx265Bridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "hevc");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.SourceStreamCreate("hevc", profile: "Main"));

        IReadOnlyList<string> headTokens = CommandTokens.Read(stages[0].LEncodeStageArguments);
        Assert.Equal("libx265", CommandTokens.ValueAfter(headTokens, "-c:v"));
        Assert.Equal("main", CommandTokens.ValueAfter(headTokens, "-profile:v"));
        Assert.DoesNotContain("lossless=1", stages[0].LEncodeStageArguments);
    }

    [Fact]
    public void Vp9Source_UsesMatchedLibvpxBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "vp9");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.SourceStreamCreate("vp9", profile: "Profile 0"));

        IReadOnlyList<string> headTokens = CommandTokens.Read(stages[0].LEncodeStageArguments);
        Assert.Equal("libvpx-vp9", CommandTokens.ValueAfter(headTokens, "-c:v"));
    }

    [Fact]
    public void ProresSource_UsesMatchedProresBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "prores");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.SourceStreamCreate("prores", profile: "Standard"));

        IReadOnlyList<string> headTokens = CommandTokens.Read(stages[0].LEncodeStageArguments);
        Assert.Equal("prores_ks", CommandTokens.ValueAfter(headTokens, "-c:v"));
    }

    [Fact]
    public void UnsupportedCodecWithBridges_ProducesNoStages()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "mpeg2video");

        // A boundary re-encode is required (head + tail) but no encoder maps to mpeg2video:
        // smart encoding fails outright rather than mismatching the copied middle.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30),
            TEncodeCommand.SourceStreamCreate("mpeg2video", profile: "Main"));

        Assert.Empty(stages);
    }

    [Fact]
    public void UnsupportedCodecWholeCopyable_StillCopiesWithoutAnEncoder()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "mpeg2video");

        // No head/tail: the whole video is stream-copied, so the unmapped codec never matters.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 30), null,
            TEncodeCommand.SourceStreamCreate("mpeg2video", profile: "Main"));
        IReadOnlyList<string> tokens = CommandTokens.Read(stages[0].LEncodeStageArguments);

        Assert.Equal(3, stages.Count);
        Assert.Equal("Copying middle", stages[0].LEncodeStageLabel);
        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
    }

    [Fact]
    public void CopyableAudio_StaysSingleCopyRegionInItsOwnStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "pcm_s16le");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        // Video is split into head/middle/tail; none of them touch audio.
        foreach (LEncodeStage videoStage in stages.Take(3))
        {
            IReadOnlyList<string> videoTokens = CommandTokens.Read(videoStage.LEncodeStageArguments);
            Assert.Contains("-an", videoTokens);
            Assert.DoesNotContain("-c:a", videoTokens);
        }

        // Audio is one continuous stream-copy region cut over the whole requested interval,
        // in its own stage, not folded into the join.
        IReadOnlyList<string> audioTokens = CommandTokens.Read(stages[^2].LEncodeStageArguments);
        Assert.Equal("copy", CommandTokens.ValueAfter(audioTokens, "-c:a"));
        Assert.Equal("10", CommandTokens.ValueAfter(audioTokens, "-ss"));
        Assert.Equal("20", CommandTokens.ValueAfter(audioTokens, "-t"));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.DoesNotContain("-c:a", muxTokens);
    }

    [Fact]
    public void EncodeAudioMode_ReEncodesTheContinuousIntervalInItsOwnStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "aac", audioMode: "Encode");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> audioTokens = CommandTokens.Read(stages[^2].LEncodeStageArguments);
        Assert.NotEqual("copy", CommandTokens.ValueAfter(audioTokens, "-c:a"));
        Assert.Equal("aac", CommandTokens.ValueAfter(audioTokens, "-c:a"));
        // Still one continuous audio region over the requested interval, not per-region pieces.
        Assert.Equal("0:a:0", CommandTokens.ValueAfter(audioTokens, "-map"));
        Assert.Equal("20", CommandTokens.ValueAfter(audioTokens, "-t"));
    }

    [Fact]
    public void AllAudioTracks_ExtractsEveryTrackAndCarriesThemThroughTheJoin()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioStream: "Include all audio tracks");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> audioTokens = CommandTokens.Read(stages[^2].LEncodeStageArguments);
        Assert.Equal("0:a", CommandTokens.ValueAfter(audioTokens, "-map"));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a"));
    }

    [Fact]
    public void FinalMux_MapsVideoAndAudioTracksOnce()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Equal(2, CommandTokens.Count(muxTokens, "-map"));
        Assert.Equal(1, CommandTokens.Count(muxTokens, "0:v:0"));
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a"));
    }

    [Fact]
    public void SilentSource_JoinsVideoWithoutAnyAudioTrack()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "", sampleRate: 0);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        // head, middle, tail, join — no audio stage.
        Assert.Equal(4, stages.Count);
        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Contains("-an", muxTokens);
        Assert.Equal(0, CommandTokens.Count(muxTokens, "1:a"));
        Assert.Equal("copy", CommandTokens.ValueAfter(muxTokens, "-c"));
    }

    [Fact]
    public void WholeOutcome_EmitsSingleNormalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, copyMode: false);

        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeWhole, (10, 30), null, null, null));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("libx264", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.NotEqual("copy", CommandTokens.ValueAfter(tokens, "-c"));
    }

    [Fact]
    public void InvalidOutcome_EmitsSingleEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        Assert.Single(TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeInvalid, (30, 10), null, null, null));
    }

    [Fact]
    public void ProcessingPresent_DefersToWholeIntervalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartCropWorkCreate(SmartSource, SmartOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30)));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("libx264", CommandTokens.ValueAfter(tokens, "-c:v"));
    }

    [Fact]
    public void NonKeyframeBoundsSplit_ProducesHybridStageList()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        // Interval is [10, 30]; interior keyframes at 12 and 28 align with neither bound.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartBridgeResolve(work, 12, 28);

        Assert.Equal(5, stages.Count);
        Assert.Equal("Encoding head bridge", stages[0].LEncodeStageLabel);
        Assert.Equal("Copying middle", stages[1].LEncodeStageLabel);
        Assert.Equal("Encoding tail bridge", stages[2].LEncodeStageLabel);

        IReadOnlyList<string> middleTokens = CommandTokens.Read(stages[1].LEncodeStageArguments);
        Assert.Equal("copy", CommandTokens.ValueAfter(middleTokens, "-c:v"));
        Assert.Equal("12.002", CommandTokens.ValueAfter(middleTokens, "-ss"));
        Assert.Equal("16", CommandTokens.ValueAfter(middleTokens, "-t"));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
        Assert.Contains("concat", muxTokens);
    }

    [Fact]
    public void KeyframeStarvedSplit_FallsBackToWholeIntervalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        // No usable interior keyframe within [10, 30]: whole-interval fallback.
        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartBridgeResolve(work));

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", CommandTokens.Read(stage.LEncodeStageArguments));
    }

    [Fact]
    public void CleanCopyableCut_UsesHybridSmartPlan()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        Assert.Equal(5, stages.Count);
        Assert.True(stages[0].LEncodeStageTemporary);
        Assert.Contains("concat", CommandTokens.Read(stages[^1].LEncodeStageArguments));
    }
}
