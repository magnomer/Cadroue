using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public enum QRosterDetailKind { QRosterDetailNone, QRosterDetailJob, QRosterDetailCard }

public sealed record QRosterDetailRow(string QRosterDetailLabel, string QRosterDetailValue, string QRosterDetailKey);

public sealed record QRosterLine(string QRosterLineText, string QRosterLineKey);

public sealed record QRosterCompareRow(QRosterLine QRosterCompareSource, QRosterLine QRosterCompareOutput);

public sealed record QRosterBar(
    double QRosterBarRest,
    double QRosterBarMark,
    bool QRosterBarOver,
    string QRosterBarPercent,
    string QRosterBarKey);

public sealed class QRosterDetail
{
    public const string QRosterLineHead = "Head";
    public const string QRosterLinePlain = "Plain";
    public const string QRosterLineChanged = "Changed";
    public const string QRosterValuePlain = "Plain";
    public const string QRosterValueFail = "Fail";
    public const string QRosterPercentOver = "Over";
    public const string QRosterPercentUnder = "Under";
    public const string QRosterPercentEqual = "Equal";

    private const double QRosterBarFloor = 0.0001;

    // The gate's owner and state tokens, each to its wording key.
    private static readonly IReadOnlyDictionary<string, string> qRosterOwnerKeys = new Dictionary<string, string>
    {
        ["ThisTab"] = "Roster.Owner.ThisTab",
        ["OtherTab"] = "Roster.Owner.OtherTab",
        ["OtherWindow"] = "Roster.Owner.OtherWindow"
    };

    private static readonly IReadOnlyDictionary<string, string> qRosterStateKeys = new Dictionary<string, string>
    {
        ["Done"] = "Roster.State.Done",
        ["Failed"] = "Roster.State.Failed",
        ["Unresolved"] = "Roster.State.Unresolved",
        ["Partial"] = "Roster.State.Partial",
        ["Blocked"] = "Roster.State.Blocked",
        ["Processing"] = "Roster.Phase.Processing",
        ["Started"] = "Roster.Phase.Started",
        ["NotStarted"] = "Roster.Phase.NotStarted"
    };

    public required IReadOnlyList<QRosterDetailRow> QRosterDetailFailures { get; init; }

    public required IReadOnlyList<string> QRosterDetailTabs { get; init; }

    public required IReadOnlyList<string> QRosterDetailMeters { get; init; }

    public required IReadOnlyList<QRosterBar> QRosterDetailBars { get; init; }

    public required IReadOnlyList<QRosterCompareRow> QRosterDetailCompares { get; init; }

    public required bool QRosterDetailSound { get; init; }

    public required IReadOnlyList<QRosterCompareRow> QRosterDetailSounds { get; init; }

    public required string QRosterDetailSource { get; init; }

    public required string QRosterDetailOutput { get; init; }

    public required IReadOnlyList<string> QRosterDetailSources { get; init; }

    public required IReadOnlyList<string> QRosterDetailOutputs { get; init; }

    public required IReadOnlyList<QRosterDetailRow> QRosterDetailRecords { get; init; }

    public required IReadOnlyList<QRosterDetailRow> QRosterDetailVideos { get; init; }

    public required IReadOnlyList<QRosterDetailRow> QRosterDetailAudios { get; init; }

    public static QRosterDetailKind QRosterKindRead(QRoster lRoster)
    {
        if (lRoster.QRosterSelection.QRosterCardId != Guid.Empty)
        {
            return QRosterDetailKind.QRosterDetailCard;
        }

        return lRoster.QRosterSelectRead() is null
            ? QRosterDetailKind.QRosterDetailNone
            : QRosterDetailKind.QRosterDetailJob;
    }

    // Paints the selected item's detail from the roster's detail gate.
    public static QRosterDetail QRosterDetailRead(QRoster lRoster) =>
        QRosterDetailCreate(lRoster.QRosterDetailGate.CRosterDetailRead(
            lRoster.QRosterSelection.QRosterCurrentId, lRoster.QRosterStepRead));

