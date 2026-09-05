using System.Globalization;

using Cadroue.Core;

namespace Cadroue.Tests;

/// <summary>
/// Owns the temporary folder for one bridged-resilience test and synthesizes the awkward
/// source media it needs: delayed audio tracks, reordered B-frame GOPs, non-zero timelines.
/// Creation only; it asserts nothing and measures nothing.
/// </summary>
internal sealed class TBridgeFixture : IDisposable
{
    private readonly string tBridgeRoot = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "SmartResilience", Guid.NewGuid().ToString("N"));

    public TBridgeFixture()
    {
        Directory.CreateDirectory(tBridgeRoot);
    }

    internal string TBridgeFixtureRoot => tBridgeRoot;

    internal string TBridgeDelayedCreate()
    {
        string path = Path.Combine(tBridgeRoot, "delayed-multiple-audio.mkv");
        TBridgeMetric.TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            "-hide_banner -loglevel error "
            + "-f lavfi -i testsrc2=size=160x90:rate=30:duration=8 "
            + "-itsoffset 3 -f lavfi -i sine=frequency=440:sample_rate=48000:duration=5 "
            + "-itsoffset 3.25 -f lavfi -i sine=frequency=880:sample_rate=48000:duration=4.75 "
            + "-map 0:v:0 -map 1:a:0 -map 2:a:0 -c:v libx264 -preset ultrafast -g 60 "
            + $"-c:a aac -y {TBridgeMetric.TBridgePathFormat(path)}");
        return path;
    }

    internal string TBridgeSyncCreate()
    {
        string path = Path.Combine(tBridgeRoot, "synchronized-audio.mkv");
        TBridgeMetric.TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            "-hide_banner -loglevel error "
            + "-f lavfi -i testsrc2=size=160x90:rate=30:duration=8 "
            + "-f lavfi -i sine=frequency=440:sample_rate=48000:duration=8 "
            + "-map 0:v:0 -map 1:a:0 -c:v libx264 -preset ultrafast -g 60 "
            + $"-c:a aac -y {TBridgeMetric.TBridgePathFormat(path)}");
        return path;
    }

    internal string TBridgeReorderCreate(
        string name,
        double duration,
        int keyframeInterval,
        int sampleRate = 48_000,
        int videoTimescale = 0,
        string videoRate = "24000/1001")
    {
        string path = Path.Combine(tBridgeRoot, name);
        string timescale = videoTimescale > 0 ? $" -video_track_timescale {videoTimescale}" : string.Empty;
        TBridgeMetric.TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            "-hide_banner -loglevel error "
            + $"-f lavfi -i testsrc2=size=160x90:rate={videoRate}:duration={duration.ToString(CultureInfo.InvariantCulture)} "
            + $"-f lavfi -i sine=frequency=440:sample_rate={sampleRate}:duration={duration.ToString(CultureInfo.InvariantCulture)} "
            + "-map 0:v:0 -map 1:a:0 -c:v libx264 -preset medium -bf 3 "
            + $"-g {keyframeInterval} -keyint_min {keyframeInterval} -sc_threshold 0 "
            + $"-c:a aac{timescale} -y {TBridgeMetric.TBridgePathFormat(path)}");
        return path;
    }

    internal string TBridgeOffsetCreate()
    {
        string source = TBridgeReorderCreate("offset-base.mp4", 24, 96, 44_100);
        string offset = Path.Combine(tBridgeRoot, "offset-source.mp4");
        TBridgeMetric.TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -itsoffset 3.4 -i {TBridgeMetric.TBridgePathFormat(source)} -map 0 -c copy -y {TBridgeMetric.TBridgePathFormat(offset)}");
        return offset;
    }

    internal static LWorkItem TBridgeWorkCreate(
        string source,
        double origin,
        double end,
        string audioStream,
        bool mp4Output = false)
    {
        string extension = mp4Output ? "mp4" : "mkv";
        string output = Path.Combine(Path.GetDirectoryName(source) ?? string.Empty, $"smart-output.{extension}");
        return TEncodeCommand.TBridgeIntervalCreate(
            source,
            output,
            origin,
            end,
            audioStream,
            mp4Output ? "mp4" : "matroska",
            extension);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(tBridgeRoot))
            {
                Directory.Delete(tBridgeRoot, true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
