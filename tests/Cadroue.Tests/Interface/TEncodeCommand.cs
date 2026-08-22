using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("EncodeCommand", DisableParallelization = true)]
public sealed class TEncodeCommandCollection;

/// <summary>
/// Test-side relay for production command construction and its depot configuration.
/// </summary>
internal sealed class TEncodeCommand : IDisposable
{
    private readonly string tDepotRoot = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "EncodeCommand", Guid.NewGuid().ToString("N"));
    private bool tDisposed;

    internal TEncodeCommand()
    {
        LDepot.LDepotRootSet(tDepotRoot);
    }

    internal static LEncoding OutputCreate(
        string container = "mp4",
        string extension = "mp4",
        string videoStream = "Include",
        string videoMode = "Encode",
        string videoEncoder = "libx264",
        string videoRateControl = "CRF (constant quality)",
        string videoQuality = "19",
        string videoSpeed = "slow",
        string videoSize = "Same as source",
        bool videoSizeReactive = false,
        string videoFps = "Same as source",
        string videoPixelFormat = "Auto",
        IReadOnlyDictionary<string, string>? videoExtras = null,
        string audioStream = "Include",
        string audioMode = "Encode",
        string audioEncoder = "AAC",
        string audioRateControl = "Bitrate",
        string audioQuality = "192k",
        string audioSpeed = "",
        IReadOnlyDictionary<string, string>? audioExtras = null,
        string audioSampleRate = "Same as source",
        string audioChannels = "Same as source") =>
        new(
            "{OriginalName}", container, extension, "Same as source", string.Empty,
            new LEncodingVideo(
                videoStream, videoMode, videoEncoder, videoRateControl, videoQuality, videoSpeed,
                videoSize, videoSizeReactive, videoFps, videoPixelFormat,
                videoExtras ?? new Dictionary<string, string>()),
            new LEncodingAudio(
                audioStream, audioMode, audioEncoder, audioRateControl, audioQuality, audioSpeed,
                audioExtras ?? new Dictionary<string, string>(), audioSampleRate, audioChannels),
            "Command test", "Overwrite", "_1");

    internal static LWorkItem WorkCreate(
        LWorkKind kind,
        string source,
        string outputPath,
        LEncoding output,
        TimeSpan? start = null,
        TimeSpan? end = null,
        LWorkCrop? crop = null,
        LWorkVideo? video = null,
        LWorkAudio? audio = null,
        IReadOnlyList<string>? mergeSources = null) =>
        new(
            Guid.NewGuid(), kind, LWorkPriority.LWorkPriorityNormal, source,
            start ?? TimeSpan.Zero, end ?? TimeSpan.Zero,
            Path.GetFileName(outputPath), outputPath, output,
            lWorkCrop: crop, lWorkVideo: video, lWorkAudio: audio, lWorkMergeSources: mergeSources);

    internal static IReadOnlyList<LEncodeStage> StagesBuild(LWorkItem work) =>
        LEncode.LEncodeStagesBuild(work);

    internal static LWorkItem SmartWorkCreate(
        string source, string output, string codec = "h264", bool copyMode = true,
        string audioCodec = "aac", int sampleRate = 48000, string audioMode = "Copy")
    {
        LEncoding encoding = copyMode
            ? OutputCreate(videoMode: "Smart", audioMode: audioMode)
            : OutputCreate();
        LWorkItem work = WorkCreate(
            LWorkKind.LWorkKindSplit, source, output, encoding,
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        work.LWorkSourceMedia = new LWorkMedia(1920, 1080, 30, 30_000, true)
        {
            LWorkMediaCodec = codec,
            LWorkAudioCodec = audioCodec,
            LWorkMediaSamplerate = sampleRate
        };
        return work;
    }

    internal static LBridgeStream SourceStreamCreate(
        string codec = "h264",
        string profile = "High",
        string pixel = "yuv420p",
        long bitrate = 5_000_000) =>
        new(codec, profile, pixel, "bt709", "bt709", "bt709", "tv", "30000/1001", bitrate);

    internal static IReadOnlyList<LEncodeStage> SmartStagesBuild(
        LWorkItem work,
        LBridgeOutcome outcome,
        (double origin, double end) interval,
        (double origin, double end)? head,
        (double origin, double end)? middle,
        (double origin, double end)? tail,
        LBridgeStream? source = null) =>
        LEncode.LEncodeSmartBuild(
            work,
            new LBridgePlan(
                outcome,
                SpanCreate(interval),
                head is { } tHead ? SpanCreate(tHead) : null,
                middle is { } tMiddle ? SpanCreate(tMiddle) : null,
                tail is { } tTail ? SpanCreate(tTail) : null),
            source ?? SourceStreamCreate(work.LWorkSourceMedia?.LWorkMediaCodec ?? "h264"));

    internal static IReadOnlyList<LEncodeStage> SmartResolveBuild(
        LWorkItem work,
        LBridgeOutcome outcome,
        (double origin, double end) interval,
        (double origin, double end)? head,
        (double origin, double end)? middle,
        (double origin, double end)? tail,
        LBridgeStream? source = null) =>
        LEncode.LEncodeSmartResolve(
            work,
            new LBridgePlan(
                outcome,
                SpanCreate(interval),
                head is { } tHead ? SpanCreate(tHead) : null,
                middle is { } tMiddle ? SpanCreate(tMiddle) : null,
                tail is { } tTail ? SpanCreate(tTail) : null),
            source ?? SourceStreamCreate(work.LWorkSourceMedia?.LWorkMediaCodec ?? "h264"));

    internal static IReadOnlyList<LEncodeStage> SmartBridgeResolve(
        LWorkItem work, params double[] keyframes) =>
        LEncode.LEncodeBridgeResolve(
            work, keyframes.Select(TimeSpan.FromSeconds).ToArray());

    internal static LWorkItem SmartCropWorkCreate(string source, string output)
    {
        LWorkItem work = WorkCreate(
            LWorkKind.LWorkKindSplit, source, output,
            OutputCreate(videoMode: "Smart", audioMode: "Copy"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30),
            crop: new LWorkCrop(20, 0, 0, 0, 0, false, false));
        work.LWorkSourceMedia = new LWorkMedia(1920, 1080, 30, 30_000, true) { LWorkMediaCodec = "h264" };
        return work;
    }

    private static LBridgeSpan SpanCreate((double origin, double end) span) =>
        new(TimeSpan.FromSeconds(span.origin), TimeSpan.FromSeconds(span.end));

    public void Dispose()
    {
        if (tDisposed)
        {
            return;
        }

        LDepot.LDepotRootSet(null);
        try
        {
            if (Directory.Exists(tDepotRoot))
            {
                Directory.Delete(tDepotRoot, true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }

        tDisposed = true;
    }
}

/// <summary>
/// Locks the three video modes (Copy / Smart / Encode) and the legacy Auto normalization
/// through the production stage builder.
/// </summary>
[Collection("EncodeCommand")]
public sealed class ModeCommandTests
{
    private static readonly string ModeSource = Path.Combine("input media", "mode clip.mov");
    private static readonly string ModeOutput = Path.Combine("output media", "mode clip.mp4");

    [Fact]
    public void CopyMode_CleanCut_StreamCopiesToTheRequestedEnd()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindSplit, ModeSource, ModeOutput,
            TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        LEncodeStage stage = Assert.Single(TEncodeCommand.StagesBuild(work));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.Equal("10", CommandTokens.ValueAfter(tokens, "-ss"));
        Assert.Equal("20", CommandTokens.ValueAfter(tokens, "-t"));
        Assert.Equal("make_zero", CommandTokens.ValueAfter(tokens, "-avoid_negative_ts"));
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
    }

    [Fact]
    public void SmartMode_CleanCutWithInteriorKeyframes_EmitsBridgeStages()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(ModeSource, ModeOutput);

        // Interval is [10, 30]; interior keyframes at 12 and 28 align with neither bound.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartBridgeResolve(work, 12, 28);

        Assert.Equal(4, stages.Count);
        Assert.Equal("Encoding head bridge", stages[0].LEncodeStageLabel);
        Assert.Equal("Copying middle", stages[1].LEncodeStageLabel);
        Assert.Equal("Encoding tail bridge", stages[2].LEncodeStageLabel);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
        Assert.Contains("concat", CommandTokens.Read(stages[^1].LEncodeStageArguments));
    }

    [Fact]
    public void SmartMode_ItemWithEdit_FallsBackToSingleFullEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartCropWorkCreate(ModeSource, ModeOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30)));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("libx264", CommandTokens.ValueAfter(tokens, "-c:v"));
    }

    [Fact]
    public void EncodeMode_EmitsSingleStageCarryingTheVideoEncoder()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindSplit, ModeSource, ModeOutput,
            TEncodeCommand.OutputCreate(videoMode: "Encode"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        LEncodeStage stage = Assert.Single(TEncodeCommand.StagesBuild(work));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal("libx264", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
    }

    [Fact]
    public void LegacyAutoMode_NormalizesToEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindSplit, ModeSource, ModeOutput,
            TEncodeCommand.OutputCreate(videoMode: "Auto"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        LEncodeStage stage = Assert.Single(TEncodeCommand.StagesBuild(work));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal("libx264", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.NotEqual("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.DoesNotContain("concat", tokens);
    }
}