    // The gate's job in the roster's words; no job paints an empty detail.
    public static QRosterDetail QRosterDetailCreate(CRosterJob? lJob)
    {
        if (lJob is null)
        {
            return new QRosterDetail
            {
                QRosterDetailFailures = [],
                QRosterDetailTabs = [],
                QRosterDetailMeters = [],
                QRosterDetailBars = [],
                QRosterDetailCompares = [],
                QRosterDetailSound = false,
                QRosterDetailSounds = [],
                QRosterDetailSource = string.Empty,
                QRosterDetailOutput = string.Empty,
                QRosterDetailSources = [],
                QRosterDetailOutputs = [],
                QRosterDetailRecords = [],
                QRosterDetailVideos = [],
                QRosterDetailAudios = []
            };
        }

        CRosterSide lSource = lJob.CRosterJobSource;
        CRosterSide lOutput = lJob.CRosterJobOutput;
        string lUnknown = QLocalizationCatalog.QLocalizationTextRead(
            lJob.CRosterJobMeasured ? "Roster.Value.Unknown" : "Roster.Value.Measuring");
        return new QRosterDetail
        {
            QRosterDetailFailures = lJob.CRosterJobFailure is { } lFailure
                ? [QRosterRowCreate("Roster.Field.FailureReason", lFailure, QRosterValueFail)]
                : [],
            QRosterDetailTabs = lJob.CRosterJobTab is { } lTab ? [lTab] : [],
            QRosterDetailMeters = lJob.CRosterJobSpent is { } lSpent
                ? [
                    QRosterFormatter.QRosterMeterFormat(
                        QRosterFormatter.QRosterElapsedFormat(lSpent),
                        QRosterFormatter.QRosterSpeedFormat(lSpent, lOutput.CRosterSideBytes))
                ]
                : [],
            QRosterDetailBars = QRosterBarsCreate(lSource.CRosterSideBytes, lOutput.CRosterSideBytes),
            QRosterDetailCompares = QRosterComparesCreate(lJob, lUnknown),
            QRosterDetailSound = lJob.CRosterJobSound,
            QRosterDetailSounds = lJob.CRosterJobSound ? QRosterSoundsCreate(lSource, lOutput, lUnknown) : [],
            QRosterDetailSource = lSource.CRosterSidePath,
            QRosterDetailOutput = lOutput.CRosterSidePath,
            QRosterDetailSources = lJob.CRosterJobSources,
            QRosterDetailOutputs = [lOutput.CRosterSidePath],
            QRosterDetailRecords = QRosterRecordsCreate(lJob),
            QRosterDetailVideos = QRosterVideosCreate(lJob.CRosterJobVideo),
            QRosterDetailAudios = QRosterAudiosCreate(lJob.CRosterJobAudio)
        };
    }

    public static IReadOnlyList<QRosterBar> QRosterBarsCreate(long? lSourceBytes, long? lOutputBytes)
    {
        if (lSourceBytes is not { } lSourceWhole || lSourceWhole <= 0
            || lOutputBytes is not { } lOutputWhole || lOutputWhole < 0)
        {
            return [];
        }

        bool lOver = lOutputWhole > lSourceWhole;
        double lRest = lOver ? lSourceWhole : (double)lSourceWhole - lOutputWhole;
        double lMark = lOver ? (double)lOutputWhole - lSourceWhole : lOutputWhole;
        double lPercent = Math.Round((double)lOutputWhole / lSourceWhole * 100);
        string lKey = lPercent > 100 ? QRosterPercentOver : lPercent < 100 ? QRosterPercentUnder : QRosterPercentEqual;
        return [new QRosterBar(
            Math.Max(lRest, QRosterBarFloor), Math.Max(lMark, QRosterBarFloor), lOver, $"{lPercent:0}%", lKey)];
    }

