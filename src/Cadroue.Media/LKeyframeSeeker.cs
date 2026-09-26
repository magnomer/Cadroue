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

    public static async Task<IReadOnlyList<LKeyframeEntry>> LKeyframeRangeScan(
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
        LMediaInfo mediaInfo = await LMedia.LMediaFfprobeRead(sourcePath, cancellationToken).ConfigureAwait(false);
        LKeyframeSpanResult result = await LKeyframeSpanScan(
            sourcePath, mediaInfo.LMediaStartTime.TotalSeconds, scanStartTime, scanEndTime, cancellationToken)
            .ConfigureAwait(false);
        return result.LKeyframeSpanEntries;
    }

    public static async Task<LKeyframeSpanResult> LKeyframeLaneScan(
        string sourcePath,
        double timelineStartSeconds,
        TimeSpan scanStartTime,
        TimeSpan scanEndTime,
        CancellationToken cancellationToken = default)
    {
        await LMedia.LMediaScanClaim(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LKeyframeSpanScan(
                sourcePath, timelineStartSeconds, scanStartTime, scanEndTime, cancellationToken, true)
                .ConfigureAwait(false);
        }
        finally
        {
            LMedia.LMediaScanRelease();
        }
    }

    public static async Task<LKeyframeSpanResult> LKeyframeSpanScan(
        string sourcePath,
        double timelineStartSeconds,
        TimeSpan scanStartTime,
        TimeSpan scanEndTime,
        CancellationToken cancellationToken = default,
        bool background = false)
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
        string[] arguments =
        [
            "-v", "error",
            "-select_streams", "v:0",
            "-show_packets",
            "-read_intervals", readIntervals,
            "-print_format", "csv",
            "-show_entries", "packet=pts_time,dts_time,flags",
            "-i", sourcePath
        ];

        var keyframePackets = new List<LKeyframePacket>();
        int packetCount = 0;
        var employer = new LEmployer(LTool.LToolFfprobeRead()) { LEmployerBackground = background };
        LEmployerResult result = await employer.LEmployerRun(
            arguments,
            cancellationToken,
            line => packetCount += LKeyframeLineParse(line, keyframePackets)).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (result.LEmployerExit != 0)
        {
            throw new InvalidOperationException(LKeyframeFailureFormat(result.LEmployerExit, result.LEmployerError));
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
