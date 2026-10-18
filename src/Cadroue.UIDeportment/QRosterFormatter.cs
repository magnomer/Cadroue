using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public static class QRosterFormatter
{
    public static string QRosterExtrasFormat(IEnumerable<KeyValuePair<string, string>> lExtras) =>
        string.Join("  ", lExtras.Select(lExtra => $"{lExtra.Key} {lExtra.Value}"));

    public static string QRosterRateFormat(double? lRatePerSecond) =>
        lRatePerSecond is { } lRate && lRate > 0
            ? QLocalizationCatalog.QLocalizationFormat("Roster.Field.KeyframeRate", lRate)
            : QLocalizationCatalog.QLocalizationTextRead("Roster.Value.Unknown");

    public static string QRosterContainerFormat(string lExtension) =>
        lExtension.Length == 0
            ? QLocalizationCatalog.QLocalizationTextRead("Roster.Value.Unknown")
            : lExtension.ToUpperInvariant();

    public static string QRosterStampFormat(DateTimeOffset? lStamp) =>
        lStamp is { } lValue
            ? lValue.ToString("yyyy-MM-dd HH:mm:ss")
            : QLocalizationCatalog.QLocalizationTextRead("Roster.Value.NotYet");

    public static string QRosterElapsedFormat(TimeSpan lSpent)
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

    public static string QRosterSpeedFormat(TimeSpan lSpent, long? lBytes) =>
        lSpent.TotalSeconds > 0 && lBytes is { } lWhole && lWhole > 0
            ? $"{lWhole / 1048576d / lSpent.TotalSeconds:0.##} MiB/s"
            : QLocalizationCatalog.QLocalizationTextRead("Roster.Value.NotYet");

    public static string QRosterMeterFormat(string lSpent, string lSpeed) =>
        $"{lSpent} / {lSpeed}";

    public static string QRosterMebiFormat(long? lSizeBytes)
    {
        if (lSizeBytes is not { } lWholeBytes || lWholeBytes < 0)
        {
            return QLocalizationCatalog.QLocalizationTextRead("Roster.Value.Unknown");
        }

        double lMebibytes = lWholeBytes / 1048576d;
        return lMebibytes >= 1024d
            ? $"{lMebibytes / 1024d:0.#} GiB"
            : $"{Math.Round(lMebibytes)} MiB";
    }

    public static string QRosterClockFormat(TimeSpan lSpan)
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

    public static string QRosterCodecFormat(CRosterSide lSide) =>
        lSide.CRosterSideCodec is { } lCodec ? lCodec.ToUpperInvariant() : "-";

    public static string QRosterBitrateFormat(CRosterSide lSide) =>
        lSide.CRosterSideKilobits is { } lKilobits ? $"{lKilobits}k" : "-";

    public static string QRosterSampleFormat(CRosterSide lSide) =>
        lSide.CRosterSideSamplerate is { } lSamplerate ? $"{lSamplerate} Hz" : "-";

    public static string QRosterLoudnessFormat(double? lLoudness) =>
        lLoudness is { } lLufs ? $"{lLufs:0.#} LUFS" : "-";

    public static string QRosterDimensionFormat(CRosterSide lSide) =>
        lSide.CRosterSideVideo
            ? $"{lSide.CRosterSideWidth} x {lSide.CRosterSideHeight}"
            : QLocalizationCatalog.QLocalizationTextRead("Roster.AudioOnly");

    public static string QRosterSizeFormat(string lVideoSize) =>
        string.IsNullOrWhiteSpace(lVideoSize)
            ? QLocalizationCatalog.QLocalizationTextRead("Roster.Value.Unknown")
            : lVideoSize.Replace("x", " x ").Replace("X", " x ").Replace("×", " x ");

    public static string QRosterFpsFormat(CRosterSide lSide) =>
        lSide.CRosterSideVideo
            ? $"{lSide.CRosterSideFramerate:0.###} fps"
            : QLocalizationCatalog.QLocalizationTextRead("Roster.AudioOnly");

    public static string QRosterFpsFormat(string lVideoFps) =>
        string.IsNullOrWhiteSpace(lVideoFps)
            ? QLocalizationCatalog.QLocalizationTextRead("Roster.Value.Unknown")
            : $"{lVideoFps} fps";
}
