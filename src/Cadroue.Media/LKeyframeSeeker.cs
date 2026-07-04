using System.Diagnostics;
using System.Globalization;

namespace Cadroue.Media;

public static class LKeyframeSeeker
{
    private const double LKeyframeSeekerScanEndToleranceSeconds = 1d;
    private const double LKeyframeSeekerRangeToleranceSeconds = 0.001d;

    public static IReadOnlyList<LKeyframeEntry> LKeyframeSeekerScanRange(
        string sourcePath,
        TimeSpan scanStartTime,
        TimeSpan scanEndTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return Array.Empty<LKeyframeEntry>();

        TimeSpan normalizedStart = scanStartTime < TimeSpan.Zero ? TimeSpan.Zero : scanStartTime;
        if (scanEndTime <= normalizedStart)
            return Array.Empty<LKeyframeEntry>();

        cancellationToken.ThrowIfCancellationRequested();

        double startSeconds = normalizedStart.TotalSeconds;
        double scanDuration = (scanEndTime - normalizedStart).TotalSeconds + LKeyframeSeekerScanEndToleranceSeconds;
        string readIntervals = FormattableString.Invariant($"{startSeconds:F3}%+{scanDuration:F3}");

        var psi = new ProcessStartInfo("ffprobe")
        {
            Arguments = $"-v quiet -select_streams v:0 -show_packets -read_intervals \"{readIntervals}\" -print_format csv -show_entries packet=pts_time,dts_time,flags -i \"{sourcePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var keyframeTimes = new SortedSet<long>();
        double scanStartSeconds = normalizedStart.TotalSeconds;
        double scanEndSeconds = scanEndTime.TotalSeconds;
        Process? process = null;

        try
        {
            process = Process.Start(psi);
            if (process is null)
                return Array.Empty<LKeyframeEntry>();

            using var killOnCancel = cancellationToken.Register(
                static p => { try { ((Process)p!).Kill(); } catch { } }, process);

            string? line;
            while ((line = process.StandardOutput.ReadLine()) is not null)
            {
                LKeyframeLineParse(line, scanStartSeconds, scanEndSeconds, keyframeTimes);
            }

            process.WaitForExit();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Array.Empty<LKeyframeEntry>();
        }
        finally
        {
            if (process is not null && !process.HasExited)
                try { process.Kill(); } catch { }
            process?.Dispose();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return keyframeTimes
            .Select(ms => new LKeyframeEntry(TimeSpan.FromMilliseconds(ms)))
            .ToArray();
    }

    private static void LKeyframeLineParse(
        string line,
        double scanStartSeconds,
        double scanEndSeconds,
        SortedSet<long> result)
    {
        // CSV: packet,pts_time,dts_time,flags  e.g. "packet,1234.567000,1234.567000,K_"
        string[] parts = line.Split(',');
        if (parts.Length < 4) return;
        if (!parts[3].Contains('K')) return;

        bool hasPts = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double ptsSeconds);
        bool hasDts = double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double dtsSeconds);
        if (!hasPts && !hasDts) return;
        double timeSeconds = hasPts ? ptsSeconds : dtsSeconds;

        if (timeSeconds + LKeyframeSeekerRangeToleranceSeconds < scanStartSeconds) return;
        if (timeSeconds - LKeyframeSeekerRangeToleranceSeconds > scanEndSeconds) return;

        long ms = (long)Math.Round(timeSeconds * 1000d);
        if (ms >= 0) result.Add(ms);
    }
}
