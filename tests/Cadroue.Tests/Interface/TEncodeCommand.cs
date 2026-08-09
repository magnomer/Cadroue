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
