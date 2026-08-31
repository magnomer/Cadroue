using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TBridgeResilience : IDisposable
{
    private readonly string tBridgeRoot = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "SmartResilience", Guid.NewGuid().ToString("N"));

    public TBridgeResilience()
    {
        Directory.CreateDirectory(tBridgeRoot);
    }

    [Fact]
    public void DelayedMultipleAudioTracks_PreserveCutRelativeOffsets()
    {
        string source = TBridgeDelayedCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate(source, 1.1, 6.4, "Include all audio tracks");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work, (1.1, 6.4), (1.1, 2), (2, 6, 5.933), (6, 6.4));
        Assert.Single(stages, stage => stage.LEncodeStageLabel == "Copying audio");

        foreach (LEncodeStage stage in stages)
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        IReadOnlyDictionary<int, double> starts = TBridgePacketRead(work.LWorkOutputPath);

        Assert.Equal(2, starts.Count);
        Assert.InRange(TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(starts.Values.Min(), 1.85, 1.95);
        Assert.InRange(starts.Values.Max() - starts.Values.Min(), 0.20, 0.30);
    }

    [Fact]
    public void OrdinaryAudioAndVideo_StartTogetherAfterAccurateCut()
    {
        string source = TBridgeSyncCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate(source, 1.1, 6.4, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work, (1.1, 6.4), (1.1, 2), (2, 6, 5.933), (6, 6.4));

        foreach (LEncodeStage stage in stages)
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(TBridgeFirstRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.InRange(TBridgeFormatRead(work.LWorkOutputPath), 5.25, 5.37);
    }

    [Fact]
    public void MatroskaBFramesWithFourSecondGops_DoNotLengthenVideoPastAudio()
    {
        string source = TBridgeReorderCreate("four-second-gops.mkv", 24, 96);
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate(source, 1.1, 22.5, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TBridgeFormatRead(work.LWorkOutputPath), 21.35, 21.47);
        Assert.InRange(TBridgeCountRead(work.LWorkOutputPath), 510, 516);
    }

    [Fact]
    public void SubMillisecondMp4Keyframe_PreservesFirstCopiedGop()
    {
        string source = TBridgeReorderCreate("submillisecond-keyframes.mp4", 20, 49);
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate(source, 1, 18, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TBridgeFormatRead(work.LWorkOutputPath), 16.95, 17.07);
        Assert.InRange(TBridgeCountRead(work.LWorkOutputPath), 405, 411);
        Assert.True(string.IsNullOrWhiteSpace(TBridgeErrorRead(work.LWorkOutputPath)));
    }

    [Fact]
    public void RoundedKeyframeAlignedMp4Cut_UsesOnePassAndSurvivesFullTranscode()
    {
        string source = TBridgeReorderCreate("rounded-keyframe-aligned.mp4", 20, 49, 44_100);
        using var environment = new TEncodeCommand();
        // The actual packet boundaries are 2.043708s and 18.393375s. UI and
        // sidecar times are millisecond-based, so both ends arrive rounded.
        LWorkItem work = TBridgeWorkCreate(source, 2.044, 18.393, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);
        Assert.Single(stages);
        Assert.Equal("Copying", stages[0].LEncodeStageLabel);

        foreach (LEncodeStage stage in stages)
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        double videoStart = TBridgeFirstRead(work.LWorkOutputPath, "v:0");
        double audioStart = TBridgeFirstRead(work.LWorkOutputPath, "a:0");
        Assert.InRange(Math.Abs(videoStart - audioStart), 0, 0.11);
        // Simultaneous stream copy retains codec preroll just like ordinary Copy;
        // the later decode must preserve it instead of silently dropping audio.
        Assert.InRange(TBridgeFormatRead(work.LWorkOutputPath), 16.45, 16.60);
        Assert.InRange(TBridgeCountRead(work.LWorkOutputPath), 388, 395);
        Assert.True(string.IsNullOrWhiteSpace(TBridgeErrorRead(work.LWorkOutputPath)));

        string converted = Path.Combine(tBridgeRoot, "smart-output-converted.mp4");
        TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -i {TBridgePathFormat(work.LWorkOutputPath)} "
            + $"-map 0:v:0 -map 0:a:0 -c:v libx264 -preset ultrafast -c:a aac -y {TBridgePathFormat(converted)}");
        double smartAudioDuration = TBridgeDecodeRead(work.LWorkOutputPath, "smart-decoded.pcm", 44_100, 1);
        double convertedAudioDuration = TBridgeDecodeRead(converted, "converted-decoded.pcm", 44_100, 1);
        Assert.InRange(Math.Abs(smartAudioDuration - convertedAudioDuration), 0, 0.05);
    }

    [Fact]
    public void Mp4NonZeroTimeline_KeepsAudioAndVideoAtRequestedDuration()
    {
        string source = TBridgeOffsetCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate(source, 10.1, 22.5, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TBridgeFormatRead(work.LWorkOutputPath), 12.35, 12.47);
        Assert.InRange(TBridgeCountRead(work.LWorkOutputPath), 294, 301);
        Assert.InRange(TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(TBridgeFirstRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.True(string.IsNullOrWhiteSpace(TBridgeErrorRead(work.LWorkOutputPath)));
    }

    [Fact]
    public void Mp4NonZeroTimeline_KeyframeScanCoversRequestedEnd()
    {
        string source = TBridgeOffsetCreate();

        IReadOnlyList<LKeyframeEntry> keyframes = TEncodeCommand.TKeyframeRead(source, 7.9, 20.1);

        Assert.Equal(4, keyframes.Count);
        Assert.InRange(keyframes[0].LKeyframePresentationTime.TotalSeconds, 8, 8.1);
        Assert.InRange(keyframes[^1].LKeyframePresentationTime.TotalSeconds, 20, 20.1);
    }

    [Fact]
    public void ThirtyFpsMp4Hybrid_PreservesFrameRateAndAudioTimeline()
    {
        string source = TBridgeReorderCreate(
            "thirty-fps-hybrid.mp4",
            12,
            60,
            videoTimescale: 90_000,
            videoRate: "30");
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate(source, 1.1, 10.5, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        foreach (LEncodeStage stage in stages)
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.Equal("30/1", TBridgeFramerateRead(work.LWorkOutputPath));
        Assert.Equal("1/90000", TBridgeTimebaseRead(work.LWorkOutputPath));
        Assert.InRange(TBridgeFormatRead(work.LWorkOutputPath), 9.35, 9.47);
        Assert.InRange(TBridgeStreamRead(work.LWorkOutputPath, "v:0"), 9.35, 9.47);
        Assert.InRange(TBridgeStreamRead(work.LWorkOutputPath, "a:0"), 9.35, 9.47);
        Assert.InRange(TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(TBridgeFirstRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.True(string.IsNullOrWhiteSpace(TBridgeErrorRead(work.LWorkOutputPath)));
    }

    [Theory]
    [InlineData(16_000)]
    [InlineData(24_000)]
    public void Mp4SmartRoutes_PreserveSourceVideoTimeBaseAndRemainMergeable(int sourceTimescale)
    {
        string source = TBridgeReorderCreate(
            $"mixed-smart-routes-{sourceTimescale}.mp4",
            12,
            48,
            48_000,
            sourceTimescale);
        using var environment = new TEncodeCommand();
        LWorkItem shortEncoded = TEncodeCommand.TBridgeIntervalCreate(
            source,
            Path.Combine(tBridgeRoot, $"short-smart-{sourceTimescale}.mp4"),
            0.1,
            1.8,
            "Include",
            "mp4",
            "mp4");
        LWorkItem hybrid = TEncodeCommand.TBridgeIntervalCreate(
            source,
            Path.Combine(tBridgeRoot, $"hybrid-smart-{sourceTimescale}.mp4"),
            2.1,
            10.5,
            "Include",
            "mp4",
            "mp4");

        IReadOnlyList<LEncodeStage> shortStages = TEncodeCommand.TBridgeSourceBuild(shortEncoded);
        IReadOnlyList<LEncodeStage> hybridStages = TEncodeCommand.TBridgeSourceBuild(hybrid);
        Assert.Single(shortStages);
        Assert.Contains(hybridStages, stage => stage.LEncodeStageLabel == "Copying middle");

        foreach (LEncodeStage stage in shortStages.Concat(hybridStages))
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        string sourceTimeBase = TBridgeTimebaseRead(source);
        Assert.Equal($"1/{sourceTimescale}", sourceTimeBase);
        Assert.Equal(sourceTimeBase, TBridgeTimebaseRead(shortEncoded.LWorkOutputPath));
        Assert.Equal(sourceTimeBase, TBridgeTimebaseRead(hybrid.LWorkOutputPath));

        string mergeList = Path.Combine(tBridgeRoot, $"smart-merge-{sourceTimescale}.txt");
        string merged = Path.Combine(tBridgeRoot, $"smart-merged-{sourceTimescale}.mp4");
        File.WriteAllLines(mergeList,
        [
            $"file '{shortEncoded.LWorkOutputPath.Replace("'", "'\\''", StringComparison.Ordinal)}'",
            $"file '{hybrid.LWorkOutputPath.Replace("'", "'\\''", StringComparison.Ordinal)}'"
        ]);
        TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -f concat -safe 0 -i {TBridgePathFormat(mergeList)} -c copy -y {TBridgePathFormat(merged)}");

        double mergedVideoDuration = TBridgeStreamRead(merged, "v:0");
        double mergedAudioDuration = TBridgeStreamRead(merged, "a:0");
        Assert.InRange(Math.Abs(mergedVideoDuration - mergedAudioDuration), 0, 0.12);
    }

    [Fact]
    public void AudioOutsideCut_OmitsInvalidIntermediateAndStillBuildsVideoMux()
    {
        string source = TBridgeDelayedCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate(source, 0, 0.5, "Include all audio tracks");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work, (0, 0.5), null, (0, 0.5, 0.5), null);

        Assert.False(TEncodeCommand.TAudioIntervalRead(source, 0, 0.5));
        LEncodeStage copy = Assert.Single(stages);
        Assert.Equal("Copying", copy.LEncodeStageLabel);
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(TEncodeToken.TEncodeTokenRead(copy.LEncodeStageArguments), "-i"));

        foreach (LEncodeStage stage in stages)
        {
            TBridgeRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.True(File.Exists(work.LWorkOutputPath));
        Assert.InRange(TBridgeFirstRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
    }

    [Fact]
    public void MissingMiddleDecodeCutoff_DoesNotConvertSmartToWholeEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate("missing-source.mp4", 1, 5, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeMissingBuild(work);

        LEncodeStage copy = Assert.Single(stages);
        Assert.Equal("Copying", copy.LEncodeStageLabel);
        Assert.Equal(
            "copy",
            TEncodeToken.TEncodeOptionRead(TEncodeToken.TEncodeTokenRead(copy.LEncodeStageArguments), "-c:v"));
    }

    [Fact]
    public void AudioProbeFailure_DoesNotConvertSmartToWholeEncode()
    {
        string source = Path.Combine(tBridgeRoot, "unprobeable-source.mkv");
        File.WriteAllText(source, "not a media file");
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeWorkCreate(source, 1.1, 6.4, "Include");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work, (1.1, 6.4), (1.1, 2), (2, 6, 5.933), (6, 6.4));

        TBridgeMiddleCheck(stages);
    }

    private static void TBridgeMiddleCheck(IReadOnlyList<LEncodeStage> stages)
    {
        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        Assert.Contains(stages, stage =>
            TEncodeToken.TEncodeOptionRead(TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments), "-c:v") == "copy");
        Assert.False(stages.Count == 1 && stages[0].LEncodeStageLabel == "Encoding");
    }

    private string TBridgeDelayedCreate()
    {
        string path = Path.Combine(tBridgeRoot, "delayed-multiple-audio.mkv");
        TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            "-hide_banner -loglevel error "
            + "-f lavfi -i testsrc2=size=160x90:rate=30:duration=8 "
            + "-itsoffset 3 -f lavfi -i sine=frequency=440:sample_rate=48000:duration=5 "
            + "-itsoffset 3.25 -f lavfi -i sine=frequency=880:sample_rate=48000:duration=4.75 "
            + "-map 0:v:0 -map 1:a:0 -map 2:a:0 -c:v libx264 -preset ultrafast -g 60 "
            + $"-c:a aac -y {TBridgePathFormat(path)}");
        return path;
    }

    private string TBridgeSyncCreate()
    {
        string path = Path.Combine(tBridgeRoot, "synchronized-audio.mkv");
        TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            "-hide_banner -loglevel error "
            + "-f lavfi -i testsrc2=size=160x90:rate=30:duration=8 "
            + "-f lavfi -i sine=frequency=440:sample_rate=48000:duration=8 "
            + "-map 0:v:0 -map 1:a:0 -c:v libx264 -preset ultrafast -g 60 "
            + $"-c:a aac -y {TBridgePathFormat(path)}");
        return path;
    }

    private string TBridgeReorderCreate(
        string name,
        double duration,
        int keyframeInterval,
        int sampleRate = 48_000,
        int videoTimescale = 0,
        string videoRate = "24000/1001")
    {
        string path = Path.Combine(tBridgeRoot, name);
        string timescale = videoTimescale > 0 ? $" -video_track_timescale {videoTimescale}" : string.Empty;
        TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            "-hide_banner -loglevel error "
            + $"-f lavfi -i testsrc2=size=160x90:rate={videoRate}:duration={duration.ToString(CultureInfo.InvariantCulture)} "
            + $"-f lavfi -i sine=frequency=440:sample_rate={sampleRate}:duration={duration.ToString(CultureInfo.InvariantCulture)} "
            + "-map 0:v:0 -map 1:a:0 -c:v libx264 -preset medium -bf 3 "
            + $"-g {keyframeInterval} -keyint_min {keyframeInterval} -sc_threshold 0 "
            + $"-c:a aac{timescale} -y {TBridgePathFormat(path)}");
        return path;
    }

    private string TBridgeOffsetCreate()
    {
        string source = TBridgeReorderCreate("offset-base.mp4", 24, 96, 44_100);
        string offset = Path.Combine(tBridgeRoot, "offset-source.mp4");
        TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -itsoffset 3.4 -i {TBridgePathFormat(source)} -map 0 -c copy -y {TBridgePathFormat(offset)}");
        return offset;
    }

    private static LWorkItem TBridgeWorkCreate(
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

    private static IReadOnlyDictionary<int, double> TBridgePacketRead(string path)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -select_streams a -show_packets -show_entries packet=stream_index,pts_time -of csv=p=0 {TBridgePathFormat(path)}");
        var starts = new Dictionary<int, double>();
        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 2
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int stream)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double pts))
            {
                starts.TryAdd(stream, pts);
            }
        }

        return starts;
    }

    private static double TBridgeFirstRead(string path, string stream)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -select_streams {stream} -show_packets -show_entries packet=pts_time -of csv=p=0 {TBridgePathFormat(path)}");
        string first = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).First().Split(',')[0];
        Assert.True(double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out double pts));
        return pts;
    }

    private static double TBridgeFormatRead(string path)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -show_entries format=duration -of default=nw=1:nk=1 {TBridgePathFormat(path)}");
        Assert.True(double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration));
        return duration;
    }

    private static int TBridgeCountRead(string path)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of default=nw=1:nk=1 {TBridgePathFormat(path)}");
        Assert.True(int.TryParse(output.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count));
        return count;
    }

    private static string TBridgeTimebaseRead(string path) => TBridgeRun(
        TEncodeCommand.TToolFfprobeRead(),
        $"-v error -select_streams v:0 -show_entries stream=time_base -of default=nw=1:nk=1 {TBridgePathFormat(path)}").Trim();

    private static string TBridgeFramerateRead(string path) => TBridgeRun(
        TEncodeCommand.TToolFfprobeRead(),
        $"-v error -select_streams v:0 -show_entries stream=avg_frame_rate -of default=nw=1:nk=1 {TBridgePathFormat(path)}").Trim();

    private static double TBridgeStreamRead(string path, string stream)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -select_streams {stream} -show_entries stream=duration -of default=nw=1:nk=1 {TBridgePathFormat(path)}");
        Assert.True(double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration));
        return duration;
    }

    private double TBridgeDecodeRead(string path, string outputName, int sampleRate, int channels)
    {
        string decoded = Path.Combine(tBridgeRoot, outputName);
        TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -i {TBridgePathFormat(path)} -map 0:a:0 "
            + $"-c:a pcm_s16le -f s16le -y {TBridgePathFormat(decoded)}");
        return new FileInfo(decoded).Length / (double)(sizeof(short) * sampleRate * channels);
    }

    private static string TBridgeErrorRead(string path)
    {
        var start = new ProcessStartInfo(TEncodeCommand.TToolFfmpegRead())
        {
            Arguments = $"-hide_banner -loglevel error -i {TBridgePathFormat(path)} -map 0:v:0 -f null -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using Process process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start FFmpeg");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        output.GetAwaiter().GetResult();
        return error;
    }

    private static string TBridgeRun(string program, string arguments)
    {
        var start = new ProcessStartInfo(program)
        {
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using Process process = Process.Start(start)
            ?? throw new InvalidOperationException($"Could not start {program}");
        Task<string> error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        string errorText = error.GetAwaiter().GetResult();
        Assert.True(process.ExitCode == 0, $"{program} failed ({process.ExitCode}): {errorText}");
        return output;
    }

    private static string TBridgePathFormat(string path) => '"' + path + '"';

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
