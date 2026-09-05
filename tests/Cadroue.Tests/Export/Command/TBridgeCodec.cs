using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

/// <summary>
/// Locks the source-codec match behind a boundary re-encode: which encoder each source
/// codec maps to, when an unmapped codec refuses the plan, and when the leading splice
/// normalization is inserted.
/// </summary>
[Collection("EncodeCommand")]
public sealed class TBridgeCodec
{
    [Fact]
    public void HevcSource_UsesMatchedLibx265Bridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, "hevc");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.TSourceStreamCreate("hevc", profile: "Main"));

        IReadOnlyList<string> headTokens = TEncodeToken.TEncodeTokenRead(stages[0].LEncodeStageArguments);
        Assert.Equal("libx265", TEncodeToken.TEncodeOptionRead(headTokens, "-c:v"));
        Assert.Equal("main", TEncodeToken.TEncodeOptionRead(headTokens, "-profile:v"));
        Assert.DoesNotContain("lossless=1", stages[0].LEncodeStageArguments);
    }

    [Fact]
    public void HevcSourceWithHead_InsertsLeadingNormalizeBetweenMiddleAndJoin()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, "hevc");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30),
            TEncodeCommand.TSourceStreamCreate("hevc", profile: "Main"));

        int middleOrder = TEncodeCommand.TBridgeLabelFind(stages, "Copying middle");
        int adjustOrder = TEncodeCommand.TBridgeLabelFind(stages, "Normalizing splice");
        int joinOrder = TEncodeCommand.TBridgeLabelFind(stages, "Joining bridges");
        Assert.True(middleOrder >= 0 && adjustOrder == middleOrder + 1 && adjustOrder < joinOrder);

        LEncodeStage adjust = stages[adjustOrder];
        Assert.Equal(LWorkStage.LWorkStageSplice, adjust.LEncodeStageKind);
        Assert.True(adjust.LEncodeStageTemporary);
        Assert.EndsWith(".middle.mov", adjust.LEncodeStagePath, StringComparison.Ordinal);
        Assert.Equal(string.Empty, adjust.LEncodeStageArguments);
    }

    [Fact]
    public void H264Source_HasNoLeadingNormalizeStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        Assert.DoesNotContain(stages, stage => stage.LEncodeStageKind == LWorkStage.LWorkStageSplice);
    }

    [Fact]
    public void HevcHeadlessPlan_HasNoLeadingNormalizeStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, "hevc");

        // No head bridge: the copied middle is first, so a decoder discards its leading
        // pictures at the stream start and no neutralization is required.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 28), (28, 30),
            TEncodeCommand.TSourceStreamCreate("hevc", profile: "Main"));

        Assert.DoesNotContain(stages, stage => stage.LEncodeStageKind == LWorkStage.LWorkStageSplice);
    }

    [Fact]
    public void Vp9Source_UsesMatchedLibvpxBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, "vp9");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.TSourceStreamCreate("vp9", profile: "Profile 0"));

        IReadOnlyList<string> headTokens = TEncodeToken.TEncodeTokenRead(stages[0].LEncodeStageArguments);
        Assert.Equal("libvpx-vp9", TEncodeToken.TEncodeOptionRead(headTokens, "-c:v"));
    }

    [Fact]
    public void ProresSource_UsesMatchedProresBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, "prores");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.TSourceStreamCreate("prores", profile: "Standard"));

        IReadOnlyList<string> headTokens = TEncodeToken.TEncodeTokenRead(stages[0].LEncodeStageArguments);
        Assert.Equal("prores_ks", TEncodeToken.TEncodeOptionRead(headTokens, "-c:v"));
    }

    [Fact]
    public void UnsupportedCodecWithBridges_ProducesNoStages()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, "mpeg2video");

        // A boundary re-encode is required (head + tail) but no encoder maps to mpeg2video:
        // smart encoding fails outright rather than mismatching the copied middle.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30),
            TEncodeCommand.TSourceStreamCreate("mpeg2video", profile: "Main"));

        Assert.Empty(stages);
    }

    [Fact]
    public void UnsupportedCodecWholeCopyable_StillCopiesWithoutAnEncoder()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, "mpeg2video");

        // No head/tail: the whole video is stream-copied, so the unmapped codec never matters.
        LEncodeStage stage = Assert.Single(TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 30), null,
            TEncodeCommand.TSourceStreamCreate("mpeg2video", profile: "Main")));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("Copying", stage.LEncodeStageLabel);
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
    }
}
