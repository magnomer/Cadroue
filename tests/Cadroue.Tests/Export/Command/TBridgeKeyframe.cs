using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TBridgeKeyframe
{
    [Fact]
    public void NonKeyframeBoundsSplit_ProducesHybridStageList()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeResolve(work, 12, 28);

        Assert.Equal(8, stages.Count);
        Assert.Equal("Encoding head bridge", stages[0].LEncodeStageLabel);
        Assert.True(TEncodeCommand.TBridgeLabelFind(stages, "Copying middle") >= 0);
        Assert.True(TEncodeCommand.TBridgeLabelFind(stages, "Encoding tail bridge") >= 0);

        IReadOnlyList<string> middleTokens = TEncodeToken.TEncodeTokenRead(
            stages[TEncodeCommand.TBridgeLabelFind(stages, "Copying middle")].LEncodeStageArguments);
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(middleTokens, "-c:v"));
        Assert.Equal("12", TEncodeToken.TEncodeOptionRead(middleTokens, "-ss"));
        Assert.Equal("16", TEncodeToken.TEncodeOptionRead(middleTokens, "-t"));

        IReadOnlyList<string> muxTokens = TEncodeToken.TEncodeTokenRead(stages[^1].LEncodeStageArguments);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
        Assert.Contains("concat", muxTokens);
    }

    [Fact]
    public void KeyframeStarvedSplit_FallsBackToWholeIntervalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.TBridgeResolve(work));

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments));
    }

    [Fact]
    public void CleanCopyableCut_UsesHybridSmartPlan()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            TEncodeCommand.TBridgeSpanCreate(10, 12),
            TEncodeCommand.TBridgeSpanCreate(12, 28),
            TEncodeCommand.TBridgeSpanCreate(28, 30));

        Assert.Equal(8, stages.Count);
        Assert.True(stages[0].LEncodeStageTemporary);
        Assert.Contains("concat", TEncodeToken.TEncodeTokenRead(stages[^1].LEncodeStageArguments));
    }
}
