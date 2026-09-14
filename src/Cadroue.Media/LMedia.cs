using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

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
    private const int LMediaPollMs = 200;
    private const int LMediaStreamChars = 4096;
    private static readonly TimeSpan lMediaIdleLimit = TimeSpan.FromSeconds(30);

    public static LMediaInfo LMediaFfprobeRead(string sourcePath, CancellationToken lMediaToken = default)
    {
        for (int lMediaAttempt = 1; ; lMediaAttempt++)
        {
            lMediaToken.ThrowIfCancellationRequested();

            LMediaProcessResult lMediaResult = LMediaProcessRun(LMediaFfprobeStart(sourcePath), lMediaToken);
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

    internal static LMediaProcessResult LMediaProcessRun(ProcessStartInfo lMediaStart, CancellationToken lMediaToken)
    {
        using var lMediaProcess = Process.Start(lMediaStart)
            ?? throw new InvalidOperationException(
                $"{Path.GetFileNameWithoutExtension(lMediaStart.FileName)} could not be started.");
        LCustody.LCustodyAttach(lMediaProcess);

        long lMediaPulse = Environment.TickCount64;
        void lMediaPulseSet() => Volatile.Write(ref lMediaPulse, Environment.TickCount64);

        var lMediaOutput = new StringBuilder();
        var lMediaError = new StringBuilder();
        Task lMediaOutputTask = LMediaStreamRead(lMediaProcess.StandardOutput, lMediaOutput, lMediaPulseSet, lMediaToken);
        Task lMediaErrorTask = LMediaStreamRead(lMediaProcess.StandardError, lMediaError, lMediaPulseSet, lMediaToken);

        bool lMediaStalled = false;
        while (!lMediaProcess.WaitForExit(LMediaPollMs))
        {
            if (lMediaToken.IsCancellationRequested)
            {
                lMediaProcess.Kill(entireProcessTree: true);
                lMediaToken.ThrowIfCancellationRequested();
            }

            if (Environment.TickCount64 - Volatile.Read(ref lMediaPulse) > lMediaIdleLimit.TotalMilliseconds)
            {
                lMediaStalled = true;
                lMediaProcess.Kill(entireProcessTree: true);
                lMediaProcess.WaitForExit();
                break;
            }
        }

        lMediaOutputTask.GetAwaiter().GetResult();
        lMediaErrorTask.GetAwaiter().GetResult();
        return new LMediaProcessResult(
            lMediaOutput.ToString(),
            lMediaError.ToString(),
            lMediaStalled ? -1 : lMediaProcess.ExitCode,
            lMediaStalled);
    }

    private static async Task LMediaStreamRead(
        StreamReader lMediaReader,
        StringBuilder lMediaSink,
        Action lMediaPulse,
        CancellationToken lMediaToken)
    {
        char[] lMediaBuffer = new char[LMediaStreamChars];
        int lMediaRead;
        while ((lMediaRead = await lMediaReader.ReadAsync(lMediaBuffer.AsMemory(), lMediaToken).ConfigureAwait(false)) > 0)
        {
            lMediaSink.Append(lMediaBuffer, 0, lMediaRead);
            lMediaPulse();
        }
    }

    internal static ProcessStartInfo LMediaFfprobeStart(string sourcePath)
    {
        var psi = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-print_format");
        psi.ArgumentList.Add("json");
        psi.ArgumentList.Add("-show_entries");
        psi.ArgumentList.Add(
            "stream=codec_type,codec_name,width,height,r_frame_rate,avg_frame_rate,duration,"
            + "pix_fmt,color_range,sample_rate,channels,bit_rate"
            + ":stream_side_data=rotation:stream_tags=rotate"
            + ":format=duration,start_time");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(sourcePath);
        return psi;
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
            LMediaProcessResult lMediaResult = LMediaProcessRun(psi, lMediaToken);
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

            LMediaProcessResult lMediaResult = LMediaProcessRun(psi, CancellationToken.None);
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
