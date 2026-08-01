using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cadroue.Media;

public sealed record LMediaInfo
{
    public LMediaInfo(
        TimeSpan mediaInfoDuration,
        int mediaInfoVideoWidth,
        int mediaInfoVideoHeight,
        double mediaInfoVideoFrameRate,
        string mediaInfoVideoCodecName,
        bool mediaInfoAudioPresent,
        string mediaInfoAudioCodecName,
        int mediaInfoAudioSampleRate,
        int mediaInfoAudioChannels)
    {
        if (mediaInfoDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoDuration), "Media duration must be greater than zero.");
        }

        bool mediaInfoVideoPresent = mediaInfoVideoWidth > 0 || mediaInfoVideoHeight > 0;
        if (mediaInfoVideoPresent && mediaInfoVideoWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoVideoWidth), "Video width must be greater than zero when video is present.");
        }

        if (mediaInfoVideoPresent && mediaInfoVideoHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoVideoHeight), "Video height must be greater than zero when video is present.");
        }

        if (!mediaInfoVideoPresent && !mediaInfoAudioPresent)
        {
            throw new ArgumentException("Media must contain at least one video or audio stream.");
        }

        if (mediaInfoVideoFrameRate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoVideoFrameRate), "Video frame rate cannot be negative.");
        }

        if (mediaInfoAudioPresent && mediaInfoAudioSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoAudioSampleRate), "Audio sample rate must be greater than zero when audio is present.");
        }

        if (mediaInfoAudioPresent && mediaInfoAudioChannels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoAudioChannels), "Audio channels must be greater than zero when audio is present.");
        }

        LMediaInfoDuration = mediaInfoDuration;
        LMediaVideoWidth = mediaInfoVideoWidth;
        LMediaVideoHeight = mediaInfoVideoHeight;
        LMediaVideoRate = mediaInfoVideoFrameRate;
        LMediaVideoPresent = mediaInfoVideoPresent;
        LMediaVideoCodec = mediaInfoVideoPresent
            ? string.IsNullOrWhiteSpace(mediaInfoVideoCodecName) ? "unknown" : mediaInfoVideoCodecName
            : "";
        LMediaAudioPresent = mediaInfoAudioPresent;
        LMediaAudioCodec = mediaInfoAudioPresent
            ? string.IsNullOrWhiteSpace(mediaInfoAudioCodecName) ? "unknown" : mediaInfoAudioCodecName
            : "";
        LMediaSampleRate = mediaInfoAudioPresent ? mediaInfoAudioSampleRate : 0;
        LMediaAudioChannels = mediaInfoAudioPresent ? mediaInfoAudioChannels : 0;
    }

    public TimeSpan LMediaInfoDuration { get; }

    public bool LMediaVideoPresent { get; }

    public bool LMediaAudioOnly => !LMediaVideoPresent && LMediaAudioPresent;

    public int LMediaVideoWidth { get; }

    public int LMediaVideoHeight { get; }

    public double LMediaVideoRate { get; }

    public string LMediaVideoCodec { get; }

    public bool LMediaAudioPresent { get; }

    public string LMediaAudioCodec { get; }

    public int LMediaSampleRate { get; }

    public int LMediaAudioChannels { get; }

    public int LMediaAudioBitrate { get; init; }

    public static LMediaInfo LMediaFfprobeRead(string sourcePath)
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

        string json;
        string errorText;
        int exitCode;
        using (var process = Process.Start(psi) ?? throw new InvalidOperationException("ffprobe could not be started."))
        {
            Task<string> jsonTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            json = jsonTask.GetAwaiter().GetResult();
            errorText = errorTask.GetAwaiter().GetResult();
            exitCode = process.ExitCode;
        }

        if (exitCode != 0)
        {
            throw new InvalidOperationException(LMediaFailureFormat(exitCode, errorText));
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(LMediaEmptyFormat(errorText));
        }

        try
        {
            return LMediaFfprobeParse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(LMediaInvalidFormat(errorText), ex);
        }
    }

    public static double? LMediaLoudnessRead(string sourcePath)
    {
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

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
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

    private static LMediaInfo LMediaFfprobeParse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        TimeSpan duration = TimeSpan.Zero;
        if (root.TryGetProperty("format", out JsonElement fmt)
            && fmt.TryGetProperty("duration", out JsonElement durEl)
            && double.TryParse(durEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double durSeconds))
        {
            duration = TimeSpan.FromSeconds(durSeconds);
        }

        int videoWidth = 0, videoHeight = 0;
        double fps = 0d;
        string videoCodec = "unknown";
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
                    fps = LMediaFpsResolve(stream);
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
            LMediaAudioBitrate = audioPresent ? audioBitrate : 0
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
