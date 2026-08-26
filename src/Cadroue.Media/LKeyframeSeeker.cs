using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;

namespace Cadroue.Media;

public static class LKeyframeSeeker
{
    private const double LKeyframeScanTolerance = 1d;
    private const double LKeyframeRangeTolerance = 0.001d;

    public static IReadOnlyList<LKeyframeEntry> LKeyframeRangeScan(
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

        double timelineStartSeconds;
        try
        {
            timelineStartSeconds = LMedia.LMediaFfprobeRead(sourcePath, cancellationToken).LMediaStartTime.TotalSeconds;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            timelineStartSeconds = 0;
        }

        double intervalStartSeconds = timelineStartSeconds + normalizedStart.TotalSeconds;
        double intervalEndSeconds = timelineStartSeconds + scanEndTime.TotalSeconds + LKeyframeScanTolerance;
        string intervalStart = intervalStartSeconds > 0
            ? intervalStartSeconds.ToString("0.#######", CultureInfo.InvariantCulture)
            : string.Empty;
        string readIntervals = FormattableString.Invariant(
            $"{intervalStart}%{intervalEndSeconds.ToString("0.#######", CultureInfo.InvariantCulture)}");

        var psi = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            Arguments = $"-v quiet -select_streams v:0 -show_packets -read_intervals \"{readIntervals}\" "
                + $"-print_format csv -show_entries packet=pts_time,dts_time,flags -i \"{sourcePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var keyframePackets = new List<(double Presentation, double? Decode)>();
        double scanStartSeconds = normalizedStart.TotalSeconds;
        double scanEndSeconds = scanEndTime.TotalSeconds;
        Process? process = null;

        try
        {
            process = Process.Start(psi);
            if (process is null)
                return Array.Empty<LKeyframeEntry>();

            LCustody.LCustodyAttach(process);
            LKeyframePrioritySet(process);
            using var killOnCancel = cancellationToken.Register(
                static p => { try { ((Process)p!).Kill(); } catch { } }, process);

            string? line;
            while ((line = process.StandardOutput.ReadLine()) is not null)
            {
                LKeyframeLineParse(line, keyframePackets);
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
        var keyframeTimes = new SortedDictionary<long, long?>();
        foreach ((double presentationAbsolute, double? decodeAbsolute) in keyframePackets)
        {
            double presentationSeconds = presentationAbsolute - timelineStartSeconds;
            if (presentationSeconds + LKeyframeRangeTolerance < scanStartSeconds) continue;
            if (presentationSeconds - LKeyframeRangeTolerance > scanEndSeconds) continue;

            long ticks = TimeSpan.FromSeconds(presentationSeconds).Ticks;
            if (ticks < 0) continue;

            long? decodeTicks = decodeAbsolute is double decodeSeconds
                ? TimeSpan.FromSeconds(decodeSeconds - timelineStartSeconds).Ticks
                : null;
            keyframeTimes[ticks] = decodeTicks;
        }

        return keyframeTimes
            .Select(pair => new LKeyframeEntry(
                TimeSpan.FromTicks(pair.Key),
                pair.Value is long decodeTicks ? TimeSpan.FromTicks(decodeTicks) : null))
            .ToArray();
    }

    private static void LKeyframePrioritySet(Process process)
    {
        try
        {
            process.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception exception)
            when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }

    private static void LKeyframeLineParse(
        string line,
        List<(double Presentation, double? Decode)> result)
    {
        string[] parts = line.Split(',');
        if (parts.Length < 4) return;
        if (!string.Equals(parts[0], "packet", StringComparison.Ordinal)) return;
        if (!parts[3].Contains('K')) return;

        bool hasPts = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double ptsSeconds);
        bool hasDts = double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double dtsSeconds);
        if (!hasPts && !hasDts) return;
        result.Add((hasPts ? ptsSeconds : dtsSeconds, hasDts ? dtsSeconds : null));
    }

}
