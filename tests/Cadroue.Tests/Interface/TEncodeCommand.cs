using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
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
    private bool tEncodeDisposed;

    internal TEncodeCommand()
    {
        LDepot.LDepotRootSet(tDepotRoot);
    }

    internal static LEncoding TOutputCreate(
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

    internal static LWorkItem TWorkCreate(
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

    internal static IReadOnlyList<LEncodeStage> TEncodeStagesBuild(LWorkItem work) =>
        LEncode.LEncodeStagesBuild(work);

    internal static void TSourcePixelApply(LWorkItem work, string pixel) =>
        work.LWorkSourceMedia = new LWorkMedia(1920, 1080, 30, 120_000, true)
        {
            LWorkMediaPixel = pixel
        };

    internal static IReadOnlyList<string> TVideoEncodersRead() =>
        LRepertoireCatalog.LRepertoireEncodersRead()
            .Select(encoder => encoder.LRepertoireTokens[0])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(encoder => encoder, StringComparer.Ordinal)
            .ToArray();

    internal static LWorkItem TBridgeWorkCreate(
        string source, string output, string codec = "h264", bool copyMode = true,
        string audioCodec = "aac", int sampleRate = 48000, string audioMode = "Copy",
        string audioStream = "Include")
    {
        LEncoding encoding = copyMode
            ? TOutputCreate(videoMode: "Smart", audioStream: audioStream, audioMode: audioMode)
            : TOutputCreate();
        LWorkItem work = TWorkCreate(
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

    internal static LBridgeStream TSourceStreamCreate(
        string codec = "h264",
        string profile = "High",
        string pixel = "yuv420p",
        long bitrate = 5_000_000,
        string timeBase = "1/30000") =>
        new(codec, profile, pixel, "bt709", "bt709", "bt709", "tv", "30000/1001", bitrate, timeBase);

    internal static IReadOnlyList<LEncodeStage> TBridgeStagesBuild(
        LWorkItem work,
        LBridgeOutcome outcome,
        (double origin, double end) interval,
        (double origin, double end)? head,
        (double origin, double end)? middle,
        (double origin, double end)? tail,
        LBridgeStream? source = null,
        string? intermediateExtension = null) =>
        LEncode.LEncodeSmartBuild(
            work,
            new LBridgePlan(
                outcome,
                TBridgeSpanCreate(interval),
                head is { } tHead ? TBridgeSpanCreate(tHead) : null,
                middle is { } tMiddle ? TBridgeSpanCreate(tMiddle) : null,
                tail is { } tTail ? TBridgeSpanCreate(tTail) : null),
            source ?? TSourceStreamCreate(work.LWorkSourceMedia?.LWorkMediaCodec ?? "h264"),
            intermediateExtension);

    internal static IReadOnlyList<LEncodeStage> TBridgeResolveBuild(
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
                TBridgeSpanCreate(interval),
                head is { } tHead ? TBridgeSpanCreate(tHead) : null,
                middle is { } tMiddle ? TBridgeSpanCreate(tMiddle) : null,
                tail is { } tTail ? TBridgeSpanCreate(tTail) : null),
            source ?? TSourceStreamCreate(work.LWorkSourceMedia?.LWorkMediaCodec ?? "h264"));

    internal static IReadOnlyList<LEncodeStage> TBridgePlanBuild(
        LWorkItem work,
        LBridgePlan plan,
        LBridgeStream? source = null,
        string? intermediateExtension = null) =>
        LEncode.LEncodeSmartBuild(
            work,
            plan,
            source ?? TSourceStreamCreate(work.LWorkSourceMedia?.LWorkMediaCodec ?? "h264"),
            intermediateExtension);

    internal static IReadOnlyList<LEncodeStage> TBridgeDecodeBuild(
        LWorkItem work,
        (double origin, double end) interval,
        (double origin, double end)? head,
        (double origin, double end, double decodeEnd) middle,
        (double origin, double end)? tail,
        LBridgeStream? source = null) =>
        TBridgePlanBuild(
            work,
            new LBridgePlan(
                LBridgeOutcome.LBridgeOutcomeSmart,
                TBridgeSpanCreate(interval),
                head is { } tHead ? TBridgeSpanCreate(tHead) : null,
                new LBridgeSpan(
                    TimeSpan.FromSeconds(middle.origin),
                    TimeSpan.FromSeconds(middle.end),
                    TimeSpan.FromSeconds(middle.decodeEnd)),
                tail is { } tTail ? TBridgeSpanCreate(tTail) : null),
            source);

    internal static IReadOnlyList<LEncodeStage> TBridgeResolve(
        LWorkItem work, params double[] keyframes) =>
        LEncode.LEncodeBridgeResolve(
            work, keyframes.Select(TimeSpan.FromSeconds).ToArray());

    internal static bool? TAudioIntervalRead(string source, double origin, double end) =>
        LScoutAudio.LScoutAudioRead(
            source, TimeSpan.FromSeconds(origin), TimeSpan.FromSeconds(end));

    internal static IReadOnlyList<LKeyframeEntry> TKeyframeRead(string source, double origin, double end) =>
        LKeyframeSeeker.LKeyframeRangeScan(
            source, TimeSpan.FromSeconds(origin), TimeSpan.FromSeconds(end));

    internal static string TToolFfmpegRead() => LTool.LToolFfmpegRead();

    internal static string TToolFfprobeRead() => LTool.LToolFfprobeRead();

    internal static LWorkItem TBridgeIntervalCreate(
        string source,
        string output,
        double origin,
        double end,
        string audioStream,
        string container = "matroska",
        string extension = "mkv")
    {
        LWorkItem work = TWorkCreate(
            LWorkKind.LWorkKindSplit,
            source,
            output,
            TOutputCreate(
                container: container,
                extension: extension,
                videoMode: "Smart",
                audioStream: audioStream,
                audioMode: "Copy"),
            TimeSpan.FromSeconds(origin),
            TimeSpan.FromSeconds(end));
        work.LWorkSourceMedia = new LWorkMedia(160, 90, 30, 8_000, true)
        {
            LWorkMediaCodec = "h264",
            LWorkAudioCodec = "aac",
            LWorkMediaSamplerate = 48_000
        };
        return work;
    }

    internal static LWorkItem TVideoIntervalCreate(
        string source,
        string output,
        double origin,
        double end,
        string videoMode)
    {
        LWorkItem work = TWorkCreate(
            LWorkKind.LWorkKindSplit,
            source,
            output,
            TOutputCreate(
                container: "matroska",
                extension: "mkv",
                videoMode: videoMode,
                audioStream: "Exclude",
                audioMode: "Exclude"),
            TimeSpan.FromSeconds(origin),
            TimeSpan.FromSeconds(end));
        work.LWorkSourceMedia = new LWorkMedia(
            160, 90, 30, Math.Max(12_000, (long)Math.Ceiling(end * 1_000)), false)
        {
            LWorkMediaCodec = "h264",
            LWorkAudioCodec = string.Empty
        };
        return work;
    }

    internal static IReadOnlyList<LEncodeStage> TBridgeSourceBuild(LWorkItem work)
    {
        IReadOnlyList<LKeyframeEntry> keyframes = LScoutBridge.LScoutBridgeRead(
            work.LWorkSourcePath, work.LWorkOrigin, work.LWorkEnd);
        LWorkMedia? media = LScout.LScoutMediaRead(work.LWorkSourcePath);
        bool openEnd = LBridge.LBridgeEndCheck(
            work.LWorkEnd,
            media?.LWorkMediaDuration ?? TimeSpan.Zero,
            media?.LWorkMediaFramerate ?? 0);
        LBridgePlan plan = LBridge.LBridgeRegionResolve(keyframes, work.LWorkOrigin, work.LWorkEnd, openEnd);
        return LEncode.LEncodeSmartBuild(work, plan, LScoutStream.LScoutStreamRead(work.LWorkSourcePath));
    }

    internal static IReadOnlyList<LEncodeStage> TBridgeMissingBuild(LWorkItem work) =>
        LEncode.LEncodeSmartBuild(
            work,
            new LBridgePlan(
                LBridgeOutcome.LBridgeOutcomeSmart,
                new LBridgeSpan(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)),
                null,
                new LBridgeSpan(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)),
                null),
            TSourceStreamCreate("h264"));

    internal static LWorkItem TBridgeCropCreate(string source, string output)
    {
        LWorkItem work = TWorkCreate(
            LWorkKind.LWorkKindSplit, source, output,
            TOutputCreate(videoMode: "Smart", audioMode: "Copy"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30),
            crop: new LWorkCrop(20, 0, 0, 0, 0, false, false));
        work.LWorkSourceMedia = new LWorkMedia(1920, 1080, 30, 30_000, true) { LWorkMediaCodec = "h264" };
        return work;
    }

    private static LBridgeSpan TBridgeSpanCreate((double origin, double end) span) =>
        new(
            TimeSpan.FromSeconds(span.origin),
            TimeSpan.FromSeconds(span.end),
            TimeSpan.FromSeconds(span.end));

    public void Dispose()
    {
        if (tEncodeDisposed)
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

        tEncodeDisposed = true;
    }
}

/// <summary>
/// Locks the three video modes (Copy / Smart / Encode) and the legacy Auto normalization
/// through the production stage builder.
/// </summary>
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

        // Interval is [10, 30]; interior keyframes at 12 and 28 align with neither bound.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeResolve(work, 12, 28);

        // head, middle, tail — each followed by its MPEG-TS join piece — plus the join.
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
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30)));
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