    private static IReadOnlyList<QRosterCompareRow> QRosterComparesCreate(CRosterJob lJob, string lUnknown)
    {
        CRosterSide lSource = lJob.CRosterJobSource;
        CRosterSide lOutput = lJob.CRosterJobOutput;
        CRosterEncoding lVideo = lJob.CRosterJobVideo;
        return
        [
            new QRosterCompareRow(
                new QRosterLine(QLocalizationCatalog.QLocalizationTextRead("Roster.Section.Source"), QRosterLineHead),
                new QRosterLine(QLocalizationCatalog.QLocalizationTextRead("Roster.Section.Output"), QRosterLineHead)),
            QRosterCompareCreate(
                lSource.CRosterSideBytes is { } lSourceWhole
                    ? QRosterFormatter.QRosterMebiFormat(lSourceWhole)
                    : lUnknown,
                QRosterFormatter.QRosterMebiFormat(lOutput.CRosterSideBytes)),
            QRosterCompareCreate(
                lSource.CRosterSideMedia ? QRosterFormatter.QRosterDimensionFormat(lSource) : lUnknown,
                lOutput.CRosterSideVideo
                    ? QRosterFormatter.QRosterDimensionFormat(lOutput)
                    : QRosterFormatter.QRosterSizeFormat(lVideo.CRosterEncodingSize)),
            QRosterCompareCreate(
                lSource.CRosterSideMedia ? QRosterFormatter.QRosterFpsFormat(lSource) : lUnknown,
                lOutput.CRosterSideVideo
                    ? QRosterFormatter.QRosterFpsFormat(lOutput)
                    : QRosterFormatter.QRosterFpsFormat(lVideo.CRosterEncodingFramerate)),
            QRosterCompareCreate(
                lSource.CRosterSideKeyframe is { } lSourceRate
                    ? QRosterFormatter.QRosterRateFormat(lSourceRate)
                    : lUnknown,
                QRosterFormatter.QRosterRateFormat(lOutput.CRosterSideKeyframe)),
            QRosterCompareCreate(
                lSource.CRosterSideDuration is { } lSourceSpan
                    ? QRosterFormatter.QRosterClockFormat(lSourceSpan)
                    : lUnknown,
                lOutput.CRosterSideDuration is { } lOutputSpan
                    ? QRosterFormatter.QRosterClockFormat(lOutputSpan)
                    : lUnknown),
            QRosterCompareCreate(
                QRosterFormatter.QRosterContainerFormat(lSource.CRosterSideExtension),
                QRosterFormatter.QRosterContainerFormat(lOutput.CRosterSideExtension))
        ];
    }

    // The sound rows, shown when either side has sound. A source with sound but no loudness reads as unknown.
    private static IReadOnlyList<QRosterCompareRow> QRosterSoundsCreate(
        CRosterSide lSource, CRosterSide lOutput, string lUnknown) =>
    [
        QRosterCompareCreate(
            QRosterFormatter.QRosterCodecFormat(lSource),
            QRosterFormatter.QRosterCodecFormat(lOutput)),
        QRosterCompareCreate(
            QRosterFormatter.QRosterBitrateFormat(lSource),
            QRosterFormatter.QRosterBitrateFormat(lOutput)),
        QRosterCompareCreate(
            QRosterFormatter.QRosterSampleFormat(lSource),
            QRosterFormatter.QRosterSampleFormat(lOutput)),
        QRosterCompareCreate(
            lSource is { CRosterSideSamplerate: not null, CRosterSideLoudness: null }
                ? lUnknown
                : QRosterFormatter.QRosterLoudnessFormat(lSource.CRosterSideLoudness),
            QRosterFormatter.QRosterLoudnessFormat(lOutput.CRosterSideLoudness))
    ];

    public static QRosterCompareRow QRosterCompareCreate(string lSource, string lOutput) =>
        new(
            new QRosterLine(lSource, QRosterLinePlain),
            new QRosterLine(
                lOutput,
                string.Equals(lSource, lOutput, StringComparison.Ordinal) ? QRosterLinePlain : QRosterLineChanged));

