using System.Diagnostics;
using System.Globalization;

using Xunit;

namespace Cadroue.Tests;

internal static class TBridgeMetric
{
    internal static IReadOnlyDictionary<int, double> TBridgePacketRead(string path)
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

    internal static double TBridgeFirstRead(string path, string stream)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -select_streams {stream} -show_packets -show_entries packet=pts_time -of csv=p=0 {TBridgePathFormat(path)}");
        string first = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).First().Split(',')[0];
        Assert.True(double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out double pts));
        return pts;
    }

    internal static double TBridgeFormatRead(string path)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -show_entries format=duration -of default=nw=1:nk=1 {TBridgePathFormat(path)}");
        Assert.True(double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration));
        return duration;
    }

    internal static int TBridgeCountRead(string path)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of default=nw=1:nk=1 {TBridgePathFormat(path)}");
        Assert.True(int.TryParse(output.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count));
        return count;
    }

    internal static string TBridgeTimebaseRead(string path) => TBridgeRun(
        TEncodeCommand.TToolFfprobeRead(),
        $"-v error -select_streams v:0 -show_entries stream=time_base -of default=nw=1:nk=1 {TBridgePathFormat(path)}").Trim();

    internal static string TBridgeFramerateRead(string path) => TBridgeRun(
        TEncodeCommand.TToolFfprobeRead(),
        $"-v error -select_streams v:0 -show_entries stream=avg_frame_rate -of default=nw=1:nk=1 {TBridgePathFormat(path)}").Trim();

    internal static double TBridgeStreamRead(string path, string stream)
    {
        string output = TBridgeRun(
            TEncodeCommand.TToolFfprobeRead(),
            $"-v error -select_streams {stream} -show_entries stream=duration -of default=nw=1:nk=1 {TBridgePathFormat(path)}");
        Assert.True(double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double duration));
        return duration;
    }

    internal static double TBridgeDecodeRead(string path, string decoded, int sampleRate, int channels)
    {
        TBridgeRun(
            TEncodeCommand.TToolFfmpegRead(),
            $"-hide_banner -loglevel error -i {TBridgePathFormat(path)} -map 0:a:0 "
            + $"-c:a pcm_s16le -f s16le -y {TBridgePathFormat(decoded)}");
        return new FileInfo(decoded).Length / (double)(sizeof(short) * sampleRate * channels);
    }

    internal static string TBridgeErrorRead(string path)
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

    internal static string TBridgeRun(string program, string arguments)
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

    internal static string TBridgePathFormat(string path) => '"' + path + '"';
}
