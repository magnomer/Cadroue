using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class SmartCutCommandTests
{
    private static readonly string SmartSource = Path.Combine("input media", "source clip.mov");
    private static readonly string SmartOutput = Path.Combine("output media", "smart clip.mp4");

    [Fact]
    public void SmartPlan_EmitsVideoOnlyBridgesCopyMiddleAndAudioMux()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        Assert.Equal(4, stages.Count);

        LEncodeStage head = stages[0];
        IReadOnlyList<string> headTokens = CommandTokens.Read(head.LEncodeStageArguments);
        Assert.True(head.LEncodeStageTemporary);
        Assert.Equal("libx264", CommandTokens.ValueAfter(headTokens, "-c:v"));
        Assert.Equal("0", CommandTokens.ValueAfter(headTokens, "-qp"));
        Assert.Contains("-an", headTokens);
        Assert.DoesNotContain("-c:a", headTokens);
        Assert.Equal("10", CommandTokens.ValueAfter(headTokens, "-ss"));
        Assert.Equal("2", CommandTokens.ValueAfter(headTokens, "-t"));

        LEncodeStage middle = stages[1];
        IReadOnlyList<string> middleTokens = CommandTokens.Read(middle.LEncodeStageArguments);
        Assert.True(middle.LEncodeStageTemporary);
        Assert.Equal("copy", CommandTokens.ValueAfter(middleTokens, "-c:v"));
        Assert.Contains("-an", middleTokens);
        Assert.DoesNotContain("-c:a", middleTokens);
        Assert.Equal("12", CommandTokens.ValueAfter(middleTokens, "-ss"));
        Assert.Equal("16", CommandTokens.ValueAfter(middleTokens, "-t"));

        LEncodeStage tail = stages[2];
        IReadOnlyList<string> tailTokens = CommandTokens.Read(tail.LEncodeStageArguments);
        Assert.True(tail.LEncodeStageTemporary);
        Assert.Equal("libx264", CommandTokens.ValueAfter(tailTokens, "-c:v"));
        Assert.Equal("0", CommandTokens.ValueAfter(tailTokens, "-qp"));
        Assert.Contains("-an", tailTokens);

        LEncodeStage mux = stages[3];
        IReadOnlyList<string> muxTokens = CommandTokens.Read(mux.LEncodeStageArguments);
        Assert.False(mux.LEncodeStageTemporary);
        Assert.Equal(work.LWorkOutputPath, mux.LEncodeStagePath);
        Assert.Equal(LWorkStage.LWorkStageMux, mux.LEncodeStageKind);
        Assert.Contains("concat", muxTokens);
        Assert.Equal("copy", CommandTokens.ValueAfter(muxTokens, "-c:v"));
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

        Assert.Equal(3, stages.Count);
        Assert.Equal("Copying middle", stages[0].LEncodeStageLabel);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
    }

    [Fact]
    public void HevcSource_UsesLibx265LosslessBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "hevc");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null);

        IReadOnlyList<string> headTokens = CommandTokens.Read(stages[0].LEncodeStageArguments);
        Assert.Equal("libx265", CommandTokens.ValueAfter(headTokens, "-c:v"));
        Assert.Contains("lossless=1", stages[0].LEncodeStageArguments);
    }

    [Fact]
    public void CopyableAudio_StaysSingleCopyRegionAcrossRegionSplitVideo()
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
            Assert.Equal(0, CommandTokens.Count(videoTokens, "1:a:0"));
        }

        // Audio is one continuous stream-copy region cut over the whole requested interval.
        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Equal("copy", CommandTokens.ValueAfter(muxTokens, "-c:a"));
        Assert.Equal("10", CommandTokens.ValueAfter(muxTokens, "-ss"));
        Assert.Equal("20", CommandTokens.ValueAfter(muxTokens, "-t"));
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a:0"));
    }

    [Fact]
    public void LossyAudio_ReEncodesTheContinuousIntervalInsteadOfCopying()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "aac");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.NotEqual("copy", CommandTokens.ValueAfter(muxTokens, "-c:a"));
        Assert.Equal("aac", CommandTokens.ValueAfter(muxTokens, "-c:a"));
        // Still one continuous audio region over the requested interval, not per-region pieces.
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a:0"));
        Assert.Equal("20", CommandTokens.ValueAfter(muxTokens, "-t"));
    }

    [Fact]
    public void FinalMux_CarriesExactlyOneAudioTrack()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Equal(2, CommandTokens.Count(muxTokens, "-map"));
        Assert.Equal(1, CommandTokens.Count(muxTokens, "0:v:0"));
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a:0"));
    }

    [Fact]
    public void SilentSource_JoinsVideoWithoutAnyAudioTrack()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "", sampleRate: 0);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Contains("-an", muxTokens);
        Assert.Equal(0, CommandTokens.Count(muxTokens, "1:a:0"));
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
    public void IncompatibleBridge_FallsBackToWholeIntervalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30),
            compatible: false, reason: LBridgeReason.LBridgeReasonPixel));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", tokens);
        Assert.NotEqual("copy", CommandTokens.ValueAfter(tokens, "-c"));
    }

    [Fact]
    public void CleanCopyableCut_UsesHybridSmartPlan()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        Assert.Equal(4, stages.Count);
        Assert.True(stages[0].LEncodeStageTemporary);
        Assert.Contains("concat", CommandTokens.Read(stages[^1].LEncodeStageArguments));
    }
}
