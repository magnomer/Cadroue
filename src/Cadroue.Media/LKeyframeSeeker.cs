using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;

namespace Cadroue.Media;

public sealed record LKeyframePacket(double LKeyframePresentation, double? LKeyframeDecode);

public sealed record LKeyframeSpanResult(IReadOnlyList<LKeyframeEntry> LKeyframeSpanEntries, bool LKeyframeSpanIntra);

public static class LKeyframeSeeker
{
    private const double LKeyframeScanTolerance = 1d;
    private const double LKeyframeRangeTolerance = 0.001d;
    private const int LKeyframeIntraMinimum = 10;

    public static IReadOnlyList<LKeyframeEntry> LKeyframeRangeScan(
        string sourcePath,
        TimeSpan scanStartTime,
        TimeSpan scanEndTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file does not exist.", sourcePath);
        if (scanEndTime <= (scanStartTime < TimeSpan.Zero ? TimeSpan.Zero : scanStartTime))
            return Array.Empty<LKeyframeEntry>();

        cancellationToken.ThrowIfCancellationRequested();
        double timelineStartSeconds =
            LMedia.LMediaFfprobeRead(sourcePath, cancellationToken).LMediaStartTime.TotalSeconds;
        return LKeyframeSpanScan(sourcePath, timelineStartSeconds, scanStartTime, scanEndTime, cancellationToken)
            .LKeyframeSpanEntries;
    }

    public static LKeyframeSpanResult LKeyframeLaneScan(
        string sourcePath,
        double timelineStartSeconds,
        TimeSpan scanStartTime,
        TimeSpan scanEndTime,
        CancellationToken cancellationToken = default)
    {
        LMedia.LMediaScanClaim(cancellationToken);
        try
        {
            return LKeyframeSpanScan(sourcePath, timelineStartSeconds, scanStartTime, scanEndTime, cancellationToken);
        }
        finally
        {
            LMedia.LMediaScanRelease();
        }
    }

    public static LKeyframeSpanResult LKeyframeSpanScan(
        string sourcePath,
        double timelineStartSeconds,
        TimeSpan scanStartTime,
        TimeSpan scanEndTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file does not exist.", sourcePath);

        TimeSpan normalizedStart = scanStartTime < TimeSpan.Zero ? TimeSpan.Zero : scanStartTime;
        if (scanEndTime <= normalizedStart)
            return new LKeyframeSpanResult(Array.Empty<LKeyframeEntry>(), false);

        cancellationToken.ThrowIfCancellationRequested();

        double intervalStartSeconds = timelineStartSeconds + normalizedStart.TotalSeconds;
        double intervalEndSeconds = timelineStartSeconds + scanEndTime.TotalSeconds + LKeyframeScanTolerance;
        string intervalStart = intervalStartSeconds > 0
            ? intervalStartSeconds.ToString("0.#######", CultureInfo.InvariantCulture)
            : string.Empty;
        string readIntervals = FormattableString.Invariant(
            $"{intervalStart}%{intervalEndSeconds.ToString("0.#######", CultureInfo.InvariantCulture)}");

        var psi = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            Arguments = $"-v error -select_streams v:0 -show_packets -read_intervals \"{readIntervals}\" "
                + $"-print_format csv -show_entries packet=pts_time,dts_time,flags -i \"{sourcePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var keyframePackets = new List<LKeyframePacket>();
        int packetCount = 0;
        {
            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("ffprobe could not be started.");
            try
            {
                LCustody.LCustodyAttach(process);
                LKeyframePrioritySet(process);
                using var killOnCancel = cancellationToken.Register(
                    static p => { try { ((Process)p!).Kill(); } catch { } }, process);

                Task<string> errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
                string? line;
                while ((line = process.StandardOutput.ReadLine()) is not null)
                {
                    packetCount += LKeyframeLineParse(line, keyframePackets);
                }

                process.WaitForExit();
                cancellationToken.ThrowIfCancellationRequested();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        LKeyframeFailureFormat(process.ExitCode, errorTask.GetAwaiter().GetResult()));
                }
            }
            finally
            {
                if (!process.HasExited)
                    try { process.Kill(); } catch { }
            }
        }

        return new LKeyframeSpanResult(
            LKeyframeEntriesResolve(
                keyframePackets, timelineStartSeconds, normalizedStart.TotalSeconds, scanEndTime.TotalSeconds),
            packetCount >= LKeyframeIntraMinimum && packetCount == keyframePackets.Count);
    }

    private static IReadOnlyList<LKeyframeEntry> LKeyframeEntriesResolve(
        List<LKeyframePacket> keyframePackets,
        double timelineStartSeconds,
        double scanStartSeconds,
        double scanEndSeconds)
    {
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

    private static string LKeyframeFailureFormat(int exitCode, string errorText)
    {
        string diagnostic = string.IsNullOrWhiteSpace(errorText)
            ? "No ffprobe diagnostic message was returned."
            : errorText.Trim();
        return $"ffprobe packet scan failed with exit code {exitCode}. "
            + (diagnostic.Length <= 2000 ? diagnostic : diagnostic[..2000]);
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

    private static int LKeyframeLineParse(
        string line,
        List<LKeyframePacket> result)
    {
        string[] parts = line.Split(',');
        if (parts.Length < 4) return 0;
        if (!string.Equals(parts[0], "packet", StringComparison.Ordinal)) return 0;
        if (!parts[3].Contains('K')) return 1;

        bool hasPts = double.TryParse(
            parts[1],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double ptsSeconds);
        bool hasDts = double.TryParse(
            parts[2],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double dtsSeconds);
        if (!hasPts && !hasDts) return 1;
        result.Add(new LKeyframePacket(hasPts ? ptsSeconds : dtsSeconds, hasDts ? dtsSeconds : null));
        return 1;
    }
}
