using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

using Cadroue.Core;

namespace Cadroue.Media;

public static partial class LMedia
{
    public static readonly IReadOnlyList<string> LMediaVideoExtensions =
        [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".mts", ".m2ts"];

    public static readonly IReadOnlyList<string> LMediaAudioExtensions =
        [".mp3", ".aac", ".flac", ".wav", ".ogg"];

    public static bool LMediaAudioCheck(string lMediaSourcePath) =>
        LMediaAudioExtensions.Contains(Path.GetExtension(lMediaSourcePath), StringComparer.OrdinalIgnoreCase);

    public static bool LMediaCheck(string lMediaSourcePath)
    {
        string lMediaExtension = Path.GetExtension(lMediaSourcePath);
        return LMediaVideoExtensions.Contains(lMediaExtension, StringComparer.OrdinalIgnoreCase)
            || LMediaAudioExtensions.Contains(lMediaExtension, StringComparer.OrdinalIgnoreCase);
    }

    private const int LMediaProbeAttempts = 3;
    private const int LMediaRetryMs = 120;

    public static LMediaInfo LMediaFfprobeRead(string sourcePath, CancellationToken lMediaToken = default)
    {
        for (int lMediaAttempt = 1; ; lMediaAttempt++)
        {
            lMediaToken.ThrowIfCancellationRequested();

            string json;
            string errorText;
            int exitCode;
            LMediaFfprobeRun(sourcePath, lMediaToken, out json, out errorText, out exitCode);

            if (exitCode != 0)
            {
                throw new InvalidOperationException(LMediaFailureFormat(exitCode, errorText));
            }

            bool lMediaLastAttempt = lMediaAttempt >= LMediaProbeAttempts;

            if (string.IsNullOrWhiteSpace(json))
            {
                if (!lMediaLastAttempt)
                {
                    if (lMediaToken.WaitHandle.WaitOne(LMediaRetryMs))
                    {
                        lMediaToken.ThrowIfCancellationRequested();
                    }

                    continue;
                }

                throw new InvalidOperationException(LMediaEmptyFormat(errorText));
            }

            try
            {
                return LMediaFfprobeParse(json);
            }
            catch (JsonException ex)
            {
                if (!lMediaLastAttempt)
                {
                    if (lMediaToken.WaitHandle.WaitOne(LMediaRetryMs))
                    {
                        lMediaToken.ThrowIfCancellationRequested();
                    }

                    continue;
                }

                throw new InvalidOperationException(LMediaInvalidFormat(errorText), ex);
            }
        }
    }

