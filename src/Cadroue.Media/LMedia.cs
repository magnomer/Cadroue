using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Cadroue.Core;

namespace Cadroue.Media;

internal readonly record struct LMediaProcessResult(
    string LMediaProcessOutput,
    string LMediaProcessError,
    int LMediaProcessExit,
    bool LMediaProcessStalled);

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
    private static readonly TimeSpan lMediaIdleLimit = TimeSpan.FromSeconds(30);
    private static readonly SemaphoreSlim lMediaScanSlot = new(1, 1);
    private static int lMediaScanWaiters;

    public static async Task LMediaScanClaim(CancellationToken lMediaToken = default)
    {
        if (Volatile.Read(ref lMediaScanWaiters) > 0)
        {
            await Task.Delay(1, lMediaToken).ConfigureAwait(false);
        }

        Interlocked.Increment(ref lMediaScanWaiters);
        try
        {
            await lMediaScanSlot.WaitAsync(lMediaToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref lMediaScanWaiters);
        }
    }

    public static void LMediaScanRelease() => lMediaScanSlot.Release();

    public static async Task<LMediaInfo> LMediaFfprobeRead(string sourcePath, CancellationToken lMediaToken = default)
    {
        for (int lMediaAttempt = 1; ; lMediaAttempt++)
        {
            lMediaToken.ThrowIfCancellationRequested();

            LMediaProcessResult lMediaResult = await LMediaProcessRun(
                LTool.LToolFfprobeRead(), LMediaFfprobeCreate(sourcePath), lMediaToken).ConfigureAwait(false);
            string errorText = lMediaResult.LMediaProcessError;
            if (lMediaResult.LMediaProcessStalled)
            {
                throw new InvalidOperationException(LMediaStallFormat(errorText));
            }

            if (lMediaResult.LMediaProcessExit != 0)
            {
                throw new InvalidOperationException(LMediaFailureFormat(lMediaResult.LMediaProcessExit, errorText));
            }

            bool lMediaLastAttempt = lMediaAttempt >= LMediaProbeAttempts;
            string json = lMediaResult.LMediaProcessOutput;

            if (string.IsNullOrWhiteSpace(json))
            {
                if (!lMediaLastAttempt)
                {
                    await Task.Delay(LMediaRetryMs, lMediaToken).ConfigureAwait(false);
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
                    await Task.Delay(LMediaRetryMs, lMediaToken).ConfigureAwait(false);
                    continue;
                }

                throw new InvalidOperationException(LMediaInvalidFormat(errorText), ex);
            }
        }
    }

    internal static async Task<LMediaProcessResult> LMediaProcessRun(
        string lMediaProgram,
        IReadOnlyList<string> lMediaArguments,
        CancellationToken lMediaToken,
        bool lMediaBackground = false)
    {
        var lMediaOutput = new StringBuilder();
        var lMediaEmployer = new LEmployer(lMediaProgram)
        {
            LEmployerBackground = lMediaBackground,
            LEmployerIdleLimit = lMediaIdleLimit
        };
        LEmployerResult lMediaResult = await lMediaEmployer
            .LEmployerRun(lMediaArguments, lMediaToken, lMediaLine => lMediaOutput.Append(lMediaLine).Append('\n'))
            .ConfigureAwait(false);
        return new LMediaProcessResult(
            lMediaOutput.ToString(),
            lMediaResult.LEmployerError,
            lMediaResult.LEmployerStalled ? -1 : lMediaResult.LEmployerExit,
            lMediaResult.LEmployerStalled);
    }

    internal static IReadOnlyList<string> LMediaFfprobeCreate(string sourcePath) =>
    [
        "-v", "error",
        "-print_format", "json",
        "-show_entries",
        "stream=codec_type,codec_name,width,height,r_frame_rate,avg_frame_rate,duration,"
            + "pix_fmt,color_range,sample_rate,channels,bit_rate"
            + ":stream_side_data=rotation:stream_tags=rotate"
            + ":format=duration,start_time",
        "-i", sourcePath
    ];

    public static async Task<double?> LMediaLoudnessRead(string sourcePath, CancellationToken lMediaToken = default)
    {
        lMediaToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        string[] lMediaArguments =
        [
            "-hide_banner", "-nostats", "-i", sourcePath, "-map", "0:a:0", "-af", "ebur128", "-f", "null", "-"
        ];

        try
        {
            LMediaProcessResult lMediaResult = await LMediaProcessRun(
                LTool.LToolFfmpegRead(), lMediaArguments, lMediaToken).ConfigureAwait(false);
            return lMediaResult.LMediaProcessStalled ? null : LMediaLoudnessParse(lMediaResult.LMediaProcessError);
        }
        catch (Exception lMediaException) when (
            lMediaException is System.ComponentModel.Win32Exception
                or InvalidOperationException
                or IOException)
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

    public static async Task<bool> LMediaFfprobeExist()
    {
        try
        {
            LMediaProcessResult lMediaResult = await LMediaProcessRun(
                LTool.LToolFfprobeRead(), ["-version"], CancellationToken.None).ConfigureAwait(false);
            return !lMediaResult.LMediaProcessStalled && lMediaResult.LMediaProcessExit == 0;
        }
        catch (Exception lMediaException) when (
            lMediaException is System.ComponentModel.Win32Exception
                or InvalidOperationException)
        {
            return false;
        }
    }

    private static string LMediaFailureFormat(int exitCode, string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe failed with exit code {exitCode}. {diagnostic}";
    }

    private static string LMediaStallFormat(string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe produced no output for {lMediaIdleLimit.TotalSeconds:0} seconds and was stopped. {diagnostic}";
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
}
