using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TEncodingBoundary : IDisposable
{
    private const double TEncodingCutOrigin = 20;
    private const double TEncodingCutEnd = 50;

    private readonly string tEncodingRoot = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "ModeBoundary", Guid.NewGuid().ToString("N"));

    public TEncodingBoundary()
    {
        Directory.CreateDirectory(tEncodingRoot);
    }

    [Fact]
    public void Encode_OpenGopBoundary_OutputsOnlyRequestedPresentation()
    {
        string output = TEncodingModeRun("Encode");

        TEncodingPresentationCheck(output);
    }

    [Fact]
    public void SmartBridge_OpenGopBoundary_OutputsOnlyRequestedPresentation()
    {
        string output = TEncodingModeRun("Smart");

        TEncodingPresentationCheck(output);
    }

    [Fact]
    public void Copy_OpenGopBoundary_HidesPreCutPresentationWithinReorderAllowance()
    {
        string output = TEncodingModeRun("Copy");

        (byte red, byte green, byte blue) = TEncodingPixelRead(output);
        Assert.True(
            green > red + 32 && green > blue + 32,
            $"first visible frame is not the requested green section: rgb({red},{green},{blue})");

        // Pure packet copy cannot manufacture an independent decoder refresh at
        // an open-GOP boundary. Its preroll/reorder allowance is intentionally
        // tested separately from Smart, which must produce an exact clean cut.
        Assert.InRange(TEncodingDurationRead(output), TEncodingCutEnd - TEncodingCutOrigin, TEncodingCutEnd - TEncodingCutOrigin + 0.35);
    }

    [Fact]
    public void Copy_KeyframeAlignedBoundary_StartsAtRequestedSection()
    {
        string source = TEncodingRefreshCreate();
        string output = Path.Combine(tEncodingRoot, "copy-keyframe-aligned.mkv");
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TVideoIntervalCreate(source, output, 20, 40, "Copy");

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);
        Assert.Equal("0", TEncodeToken.TEncodeOptionRead(tokens, "-copypriorss"));
        TEncodingRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);

        (byte red, byte green, byte blue) = TEncodingPixelRead(output);
        Assert.True(
            green > red + 32 && green > blue + 32,
            $"first visible frame is not the requested green section: rgb({red},{green},{blue})");
    }

    [Fact]
    public void Smart_OpenGopSourceWithIndependentRefreshes_KeepsCopiedMiddle()
    {
        string source = TEncodingRefreshCreate();
        string output = Path.Combine(tEncodingRoot, "smart-independent.mkv");
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TVideoIntervalCreate(source, output, 10, 50, "Smart");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        Assert.False(stages.Count == 1 && stages[0].LEncodeStageLabel == "Encoding");
        foreach (LEncodeStage stage in stages)
        {
            TEncodingRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        Assert.InRange(TEncodingDurationRead(output), 39.93, 40.07);
        Assert.True(string.IsNullOrWhiteSpace(TEncodingErrorRead(output)));
    }

    [Fact]
    public void Smart_WholeSourceInterval_CopiesWithoutTailReencode()
    {
        string source = TEncodingGopCreate();
        double duration = TEncodingDurationRead(source);
        string output = Path.Combine(tEncodingRoot, "smart-whole.mp4");
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeIntervalCreate(
            source, output, 0, duration, "Include", "mp4", "mp4");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeSourceBuild(work);

        // The whole source is copyable end to end: one stream copy, no tail bridge,
        // no concat, and no re-encode that could reject the source profile/pixel format.
        LEncodeStage copy = Assert.Single(stages);
        Assert.Equal("Copying", copy.LEncodeStageLabel);
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(copy.LEncodeStageArguments);
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.DoesNotContain("concat", tokens);

        TEncodingRun(TEncodeCommand.TToolFfmpegRead(), copy.LEncodeStageArguments);
        Assert.InRange(TEncodingDurationRead(output), duration - 0.15, duration + 0.15);
        Assert.True(string.IsNullOrWhiteSpace(TEncodingErrorRead(output)));
    }

    private string TEncodingGopCreate()
    {
        string path = Path.Combine(tEncodingRoot, "closed-gop-whole.mp4");
        TEncodingRun(
            TEncodeCommand.TToolFfmpegRead(),
            "-hide_banner -loglevel error -f lavfi -i testsrc=s=160x90:r=30:d=12 "
            + "-f lavfi -i sine=frequency=440:duration=12 "
            + "-c:v libx264 -preset ultrafast -g 60 -keyint_min 60 -x264-params scenecut=0 "
            + $"-pix_fmt yuv420p -c:a aac -movflags +faststart -y {TEncodingPathFormat(path)}");
        return path;
    }

    private string TEncodingModeRun(string mode)
    {
        string source = TEncodingSourceCreate();
        string output = Path.Combine(tEncodingRoot, mode.ToLowerInvariant() + ".mkv");
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TVideoIntervalCreate(
            source, output, TEncodingCutOrigin, TEncodingCutEnd, mode);
        IReadOnlyList<LEncodeStage> stages = string.Equals(mode, "Smart", StringComparison.Ordinal)
            ? TEncodeCommand.TBridgeSourceBuild(work)
            : TEncodeCommand.TEncodeStagesBuild(work);

        foreach (LEncodeStage stage in stages)
        {
            TEncodingRun(TEncodeCommand.TToolFfmpegRead(), stage.LEncodeStageArguments);
        }

        return output;
    }

    private string TEncodingSourceCreate()
    {
        string path = Path.Combine(tEncodingRoot, "open-gop-color-boundary.mp4");
        TEncodingRun(
            TEncodeCommand.TToolFfmpegRead(),
            "-hide_banner -loglevel error "
            + "-f lavfi -i color=c=red:s=160x90:r=30:d=20 "
            + "-f lavfi -i color=c=green:s=160x90:r=30:d=40 "
            + "-filter_complex \"[0:v][1:v]concat=n=2:v=1:a=0[v]\" -map \"[v]\" "
            + "-c:v libx264 -preset medium -bf 3 -g 600 -keyint_min 600 "
            + "-x264-params open-gop=1:scenecut=0 "
            + $"-an -y {TEncodingPathFormat(path)}");
        return path;
    }

    private string TEncodingRefreshCreate()
    {
        string[] colors = ["red", "green", "blue"];
        string[] parts = new string[colors.Length];
        for (int index = 0; index < colors.Length; index++)
        {
            parts[index] = Path.Combine(tEncodingRoot, $"independent-{index}.mp4");
            TEncodingRun(
                TEncodeCommand.TToolFfmpegRead(),
                $"-hide_banner -loglevel error -f lavfi -i color=c={colors[index]}:s=160x90:r=30:d=20 "
                + "-c:v libx264 -preset ultrafast -bf 3 -g 600 -keyint_min 600 "
                + $"-x264-params open-gop=1:scenecut=0 -an -y {TEncodingPathFormat(parts[index])}");
        }

        string list = Path.Combine(tEncodingRoot, "independent-parts.txt");
        File.WriteAllLines(list, parts.Select(part => $"file '{part.Replace("'", "'\\''", StringComparison.Ordinal)}'"));
        string source = Path.Combine(tEncodingRoot, "independent-open-gop.mp4");
        TEncodingRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -f concat -safe 0 -i {TEncodingPathFormat(list)} -c copy -y {TEncodingPathFormat(source)}");
        return source;
    }

    private static void TEncodingPresentationCheck(string output)
    {
        (byte red, byte green, byte blue) = TEncodingPixelRead(output);
        double duration = TEncodingDurationRead(output);
        string decodeErrors = TEncodingErrorRead(output);
        var failures = new List<string>();
        if (!(green > red + 32 && green > blue + 32))
        {
            failures.Add($"first visible frame is not the requested green section: rgb({red},{green},{blue})");
        }

        if (duration < TEncodingCutEnd - TEncodingCutOrigin - 0.07 || duration > TEncodingCutEnd - TEncodingCutOrigin + 0.07)
        {
            failures.Add($"duration is {duration:0.###}s instead of {TEncodingCutEnd - TEncodingCutOrigin:0.###}s");
        }

        if (!string.IsNullOrWhiteSpace(decodeErrors))
        {
            failures.Add("video decoder errors: " + decodeErrors.Trim());
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static (byte Red, byte Green, byte Blue) TEncodingPixelRead(string path)
    {
        var start = new ProcessStartInfo(TEncodeCommand.TToolFfmpegRead())
        {
            Arguments = $"-hide_banner -loglevel error -i {TEncodingPathFormat(path)} -frames:v 1 -vf scale=1:1 "
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

    private static double TEncodingDurationRead(string path)
    {
        string output = TEncodingRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -show_entries format=duration -of default=nw=1:nk=1 {TEncodingPathFormat(path)}");
        Assert.True(double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration));
        return duration;
    }

    private static string TEncodingErrorRead(string path)
    {
        var start = new ProcessStartInfo(TEncodeCommand.TToolFfmpegRead())
        {
            Arguments = $"-hide_banner -loglevel error -i {TEncodingPathFormat(path)} -map 0:v:0 -f null -",
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

    private static string TEncodingRun(string program, string arguments)
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

    private static string TEncodingPathFormat(string path) => '"' + path + '"';

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(tEncodingRoot))
            {
                Directory.Delete(tEncodingRoot, true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