    private static void LMediaFfprobeRun(
        string sourcePath,
        CancellationToken lMediaToken,
        out string json,
        out string errorText,
        out int exitCode)
    {
        var psi = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-print_format");
        psi.ArgumentList.Add("json");
        psi.ArgumentList.Add("-show_streams");
        psi.ArgumentList.Add("-show_format");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(sourcePath);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("ffprobe could not be started.");
        LCustody.LCustodyAttach(process);
        Task<string> jsonTask = process.StandardOutput.ReadToEndAsync(lMediaToken);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(lMediaToken);
        try
        {
            process.WaitForExitAsync(lMediaToken).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
        json = jsonTask.GetAwaiter().GetResult();
        errorText = errorTask.GetAwaiter().GetResult();
        exitCode = process.ExitCode;
    }

    public static double? LMediaLoudnessRead(string sourcePath, CancellationToken lMediaToken = default)
    {
        lMediaToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        var psi = new ProcessStartInfo(LTool.LToolFfmpegRead())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-nostats");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(sourcePath);
        psi.ArgumentList.Add("-map");
        psi.ArgumentList.Add("0:a:0");
        psi.ArgumentList.Add("-af");
        psi.ArgumentList.Add("ebur128");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("null");
        psi.ArgumentList.Add("-");

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            LCustody.LCustodyAttach(process);
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(lMediaToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(lMediaToken);
            try
            {
                process.WaitForExitAsync(lMediaToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }
            _ = outputTask.GetAwaiter().GetResult();
            return LMediaLoudnessParse(errorTask.GetAwaiter().GetResult());
        }
        catch (Exception lMediaException) when (lMediaException is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    private static double? LMediaLoudnessParse(string ffmpegText)
    {
        MatchCollection lMediaMatches = Regex.Matches(ffmpegText, @"I:\s*(-?\d+(?:\.\d+)?)\s*LUFS");
        if (lMediaMatches.Count == 0)
        {
            return null;
        }

        string lMediaValue = lMediaMatches[^1].Groups[1].Value;
        return double.TryParse(lMediaValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double lMediaLoudness)
            ? lMediaLoudness
            : null;
    }

    public static bool LMediaFfprobeExist() => LMediaFfprobeCheck();

    private static bool LMediaFfprobeCheck()
    {
        try
        {
            var psi = new ProcessStartInfo(LTool.LToolFfprobeRead())
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-version");

            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            LCustody.LCustodyAttach(process);
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception lMediaException) when (lMediaException is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private static string LMediaFailureFormat(int exitCode, string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe failed with exit code {exitCode}. {diagnostic}";
    }

    private static string LMediaEmptyFormat(string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe did not return media information. {diagnostic}";
    }

    private static string LMediaInvalidFormat(string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe returned invalid media information JSON. {diagnostic}";
    }

    private static string LMediaDiagnosticNormalize(string errorText)
    {
        string diagnostic = string.IsNullOrWhiteSpace(errorText)
            ? "No ffprobe diagnostic message was returned."
            : errorText.Trim();

        return diagnostic.Length <= 2000 ? diagnostic : diagnostic[..2000];
    }

    internal static LMediaInfo LMediaFfprobeParse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        TimeSpan duration = TimeSpan.Zero;
        TimeSpan start = TimeSpan.Zero;
        if (root.TryGetProperty("format", out JsonElement fmt)
            && fmt.TryGetProperty("duration", out JsonElement durEl)
            && double.TryParse(durEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double durSeconds))
        {
            duration = TimeSpan.FromSeconds(durSeconds);
        }

        if (fmt.ValueKind != JsonValueKind.Undefined
            && fmt.TryGetProperty("start_time", out JsonElement startEl)
            && double.TryParse(startEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double startSeconds))
        {
            start = TimeSpan.FromSeconds(startSeconds);
        }

        int videoWidth = 0, videoHeight = 0;
        double fps = 0d;
        string videoCodec = "unknown";
        string videoPixel = "";
        string videoRange = "";
        TimeSpan videoDuration = TimeSpan.Zero;
        bool audioPresent = false;
        string audioCodec = "";
        int sampleRate = 0, channels = 0, audioBitrate = 0;

        if (root.TryGetProperty("streams", out JsonElement streams))
        {
            foreach (JsonElement stream in streams.EnumerateArray())
            {
                string? codecType = stream.TryGetProperty("codec_type", out JsonElement ct) ? ct.GetString() : null;
                if (codecType == "video" && videoWidth == 0)
                {
                    videoWidth = stream.TryGetProperty("width", out JsonElement w) ? w.GetInt32() : 0;
                    videoHeight = stream.TryGetProperty("height", out JsonElement h) ? h.GetInt32() : 0;
                    videoCodec = stream.TryGetProperty("codec_name", out JsonElement cn) ? cn.GetString() ?? "unknown" : "unknown";
                    videoPixel = stream.TryGetProperty("pix_fmt", out JsonElement pf) ? pf.GetString() ?? "" : "";
                    videoRange = stream.TryGetProperty("color_range", out JsonElement cr) ? cr.GetString() ?? "" : "";
                    fps = LMediaFpsResolve(stream);
                    if (stream.TryGetProperty("duration", out JsonElement videoDurationElement)
                        && double.TryParse(
                            videoDurationElement.GetString(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double videoDurationSeconds))
                    {
                        videoDuration = TimeSpan.FromSeconds(videoDurationSeconds);
                    }
                }
                else if (codecType == "audio" && !audioPresent)
                {
                    audioPresent = true;
                    audioCodec = stream.TryGetProperty("codec_name", out JsonElement acn) ? acn.GetString() ?? "unknown" : "unknown";
                    if (stream.TryGetProperty("sample_rate", out JsonElement sr))
                        int.TryParse(sr.GetString(), out sampleRate);
                    channels = stream.TryGetProperty("channels", out JsonElement ch) ? ch.GetInt32() : 0;
                    if (stream.TryGetProperty("bit_rate", out JsonElement abr))
                        int.TryParse(abr.GetString(), out audioBitrate);
                }
            }
        }

        return new LMediaInfo(duration, videoWidth, videoHeight, fps, videoCodec, audioPresent, audioCodec, sampleRate, channels)
        {
            LMediaAudioBitrate = audioPresent ? audioBitrate : 0,
            LMediaStartTime = start,
            LMediaVideoDuration = videoDuration,
            LMediaVideoPixel = videoPixel,
            LMediaVideoRange = videoRange
        };
    }

    private static double LMediaFpsResolve(JsonElement videoStream)
    {
        string? fpsString = null;
        if (videoStream.TryGetProperty("r_frame_rate", out JsonElement rfr)) fpsString = rfr.GetString();
        if (string.IsNullOrEmpty(fpsString) || fpsString == "0/0")
            if (videoStream.TryGetProperty("avg_frame_rate", out JsonElement afr)) fpsString = afr.GetString();

        if (string.IsNullOrEmpty(fpsString)) return 0d;

        string[] parts = fpsString.Split('/');
        if (parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double num)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double den)
            && den > 0d)
        {
            return num / den;
        }

        return 0d;
    }
}
