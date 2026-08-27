using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class SmartEncodingResilienceTests : IDisposable
{
    private readonly string tRoot = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "SmartResilience", Guid.NewGuid().ToString("N"));

    public SmartEncodingResilienceTests()
    {
        Directory.CreateDirectory(tRoot);
    }

    [Fact]
    public void DelayedMultipleAudioTracks_PreserveCutRelativeOffsets()
    {
        string source = DelayedAudioSourceCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 1.1, 6.4, "Include all audio tracks");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartDecodedStagesBuild(
            work, (1.1, 6.4), (1.1, 2), (2, 6, 5.933), (6, 6.4));
        Assert.Single(stages, stage => stage.LEncodeStageLabel == "Copying audio");

        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        IReadOnlyDictionary<int, double> starts = AudioPacketStartsRead(work.LWorkOutputPath);

        Assert.Equal(2, starts.Count);
        Assert.InRange(FirstPacketStartRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(starts.Values.Min(), 1.85, 1.95);
        Assert.InRange(starts.Values.Max() - starts.Values.Min(), 0.20, 0.30);
    }

    [Fact]
    public void OrdinaryAudioAndVideo_StartTogetherAfterAccurateCut()
    {
        string source = SynchronizedAudioSourceCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 1.1, 6.4, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartDecodedStagesBuild(
            work, (1.1, 6.4), (1.1, 2), (2, 6, 5.933), (6, 6.4));

        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(FirstPacketStartRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(FirstPacketStartRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.InRange(FormatDurationRead(work.LWorkOutputPath), 5.25, 5.37);
    }

    [Fact]
    public void MatroskaBFramesWithFourSecondGops_DoNotLengthenVideoPastAudio()
    {
        string source = ReorderedSourceCreate("four-second-gops.mkv", 24, 96);
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 1.1, 22.5, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartSourceStagesBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(FormatDurationRead(work.LWorkOutputPath), 21.35, 21.47);
        Assert.InRange(VideoPacketCountRead(work.LWorkOutputPath), 510, 516);
    }

    [Fact]
    public void SubMillisecondMp4Keyframe_PreservesFirstCopiedGop()
    {
        string source = ReorderedSourceCreate("submillisecond-keyframes.mp4", 20, 49);
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 1, 18, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartSourceStagesBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(FormatDurationRead(work.LWorkOutputPath), 16.95, 17.07);
        Assert.InRange(VideoPacketCountRead(work.LWorkOutputPath), 405, 411);
        Assert.True(string.IsNullOrWhiteSpace(DecodeErrorsRead(work.LWorkOutputPath)));
    }

    [Fact]
    public void RoundedKeyframeAlignedMp4Cut_UsesOnePassAndSurvivesFullTranscode()
    {
        string source = ReorderedSourceCreate("rounded-keyframe-aligned.mp4", 20, 49, 44_100);
        using var environment = new TEncodeCommand();
        // The actual packet boundaries are 2.043708s and 18.393375s. UI and
        // sidecar times are millisecond-based, so both ends arrive rounded.
        LWorkItem work = SmartWorkCreate(source, 2.044, 18.393, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartSourceStagesBuild(work);
        Assert.Single(stages);
        Assert.Equal("Copying", stages[0].LEncodeStageLabel);

        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        double videoStart = FirstPacketStartRead(work.LWorkOutputPath, "v:0");
        double audioStart = FirstPacketStartRead(work.LWorkOutputPath, "a:0");
        Assert.InRange(Math.Abs(videoStart - audioStart), 0, 0.11);
        // Simultaneous stream copy retains codec preroll just like ordinary Copy;
        // the later decode must preserve it instead of silently dropping audio.
        Assert.InRange(FormatDurationRead(work.LWorkOutputPath), 16.45, 16.60);
        Assert.InRange(VideoPacketCountRead(work.LWorkOutputPath), 388, 395);
        Assert.True(string.IsNullOrWhiteSpace(DecodeErrorsRead(work.LWorkOutputPath)));

        string converted = Path.Combine(tRoot, "smart-output-converted.mp4");
        Run(
            TEncodeCommand.FfmpegRead(),
            $"-hide_banner -loglevel error -i {Quote(work.LWorkOutputPath)} "
            + $"-map 0:v:0 -map 0:a:0 -c:v libx264 -preset ultrafast -c:a aac -y {Quote(converted)}");
        double smartAudioDuration = DecodedAudioDurationRead(work.LWorkOutputPath, "smart-decoded.pcm", 44_100, 1);
        double convertedAudioDuration = DecodedAudioDurationRead(converted, "converted-decoded.pcm", 44_100, 1);
        Assert.InRange(Math.Abs(smartAudioDuration - convertedAudioDuration), 0, 0.05);
    }

    [Fact]
    public void Mp4NonZeroTimeline_KeepsAudioAndVideoAtRequestedDuration()
    {
        string source = OffsetMp4SourceCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 10.1, 22.5, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartSourceStagesBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(FormatDurationRead(work.LWorkOutputPath), 12.35, 12.47);
        Assert.InRange(VideoPacketCountRead(work.LWorkOutputPath), 294, 301);
        Assert.InRange(FirstPacketStartRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(FirstPacketStartRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.True(string.IsNullOrWhiteSpace(DecodeErrorsRead(work.LWorkOutputPath)));
    }

    [Fact]
    public void Mp4NonZeroTimeline_KeyframeScanCoversRequestedEnd()
    {
        string source = OffsetMp4SourceCreate();

        IReadOnlyList<LKeyframeEntry> keyframes = TEncodeCommand.KeyframesRead(source, 7.9, 20.1);

        Assert.Equal(4, keyframes.Count);
        Assert.InRange(keyframes[0].LKeyframePresentationTime.TotalSeconds, 8, 8.1);
        Assert.InRange(keyframes[^1].LKeyframePresentationTime.TotalSeconds, 20, 20.1);
    }

    [Fact]
    public void ThirtyFpsMp4Hybrid_PreservesFrameRateAndAudioTimeline()
    {
        string source = ReorderedSourceCreate(
            "thirty-fps-hybrid.mp4",
            12,
            60,
            videoTimescale: 90_000,
            videoRate: "30");
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 1.1, 10.5, "Include", true);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartSourceStagesBuild(work);

        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.Equal("30/1", VideoFrameRateRead(work.LWorkOutputPath));
        Assert.Equal("1/90000", VideoTimeBaseRead(work.LWorkOutputPath));
        Assert.InRange(FormatDurationRead(work.LWorkOutputPath), 9.35, 9.47);
        Assert.InRange(StreamDurationRead(work.LWorkOutputPath, "v:0"), 9.35, 9.47);
        Assert.InRange(StreamDurationRead(work.LWorkOutputPath, "a:0"), 9.35, 9.47);
        Assert.InRange(FirstPacketStartRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
        Assert.InRange(FirstPacketStartRead(work.LWorkOutputPath, "a:0"), -0.05, 0.05);
        Assert.True(string.IsNullOrWhiteSpace(DecodeErrorsRead(work.LWorkOutputPath)));
    }

    [Theory]
    [InlineData(16_000)]
    [InlineData(24_000)]
    public void Mp4SmartRoutes_PreserveSourceVideoTimeBaseAndRemainMergeable(int sourceTimescale)
    {
        string source = ReorderedSourceCreate(
            $"mixed-smart-routes-{sourceTimescale}.mp4",
            12,
            48,
            48_000,
            sourceTimescale);
        using var environment = new TEncodeCommand();
        LWorkItem shortEncoded = TEncodeCommand.SmartIntervalWorkCreate(
            source,
            Path.Combine(tRoot, $"short-smart-{sourceTimescale}.mp4"),
            0.1,
            1.8,
            "Include",
            "mp4",
            "mp4");
        LWorkItem hybrid = TEncodeCommand.SmartIntervalWorkCreate(
            source,
            Path.Combine(tRoot, $"hybrid-smart-{sourceTimescale}.mp4"),
            2.1,
            10.5,
            "Include",
            "mp4",
            "mp4");

        IReadOnlyList<LEncodeStage> shortStages = TEncodeCommand.SmartSourceStagesBuild(shortEncoded);
        IReadOnlyList<LEncodeStage> hybridStages = TEncodeCommand.SmartSourceStagesBuild(hybrid);
        Assert.Single(shortStages);
        Assert.Contains(hybridStages, stage => stage.LEncodeStageLabel == "Copying middle");

        foreach (LEncodeStage stage in shortStages.Concat(hybridStages))
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        string sourceTimeBase = VideoTimeBaseRead(source);
        Assert.Equal($"1/{sourceTimescale}", sourceTimeBase);
        Assert.Equal(sourceTimeBase, VideoTimeBaseRead(shortEncoded.LWorkOutputPath));
        Assert.Equal(sourceTimeBase, VideoTimeBaseRead(hybrid.LWorkOutputPath));

        string mergeList = Path.Combine(tRoot, $"smart-merge-{sourceTimescale}.txt");
        string merged = Path.Combine(tRoot, $"smart-merged-{sourceTimescale}.mp4");
        File.WriteAllLines(mergeList,
        [
            $"file '{shortEncoded.LWorkOutputPath.Replace("'", "'\\''", StringComparison.Ordinal)}'",
            $"file '{hybrid.LWorkOutputPath.Replace("'", "'\\''", StringComparison.Ordinal)}'"
        ]);
        Run(
            TEncodeCommand.FfmpegRead(),
            $"-hide_banner -loglevel error -f concat -safe 0 -i {Quote(mergeList)} -c copy -y {Quote(merged)}");

        double mergedVideoDuration = StreamDurationRead(merged, "v:0");
        double mergedAudioDuration = StreamDurationRead(merged, "a:0");
        Assert.InRange(Math.Abs(mergedVideoDuration - mergedAudioDuration), 0, 0.12);
    }

    [Fact]
    public void AudioOutsideCut_OmitsInvalidIntermediateAndStillBuildsVideoMux()
    {
        string source = DelayedAudioSourceCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 0, 0.5, "Include all audio tracks");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartDecodedStagesBuild(
            work, (0, 0.5), null, (0, 0.5, 0.5), null);

        Assert.False(TEncodeCommand.AudioIntervalRead(source, 0, 0.5));
        LEncodeStage copy = Assert.Single(stages);
        Assert.Equal("Copying", copy.LEncodeStageLabel);
        Assert.Equal(1, CommandTokens.Count(CommandTokens.Read(copy.LEncodeStageArguments), "-i"));

        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.True(File.Exists(work.LWorkOutputPath));
        Assert.InRange(FirstPacketStartRead(work.LWorkOutputPath, "v:0"), -0.05, 0.05);
    }

    [Fact]
    public void MissingMiddleDecodeCutoff_DoesNotConvertSmartToWholeEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate("missing-source.mp4", 1, 5, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartMissingDecodeBuild(work);

        LEncodeStage copy = Assert.Single(stages);
        Assert.Equal("Copying", copy.LEncodeStageLabel);
        Assert.Equal(
            "copy",
            CommandTokens.ValueAfter(CommandTokens.Read(copy.LEncodeStageArguments), "-c:v"));
    }

    [Fact]
    public void AudioProbeFailure_DoesNotConvertSmartToWholeEncode()
    {
        string source = Path.Combine(tRoot, "unprobeable-source.mkv");
        File.WriteAllText(source, "not a media file");
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 1.1, 6.4, "Include");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartDecodedStagesBuild(
            work, (1.1, 6.4), (1.1, 2), (2, 6, 5.933), (6, 6.4));

        SmartMiddleAssert(stages);
    }

    private static void SmartMiddleAssert(IReadOnlyList<LEncodeStage> stages)
    {
        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        Assert.Contains(stages, stage =>
            CommandTokens.ValueAfter(CommandTokens.Read(stage.LEncodeStageArguments), "-c:v") == "copy");
        Assert.False(stages.Count == 1 && stages[0].LEncodeStageLabel == "Encoding");
    }

    private string DelayedAudioSourceCreate()
    {
        string path = Path.Combine(tRoot, "delayed-multiple-audio.mkv");
        Run(
            TEncodeCommand.FfmpegRead(),
            "-hide_banner -loglevel error "
            + "-f lavfi -i testsrc2=size=160x90:rate=30:duration=8 "
            + "-itsoffset 3 -f lavfi -i sine=frequency=440:sample_rate=48000:duration=5 "
            + "-itsoffset 3.25 -f lavfi -i sine=frequency=880:sample_rate=48000:duration=4.75 "
            + "-map 0:v:0 -map 1:a:0 -map 2:a:0 -c:v libx264 -preset ultrafast -g 60 "
            + $"-c:a aac -y {Quote(path)}");
        return path;
    }

    private string SynchronizedAudioSourceCreate()
    {
        string path = Path.Combine(tRoot, "synchronized-audio.mkv");
        Run(
            TEncodeCommand.FfmpegRead(),
            "-hide_banner -loglevel error "
            + "-f lavfi -i testsrc2=size=160x90:rate=30:duration=8 "
            + "-f lavfi -i sine=frequency=440:sample_rate=48000:duration=8 "
            + "-map 0:v:0 -map 1:a:0 -c:v libx264 -preset ultrafast -g 60 "
            + $"-c:a aac -y {Quote(path)}");
        return path;
    }

    private string ReorderedSourceCreate(
        string name,
        double duration,
        int keyframeInterval,
        int sampleRate = 48_000,
        int videoTimescale = 0,
        string videoRate = "24000/1001")
    {
        string path = Path.Combine(tRoot, name);
        string timescale = videoTimescale > 0 ? $" -video_track_timescale {videoTimescale}" : string.Empty;
        Run(
            TEncodeCommand.FfmpegRead(),
            "-hide_banner -loglevel error "
            + $"-f lavfi -i testsrc2=size=160x90:rate={videoRate}:duration={duration.ToString(CultureInfo.InvariantCulture)} "
            + $"-f lavfi -i sine=frequency=440:sample_rate={sampleRate}:duration={duration.ToString(CultureInfo.InvariantCulture)} "
            + "-map 0:v:0 -map 1:a:0 -c:v libx264 -preset medium -bf 3 "
            + $"-g {keyframeInterval} -keyint_min {keyframeInterval} -sc_threshold 0 "
            + $"-c:a aac{timescale} -y {Quote(path)}");
        return path;
    }

    private string OffsetMp4SourceCreate()
    {
        string source = ReorderedSourceCreate("offset-base.mp4", 24, 96, 44_100);
        string offset = Path.Combine(tRoot, "offset-source.mp4");
        Run(
            TEncodeCommand.FfmpegRead(),
            $"-hide_banner -loglevel error -itsoffset 3.4 -i {Quote(source)} -map 0 -c copy -y {Quote(offset)}");
        return offset;
    }

    private static LWorkItem SmartWorkCreate(
        string source,
        double origin,
        double end,
        string audioStream,
        bool mp4Output = false)
    {
        string extension = mp4Output ? "mp4" : "mkv";
        string output = Path.Combine(Path.GetDirectoryName(source) ?? string.Empty, $"smart-output.{extension}");
        return TEncodeCommand.SmartIntervalWorkCreate(
            source,
            output,
            origin,
            end,
            audioStream,
            mp4Output ? "mp4" : "matroska",
            extension);
    }

    private static IReadOnlyDictionary<int, double> AudioPacketStartsRead(string path)
    {
        string output = Run(
            TEncodeCommand.FfprobeRead(),
            $"-v error -select_streams a -show_packets -show_entries packet=stream_index,pts_time -of csv=p=0 {Quote(path)}");
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

    private static double FirstPacketStartRead(string path, string stream)
    {
        string output = Run(
            TEncodeCommand.FfprobeRead(),
            $"-v error -select_streams {stream} -show_packets -show_entries packet=pts_time -of csv=p=0 {Quote(path)}");
        string first = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).First().Split(',')[0];
        Assert.True(double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out double pts));
        return pts;
    }

    private static double FormatDurationRead(string path)
    {
        string output = Run(
            TEncodeCommand.FfprobeRead(),
            $"-v error -show_entries format=duration -of default=nw=1:nk=1 {Quote(path)}");
        Assert.True(double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration));
        return duration;
    }

    private static int VideoPacketCountRead(string path)
    {
        string output = Run(
            TEncodeCommand.FfprobeRead(),
            $"-v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of default=nw=1:nk=1 {Quote(path)}");
        Assert.True(int.TryParse(output.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count));
        return count;
    }

    private static string VideoTimeBaseRead(string path) => Run(
        TEncodeCommand.FfprobeRead(),
        $"-v error -select_streams v:0 -show_entries stream=time_base -of default=nw=1:nk=1 {Quote(path)}").Trim();

    private static string VideoFrameRateRead(string path) => Run(
        TEncodeCommand.FfprobeRead(),
        $"-v error -select_streams v:0 -show_entries stream=avg_frame_rate -of default=nw=1:nk=1 {Quote(path)}").Trim();

    private static double StreamDurationRead(string path, string stream)
    {
        string output = Run(
            TEncodeCommand.FfprobeRead(),
            $"-v error -select_streams {stream} -show_entries stream=duration -of default=nw=1:nk=1 {Quote(path)}");
        Assert.True(double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration));
        return duration;
    }

    private double DecodedAudioDurationRead(string path, string outputName, int sampleRate, int channels)
    {
        string decoded = Path.Combine(tRoot, outputName);
        Run(
            TEncodeCommand.FfmpegRead(),
            $"-hide_banner -loglevel error -i {Quote(path)} -map 0:a:0 "
            + $"-c:a pcm_s16le -f s16le -y {Quote(decoded)}");
        return new FileInfo(decoded).Length / (double)(sizeof(short) * sampleRate * channels);
    }

    private static string DecodeErrorsRead(string path)
    {
        var start = new ProcessStartInfo(TEncodeCommand.FfmpegRead())
        {
            Arguments = $"-hide_banner -loglevel error -i {Quote(path)} -map 0:v:0 -f null -",
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

    private static string Run(string program, string arguments)
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

    private static string Quote(string path) => '"' + path + '"';

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(tRoot))
            {
                Directory.Delete(tRoot, true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
