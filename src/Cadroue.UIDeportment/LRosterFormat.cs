using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public static class LRosterFormat
{
    public static string LRosterExtrasFormat(IEnumerable<KeyValuePair<string, string>> lExtras) =>
        string.Join("  ", lExtras.Select(lExtra => $"{lExtra.Key} {lExtra.Value}"));

    public static bool LRosterReencodeCheck(string lMode) =>
        !string.Equals(lMode, "Copy", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(lMode, "Exclude", StringComparison.OrdinalIgnoreCase);

    public static string LRosterUnknownFormat(LWorkItem lWorkItem) =>
        LLocalization.LLocalizationTextRead(
            lWorkItem.LWorkSourceMeasured ? "Roster.Value.Unknown" : "Roster.Value.Measuring");

    public static string LRosterRateFormat(double? lRatePerSecond) =>
        lRatePerSecond is { } lRate && lRate > 0
            ? LLocalization.LLocalizationFormat("Roster.Field.KeyframeRate", lRate)
            : LLocalization.LLocalizationTextRead("Roster.Value.Unknown");

    public static string LRosterContainerFormat(string lMediaPath)
    {
        string lExtension = LUsher.LUsherExtensionRead(lMediaPath);
        return lExtension.Length == 0
            ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown")
            : lExtension.ToUpperInvariant();
    }

    public static string LRosterStampFormat(DateTimeOffset? lStamp) =>
        lStamp is { } lValue
            ? lValue.ToString("yyyy-MM-dd HH:mm:ss")
            : LLocalization.LLocalizationTextRead("Roster.Value.NotYet");

    public static string LRosterSpentFormat(LWorkItem lWorkItem) =>
        LRosterSpentRead(lWorkItem) is { } lSpent
            ? LRosterElapsedFormat(lSpent)
            : LLocalization.LLocalizationTextRead("Roster.Value.NotYet");

    public static string LRosterElapsedFormat(TimeSpan lSpent)
    {
        if (lSpent < TimeSpan.Zero)
        {
            lSpent = TimeSpan.Zero;
        }

        var lRounded = TimeSpan.FromSeconds(Math.Max(1, (long)Math.Ceiling(lSpent.TotalSeconds)));
        int lHours = (int)lRounded.TotalHours;
        return lHours > 0
            ? $"{lHours}:{lRounded.Minutes:00}:{lRounded.Seconds:00}"
            : $"{lRounded.Minutes:00}:{lRounded.Seconds:00}";
    }

    public static string LRosterSpeedFormat(LWorkItem lWorkItem) =>
        LRosterSpentRead(lWorkItem) is { } lSpent
            ? LRosterSpeedFormat(lSpent, lWorkItem.LWorkOutputBytes)
            : LLocalization.LLocalizationTextRead("Roster.Value.NotYet");

    public static string LRosterSpeedFormat(TimeSpan lSpent, long? lBytes) =>
        lSpent.TotalSeconds > 0 && lBytes is { } lWhole && lWhole > 0
            ? $"{lWhole / 1048576d / lSpent.TotalSeconds:0.##} MiB/s"
            : LLocalization.LLocalizationTextRead("Roster.Value.NotYet");

    public static string LRosterMebiFormat(long? lSizeBytes)
    {
        if (lSizeBytes is not { } lWholeBytes || lWholeBytes < 0)
        {
            return LLocalization.LLocalizationTextRead("Roster.Value.Unknown");
        }

        double lMebibytes = lWholeBytes / 1048576d;
        return lMebibytes >= 1024d
            ? $"{lMebibytes / 1024d:0.#} GiB"
            : $"{Math.Round(lMebibytes)} MiB";
    }

    public static string LRosterClockFormat(TimeSpan lSpan)
    {
        if (lSpan < TimeSpan.Zero)
        {
            lSpan = TimeSpan.Zero;
        }

        int lHours = (int)lSpan.TotalHours;
        return lHours > 0
            ? $"{lHours}:{lSpan.Minutes:00}:{lSpan.Seconds:00}"
            : $"{lSpan.Minutes}:{lSpan.Seconds:00}";
    }

    public static TimeSpan? LRosterSpentRead(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkStartTime is not { } lStarted)
        {
            return null;
        }

        DateTimeOffset lFinished = lWorkItem.LWorkFinishTime ?? DateTimeOffset.Now;
        TimeSpan lSpent = lFinished - lStarted;
        return lSpent < TimeSpan.Zero ? null : lSpent;
    }

    public static string LRosterCodecFormat(LWorkMedia? lMediaInfo) =>
        lMediaInfo is { LWorkMediaSamplerate: > 0 } && !string.IsNullOrWhiteSpace(lMediaInfo.LWorkMediaCodec)
            ? lMediaInfo.LWorkMediaCodec.ToUpperInvariant()
            : "-";

    public static string LRosterBitrateFormat(LWorkMedia? lMediaInfo) =>
        lMediaInfo is { LWorkMediaSamplerate: > 0, LWorkMediaBitrate: > 0 }
            ? $"{Math.Round(lMediaInfo.LWorkMediaBitrate / 1000d)}k"
            : "-";

    public static string LRosterSampleFormat(LWorkMedia? lMediaInfo) =>
        lMediaInfo is { LWorkMediaSamplerate: > 0 }
            ? $"{lMediaInfo.LWorkMediaSamplerate} Hz"
            : "-";

    public static string LRosterLoudnessFormat(double? lLoudness) =>
        lLoudness is { } lLufs ? $"{lLufs:0.#} LUFS" : "-";

    public static string LRosterDimensionFormat(LWorkMedia lMediaInfo) =>
        lMediaInfo.LWorkMediaVideo
            ? $"{lMediaInfo.LWorkMediaWidth} x {lMediaInfo.LWorkMediaHeight}"
            : LLocalization.LLocalizationTextRead("Roster.AudioOnly");

    public static string LRosterSizeFormat(string lVideoSize) =>
        string.IsNullOrWhiteSpace(lVideoSize)
            ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown")
            : lVideoSize.Replace("x", " x ").Replace("X", " x ").Replace("×", " x ");

    public static string LRosterFpsFormat(LWorkMedia lMediaInfo) =>
        lMediaInfo.LWorkMediaVideo
            ? $"{lMediaInfo.LWorkMediaFramerate:0.###} fps"
            : LLocalization.LLocalizationTextRead("Roster.AudioOnly");

    public static string LRosterFpsFormat(string lVideoFps) =>
        string.IsNullOrWhiteSpace(lVideoFps)
            ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown")
            : $"{lVideoFps} fps";
}