    private static IReadOnlyList<QRosterDetailRow> QRosterRecordsCreate(CRosterJob lJob)
    {
        var lRows = new List<QRosterDetailRow>
        {
            QRosterRowCreate("Roster.Field.Queued", lJob.CRosterJobCreated.ToString("yyyy-MM-dd HH:mm:ss")),
            QRosterRowCreate("Roster.Field.Started", QRosterFormatter.QRosterStampFormat(lJob.CRosterJobStarted)),
            QRosterRowCreate("Roster.Field.Finished", QRosterFormatter.QRosterStampFormat(lJob.CRosterJobFinished)),
            QRosterRowCreate("Roster.Field.Attempts", lJob.CRosterJobAttempts.ToString()),
            QRosterRowCreate(
                "Roster.Field.Owner",
                lJob.CRosterJobOwner is { } lOwner
                    ? QLocalizationCatalog.QLocalizationTextRead(qRosterOwnerKeys[lOwner])
                    : "-"),
            QRosterRowCreate(
                "Roster.Field.State",
                QLocalizationCatalog.QLocalizationTextRead(qRosterStateKeys[lJob.CRosterJobState])),
            QRosterRowCreate(
                "Roster.Field.Priority",
                QLocalizationCatalog.QLocalizationTextRead(
                    lJob.CRosterJobHigh ? "Roster.Priority.High" : "Roster.Priority.Normal"))
        };
        if (lJob.CRosterJobMessage is { } lMessage)
        {
            lRows.Add(QRosterRowCreate("Roster.Field.Message", lMessage));
        }

        return lRows;
    }

    private static IReadOnlyList<QRosterDetailRow> QRosterVideosCreate(CRosterEncoding lVideo)
    {
        var lRows = new List<QRosterDetailRow>
        {
            QRosterRowCreate("Roster.Field.Mode", $"{lVideo.CRosterEncodingMode} ({lVideo.CRosterEncodingStream})")
        };
        if (!lVideo.CRosterEncodingReencode)
        {
            return lRows;
        }

        lRows.Add(QRosterRowCreate("Roster.Field.Encoder", lVideo.CRosterEncodingEncoder));
        lRows.Add(QRosterRowCreate("Roster.Field.RateControl", lVideo.CRosterEncodingControl));
        lRows.Add(QRosterRowCreate("Roster.Field.Quality", lVideo.CRosterEncodingQuality));
        if (lVideo.CRosterEncodingSpeed is { } lSpeed)
        {
            lRows.Add(QRosterRowCreate("Roster.Field.SpeedPreset", lSpeed));
        }

        lRows.Add(QRosterRowCreate("Roster.Field.PixelFormat", lVideo.CRosterEncodingPixel));
        if (lVideo.CRosterEncodingExtras is { } lExtras)
        {
            lRows.Add(QRosterRowCreate("Roster.Field.Extras", QRosterFormatter.QRosterExtrasFormat(lExtras)));
        }

        return lRows;
    }

    private static IReadOnlyList<QRosterDetailRow> QRosterAudiosCreate(CRosterEncoding lAudio)
    {
        var lRows = new List<QRosterDetailRow>
        {
            QRosterRowCreate("Roster.Field.Mode", $"{lAudio.CRosterEncodingMode} ({lAudio.CRosterEncodingStream})")
        };
        if (!lAudio.CRosterEncodingReencode)
        {
            return lRows;
        }

        lRows.Add(QRosterRowCreate("Roster.Field.Encoder", lAudio.CRosterEncodingEncoder));
        lRows.Add(QRosterRowCreate("Roster.Field.RateControl", lAudio.CRosterEncodingControl));
        lRows.Add(QRosterRowCreate("Roster.Field.Quality", lAudio.CRosterEncodingQuality));
        if (lAudio.CRosterEncodingSpeed is { } lSpeed)
        {
            lRows.Add(QRosterRowCreate("Roster.Field.SpeedPreset", lSpeed));
        }

        if (lAudio.CRosterEncodingExtras is { } lExtras)
        {
            lRows.Add(QRosterRowCreate("Roster.Field.Extras", QRosterFormatter.QRosterExtrasFormat(lExtras)));
        }

        lRows.Add(QRosterRowCreate("Roster.Field.SampleRate", lAudio.CRosterEncodingSamplerate));
        lRows.Add(QRosterRowCreate("Roster.Field.Channels", lAudio.CRosterEncodingChannels));
        return lRows;
    }

    private static QRosterDetailRow QRosterRowCreate(
        string lLabelKey, string lValue, string lKey = QRosterValuePlain) =>
        new(QLocalizationCatalog.QLocalizationTextRead(lLabelKey), lValue, lKey);
}
