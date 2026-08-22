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
            "{OriginalName}", container, extension, "Same as source", string.Empty, "Transcode",
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
        string audioCodec = "aac", int sampleRate = 48000)
    {
        LEncoding encoding = copyMode
            ? OutputCreate(videoMode: "Copy", audioMode: "Copy")
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

    internal static IReadOnlyList<LEncodeStage> SmartStagesBuild(
        LWorkItem work,
        LBridgeOutcome outcome,
        (double origin, double end) interval,
        (double origin, double end)? head,
        (double origin, double end)? middle,
        (double origin, double end)? tail) =>
        LEncode.LEncodeSmartBuild(work, new LBridgePlan(
            outcome,
            SpanCreate(interval),
            head is { } tHead ? SpanCreate(tHead) : null,
            middle is { } tMiddle ? SpanCreate(tMiddle) : null,
            tail is { } tTail ? SpanCreate(tTail) : null));

    internal static IReadOnlyList<LEncodeStage> SmartResolveBuild(
        LWorkItem work,
        LBridgeOutcome outcome,
        (double origin, double end) interval,
        (double origin, double end)? head,
        (double origin, double end)? middle,
        (double origin, double end)? tail,
        bool compatible = true,
        LBridgeReason reason = LBridgeReason.LBridgeReasonCompatible) =>
        LEncode.LEncodeSmartResolve(
            work,
            new LBridgePlan(
                outcome,
                SpanCreate(interval),
                head is { } tHead ? SpanCreate(tHead) : null,
                middle is { } tMiddle ? SpanCreate(tMiddle) : null,
                tail is { } tTail ? SpanCreate(tTail) : null),
            new LBridgeCompatibility(compatible, reason));

    internal static LWorkItem SmartCropWorkCreate(string source, string output)
    {
        LWorkItem work = WorkCreate(
            LWorkKind.LWorkKindSplit, source, output,
            OutputCreate(videoMode: "Copy", audioMode: "Copy"),
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
