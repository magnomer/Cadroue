using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TBridgeFallback
{
    [Fact]
    public void WholeOutcome_EmitsSingleNormalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput, copyMode: false);

        LEncodeStage stage = Assert.Single(TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeWhole, TEncodeCommand.TBridgeSpanCreate(10, 30), null, null, null));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("libx264", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.NotEqual("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c"));
    }

    [Fact]
    public void InvalidOutcome_EmitsSingleEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        Assert.Single(TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeInvalid, TEncodeCommand.TBridgeSpanCreate(30, 10), null, null, null));
    }

    [Fact]
    public void ProcessingPresent_DefersToWholeIntervalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeCropCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.TBridgeResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            TEncodeCommand.TBridgeSpanCreate(10, 12),
            TEncodeCommand.TBridgeSpanCreate(12, 28),
            TEncodeCommand.TBridgeSpanCreate(28, 30)));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("libx264", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
    }
}
