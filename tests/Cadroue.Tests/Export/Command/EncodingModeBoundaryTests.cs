using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class EncodingModeBoundaryTests : IDisposable
{
    private const double CutOrigin = 20;
    private const double CutEnd = 50;

    private readonly string tRoot = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "ModeBoundary", Guid.NewGuid().ToString("N"));

    public EncodingModeBoundaryTests()
    {
        Directory.CreateDirectory(tRoot);
    }

    [Fact]
    public void Encode_OpenGopBoundary_OutputsOnlyRequestedPresentation()
    {
        string output = ModeRun("Encode");

        RequestedPresentationAssert(output);
    }

    [Fact]
    public void SmartBridge_OpenGopBoundary_OutputsOnlyRequestedPresentation()
    {
        string output = ModeRun("Smart");

        RequestedPresentationAssert(output);
    }

    [Fact]
    public void Copy_OpenGopBoundary_HidesPreCutPresentationWithinReorderAllowance()
    {
        string output = ModeRun("Copy");

        (byte red, byte green, byte blue) = FirstPixelRead(output);
        Assert.True(
            green > red + 32 && green > blue + 32,
            $"first visible frame is not the requested green section: rgb({red},{green},{blue})");

        // Pure packet copy cannot manufacture an independent decoder refresh at
        // an open-GOP boundary. Its preroll/reorder allowance is intentionally
        // tested separately from Smart, which must produce an exact clean cut.
        Assert.InRange(DurationRead(output), CutEnd - CutOrigin, CutEnd - CutOrigin + 0.35);
    }

    [Fact]
    public void Copy_KeyframeAlignedBoundary_StartsAtRequestedSection()
    {
        string source = IndependentRefreshSourceCreate();
        string output = Path.Combine(tRoot, "copy-keyframe-aligned.mkv");
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.VideoIntervalWorkCreate(source, output, 20, 40, "Copy");

        LEncodeStage stage = Assert.Single(TEncodeCommand.StagesBuild(work));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);
        Assert.Equal("0", CommandTokens.ValueAfter(tokens, "-copypriorss"));
        Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);

        (byte red, byte green, byte blue) = FirstPixelRead(output);
        Assert.True(
            green > red + 32 && green > blue + 32,
            $"first visible frame is not the requested green section: rgb({red},{green},{blue})");
    }

    [Fact]
    public void Smart_OpenGopSourceWithIndependentRefreshes_KeepsCopiedMiddle()
    {
        string source = IndependentRefreshSourceCreate();
        string output = Path.Combine(tRoot, "smart-independent.mkv");
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.VideoIntervalWorkCreate(source, output, 10, 50, "Smart");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartSourceStagesBuild(work);

        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        Assert.False(stages.Count == 1 && stages[0].LEncodeStageLabel == "Encoding");
        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(DurationRead(output), 39.93, 40.07);
        Assert.True(string.IsNullOrWhiteSpace(DecodeErrorsRead(output)));
    }

    [Fact]
    public void Smart_WholeSourceInterval_CopiesWithoutTailReencode()
    {
        string source = ClosedGopSourceCreate();
        double duration = DurationRead(source);
        string output = Path.Combine(tRoot, "smart-whole.mp4");
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartIntervalWorkCreate(
            source, output, 0, duration, "Include", "mp4", "mp4");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartSourceStagesBuild(work);

        // The whole source is copyable end to end: one stream copy, no tail bridge,
        // no concat, and no re-encode that could reject the source profile/pixel format.
        LEncodeStage copy = Assert.Single(stages);
        Assert.Equal("Copying", copy.LEncodeStageLabel);
        IReadOnlyList<string> tokens = CommandTokens.Read(copy.LEncodeStageArguments);
        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.DoesNotContain("concat", tokens);

        Run(TEncodeCommand.FfmpegRead(), copy.LEncodeStageArguments);
        Assert.InRange(DurationRead(output), duration - 0.15, duration + 0.15);
        Assert.True(string.IsNullOrWhiteSpace(DecodeErrorsRead(output)));
    }

    private string ClosedGopSourceCreate()
    {
        string path = Path.Combine(tRoot, "closed-gop-whole.mp4");
        Run(
            TEncodeCommand.FfmpegRead(),
            "-hide_banner -loglevel error -f lavfi -i testsrc=s=160x90:r=30:d=12 "
            + "-f lavfi -i sine=frequency=440:duration=12 "
            + "-c:v libx264 -preset ultrafast -g 60 -keyint_min 60 -x264-params scenecut=0 "
            + $"-pix_fmt yuv420p -c:a aac -movflags +faststart -y {Quote(path)}");
        return path;
    }

    private string ModeRun(string mode)
    {
        string source = SourceCreate();
        string output = Path.Combine(tRoot, mode.ToLowerInvariant() + ".mkv");
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.VideoIntervalWorkCreate(
            source, output, CutOrigin, CutEnd, mode);
        IReadOnlyList<LEncodeStage> stages = string.Equals(mode, "Smart", StringComparison.Ordinal)
            ? TEncodeCommand.SmartSourceStagesBuild(work)
            : TEncodeCommand.StagesBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            Run(TEncodeCommand.FfmpegRead(), stage.LEncodeStageArguments);
        }

        return output;
    }

    private string SourceCreate()
    {
        string path = Path.Combine(tRoot, "open-gop-color-boundary.mp4");
        Run(
            TEncodeCommand.FfmpegRead(),
            "-hide_banner -loglevel error "
            + "-f lavfi -i color=c=red:s=160x90:r=30:d=20 "
            + "-f lavfi -i color=c=green:s=160x90:r=30:d=40 "
            + "-filter_complex \"[0:v][1:v]concat=n=2:v=1:a=0[v]\" -map \"[v]\" "
            + "-c:v libx264 -preset medium -bf 3 -g 600 -keyint_min 600 "
            + "-x264-params open-gop=1:scenecut=0 "
            + $"-an -y {Quote(path)}");
        return path;
    }

    private string IndependentRefreshSourceCreate()
    {
        string[] colors = ["red", "green", "blue"];
        string[] parts = new string[colors.Length];
        for (int index = 0; index < colors.Length; index++)
        {
            parts[index] = Path.Combine(tRoot, $"independent-{index}.mp4");
            Run(
                TEncodeCommand.FfmpegRead(),
                $"-hide_banner -loglevel error -f lavfi -i color=c={colors[index]}:s=160x90:r=30:d=20 "
                + "-c:v libx264 -preset ultrafast -bf 3 -g 600 -keyint_min 600 "
                + $"-x264-params open-gop=1:scenecut=0 -an -y {Quote(parts[index])}");
        }

        string list = Path.Combine(tRoot, "independent-parts.txt");
        File.WriteAllLines(list, parts.Select(part => $"file '{part.Replace("'", "'\\''", StringComparison.Ordinal)}'"));
        string source = Path.Combine(tRoot, "independent-open-gop.mp4");
        Run(
            TEncodeCommand.FfmpegRead(),
            $"-hide_banner -loglevel error -f concat -safe 0 -i {Quote(list)} -c copy -y {Quote(source)}");
        return source;
    }

    private static void RequestedPresentationAssert(string output)
    {
        (byte red, byte green, byte blue) = FirstPixelRead(output);
        double duration = DurationRead(output);
        string decodeErrors = DecodeErrorsRead(output);
        var failures = new List<string>();
        if (!(green > red + 32 && green > blue + 32))
        {
            failures.Add($"first visible frame is not the requested green section: rgb({red},{green},{blue})");
        }

        if (duration < CutEnd - CutOrigin - 0.07 || duration > CutEnd - CutOrigin + 0.07)
        {
            failures.Add($"duration is {duration:0.###}s instead of {CutEnd - CutOrigin:0.###}s");
        }

        if (!string.IsNullOrWhiteSpace(decodeErrors))
        {
            failures.Add("video decoder errors: " + decodeErrors.Trim());
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static (byte Red, byte Green, byte Blue) FirstPixelRead(string path)
    {
        var start = new ProcessStartInfo(TEncodeCommand.FfmpegRead())
        {
            Arguments = $"-hide_banner -loglevel error -i {Quote(path)} -frames:v 1 -vf scale=1:1 "
                + "-pix_fmt rgb24 -f rawvideo -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using Process process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start FFmpeg");
        var pixel = new byte[3];
        int read = process.StandardOutput.BaseStream.Read(pixel, 0, pixel.Length);
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0 && read == pixel.Length, error);
        return (pixel[0], pixel[1], pixel[2]);
    }

    private static double DurationRead(string path)
    {
        string output = Run(
            TEncodeCommand.FfprobeRead(),
            $"-v error -show_entries format=duration -of default=nw=1:nk=1 {Quote(path)}");
        Assert.True(double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration));
        return duration;
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
