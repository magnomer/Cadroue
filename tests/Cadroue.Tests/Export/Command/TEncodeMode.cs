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

    [Fact]
    public void CopyMode_StaleOutputSize_StillStreamCopies()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(videoMode: "Copy", audioMode: "Copy", videoSize: "1280x720"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.DoesNotContain("-vf", tokens);
        Assert.Equal(LWorkStage.LWorkStagePassthrough, stage.LEncodeStageKind);
    }

    [Fact]
    public void TwoPassMode_EmitsAnalysisPassBeforeTheEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(videoRateControl: "Two-pass bitrate", videoQuality: "6M"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TEncodeStagesBuild(work);

        Assert.Equal(2, stages.Count);
        IReadOnlyList<string> first = TEncodeToken.TEncodeTokenRead(stages[0].LEncodeStageArguments);
        IReadOnlyList<string> second = TEncodeToken.TEncodeTokenRead(stages[1].LEncodeStageArguments);
        Assert.Equal(LWorkStage.LWorkStageAnalyze, stages[0].LEncodeStageKind);
        Assert.True(stages[0].LEncodeStageTemporary);
        Assert.Equal("1", TEncodeToken.TEncodeOptionRead(first, "-pass"));
        Assert.Equal("null", TEncodeToken.TEncodeOptionRead(first, "-f"));
        Assert.Contains("-an", first);
        Assert.Equal("2", TEncodeToken.TEncodeOptionRead(second, "-pass"));
        Assert.Equal("6M", TEncodeToken.TEncodeOptionRead(second, "-b:v"));
        Assert.Equal(
            TEncodeToken.TEncodeOptionRead(first, "-passlogfile"),
            TEncodeToken.TEncodeOptionRead(second, "-passlogfile"));
        Assert.Equal(LWorkStage.LWorkStageEncode, stages[1].LEncodeStageKind);
    }

    [Fact]
    public void CbrMode_BoundsTheRateAroundTheTarget()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(videoRateControl: "CBR", videoQuality: "4M"));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("4M", TEncodeToken.TEncodeOptionRead(tokens, "-b:v"));
        Assert.Equal("4M", TEncodeToken.TEncodeOptionRead(tokens, "-minrate"));
        Assert.Equal("4M", TEncodeToken.TEncodeOptionRead(tokens, "-maxrate"));
        Assert.Equal("4M", TEncodeToken.TEncodeOptionRead(tokens, "-bufsize"));
    }

    [Theory]
    [InlineData("libvpx-vp9", "Constant quality (CQ)", "0")]
    [InlineData("libvpx-vp9", "Constrained quality", "2M")]
    [InlineData("libaom-av1", "Constrained quality", "4M")]
    public void ConstrainedQuality_KeepsABitrateCeiling(string encoder, string mode, string bitrate)
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(videoEncoder: encoder, videoRateControl: mode, videoQuality: "30"));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("30", TEncodeToken.TEncodeOptionRead(tokens, "-crf"));
        Assert.Equal(bitrate, TEncodeToken.TEncodeOptionRead(tokens, "-b:v"));
    }

    [Fact]
    public void NvencLossless_SuppressesTheConflictingTuneExtra()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(
                videoEncoder: "h264_nvenc", videoRateControl: "Lossless", videoSpeed: "p4",
                videoExtras: new Dictionary<string, string> { ["-tune"] = "hq" }));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal(1, TEncodeToken.TEncodeCountRead(tokens, "-tune"));
        Assert.Equal("lossless", TEncodeToken.TEncodeOptionRead(tokens, "-tune"));
    }

    [Fact]
    public void Jpeg2000Lossless_ForcesTheReversibleTransform()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(
                videoEncoder: "jpeg2000", videoRateControl: "Lossless (reversible DWT)", videoSpeed: "",
                videoExtras: new Dictionary<string, string> { ["-pred"] = "dwt97int" }));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal(1, TEncodeToken.TEncodeCountRead(tokens, "-pred"));
        Assert.Equal("dwt53", TEncodeToken.TEncodeOptionRead(tokens, "-pred"));
    }

    [Theory]
    [InlineData("VP8, libvpx / libvpx")]
    [InlineData("VP8, libvpx / libvpx / libvpx-vp8")]
    public void Vp8Family_ResolvesToTheOneRealEncoder(string encoderText)
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, TEncodeSource, TEncodeOutput,
            TEncodeCommand.TOutputCreate(
                videoEncoder: encoderText, videoRateControl: "Constant quality (CQ)", videoQuality: "10",
                videoSpeed: "1"));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("libvpx", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.Equal("10", TEncodeToken.TEncodeOptionRead(tokens, "-crf"));
        Assert.Equal("0", TEncodeToken.TEncodeOptionRead(tokens, "-b:v"));
    }
}
