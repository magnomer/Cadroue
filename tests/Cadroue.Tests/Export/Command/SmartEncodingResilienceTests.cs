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
    public void AudioOutsideCut_OmitsInvalidIntermediateAndStillBuildsVideoMux()
    {
        string source = DelayedAudioSourceCreate();
        using var environment = new TEncodeCommand();
        LWorkItem work = SmartWorkCreate(source, 0, 0.5, "Include all audio tracks");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartDecodedStagesBuild(
            work, (0, 0.5), null, (0, 0.5, 0.5), null);

        Assert.False(TEncodeCommand.AudioIntervalRead(source, 0, 0.5));
        Assert.DoesNotContain(stages, stage => stage.LEncodeStageLabel.Contains("audio", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, stages.Count);
        IReadOnlyList<string> mux = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Contains("-an", mux);
        Assert.Equal(1, CommandTokens.Count(mux, "-i"));

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

        SmartMiddleAssert(stages);
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

    private static LWorkItem SmartWorkCreate(string source, double origin, double end, string audioStream)
    {
        string output = Path.Combine(Path.GetDirectoryName(source) ?? string.Empty, "smart-output.mkv");
        return TEncodeCommand.SmartIntervalWorkCreate(source, output, origin, end, audioStream);
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
