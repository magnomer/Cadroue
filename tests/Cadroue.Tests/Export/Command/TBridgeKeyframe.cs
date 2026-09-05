using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

/// <summary>
/// Locks which plan the interior keyframes of an interval select: a hybrid bridged plan
/// when usable keyframes exist, and a whole-interval encode when none do.
/// </summary>
[Collection("EncodeCommand")]
public sealed class TBridgeKeyframe
{
    [Fact]
    public void NonKeyframeBoundsSplit_ProducesHybridStageList()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        // Interval is [10, 30]; interior keyframes at 12 and 28 align with neither bound.
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

        // No usable interior keyframe within [10, 30]: whole-interval fallback.
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
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        Assert.Equal(8, stages.Count);
        Assert.True(stages[0].LEncodeStageTemporary);
        Assert.Contains("concat", TEncodeToken.TEncodeTokenRead(stages[^1].LEncodeStageArguments));
    }
}
