using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TEncodeMode
{
    private static readonly string TEncodeSource = Path.Combine("input media", "mode clip.mov");
    private static readonly string TEncodeOutput = Path.Combine("output media", "mode clip.mp4");

    [Fact]
    public void CopyMode_CleanCut_StreamCopiesToTheRequestedEnd()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(videoMode: "Copy", audioMode: "Copy"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.Equal("10", TEncodeToken.TEncodeOptionRead(tokens, "-ss"));
        Assert.Equal("0", TEncodeToken.TEncodeOptionRead(tokens, "-copypriorss"));
        Assert.Equal("20", TEncodeToken.TEncodeOptionRead(tokens, "-t"));
        Assert.Equal("make_zero", TEncodeToken.TEncodeOptionRead(tokens, "-avoid_negative_ts"));
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal(LWorkStage.LWorkStagePassthrough, stage.LEncodeStageKind);
        Assert.Equal("Copying", stage.LEncodeStageLabel);
    }

    [Fact]
    public void SmartMode_CleanCutWithInteriorKeyframes_EmitsBridgeStages()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(TEncodeSource, TEncodeOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeResolve(work, 12, 28);

        Assert.Equal(8, stages.Count);
        Assert.Equal("Encoding head bridge", stages[0].LEncodeStageLabel);
        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Encoding tail bridge");
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
        Assert.Contains("concat", TEncodeToken.TEncodeTokenRead(stages[^1].LEncodeStageArguments));
    }

    [Fact]
    public void SmartMode_ItemWithEdit_FallsBackToSingleFullEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeCropCreate(TEncodeSource, TEncodeOutput);

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

    [Fact]
    public void EncodeMode_EmitsSingleStageCarryingTheVideoEncoder()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(videoMode: "Encode"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("libx264", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.Equal(2, TEncodeToken.TEncodeCountRead(tokens, "-ss"));
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
    }

    [Fact]
    public void LegacyAutoMode_NormalizesToEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(videoMode: "Auto"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("libx264", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.NotEqual("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.DoesNotContain("concat", tokens);
    }
}
