using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

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

    public static QRosterDetail QRosterDetailRead(QRoster lRoster)
    {
        if (lRoster.QRosterSelectRead() is not { } lWorkItem)
        {
            return QRosterDetailCreate(null, string.Empty, null, string.Empty, []);
        }

        CLineageStep lStep = lRoster.QRosterStepRead(lWorkItem.LWorkId);
        return QRosterDetailCreate(
            lWorkItem,
            lRoster.QRosterOwnerFormat(lWorkItem),
            lStep.CLineageStepSource,
            lRoster.QRosterTitleRead(lStep),
            [lStep.CLineageSourceExtension, lStep.CLineageOutputExtension]);
    }

    public static QRosterDetail QRosterDetailCreate(
        LWorkItem? lWorkItem,
        string lOwnerText,
        long? lSourceBytes,
        string lTabName,
        IReadOnlyList<string> lExtensions)
    {
        if (lWorkItem is null)
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

        long? lOutputBytes = lWorkItem.LWorkOutputBytes;
        bool lFailed = lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateFailed;
        bool lMessage = !string.IsNullOrWhiteSpace(lWorkItem.LWorkMessage);
        IReadOnlyList<QRosterBar> lBars = QRosterBarsCreate(lSourceBytes, lOutputBytes);
        IReadOnlyList<QRosterCompareRow> lSounds = QRosterSoundsCreate(lWorkItem);
        return new QRosterDetail
        {
            QRosterDetailFailures = lFailed && lMessage
                ? [QRosterRowCreate("Roster.Field.FailureReason", lWorkItem.LWorkMessage, QRosterValueFail)]
                : [],
            QRosterDetailTabs = string.IsNullOrWhiteSpace(lTabName) ? [] : [lTabName],
            QRosterDetailMeters = lBars.Count == 0 || QRosterFormatter.QRosterSpentRead(lWorkItem) is null
                ? []
                : [
                    QRosterFormatter.QRosterMeterFormat(
                        QRosterFormatter.QRosterSpentFormat(lWorkItem),
                        QRosterFormatter.QRosterSpeedFormat(lWorkItem))
                ],
            QRosterDetailBars = lBars,
            QRosterDetailCompares = QRosterComparesCreate(lWorkItem, lSourceBytes, lOutputBytes, lExtensions),
            QRosterDetailSound = lSounds.Count > 0,
            QRosterDetailSounds = lSounds,
            QRosterDetailSource = lWorkItem.LWorkSourcePath,
            QRosterDetailOutput = lWorkItem.LWorkOutputPath,
            QRosterDetailSources = lWorkItem.LWorkMergeSources.Count > 1
                ? lWorkItem.LWorkMergeSources
                : [lWorkItem.LWorkSourcePath],
            QRosterDetailOutputs = [lWorkItem.LWorkOutputPath],
            QRosterDetailRecords = QRosterRecordsCreate(lWorkItem, lOwnerText, !lFailed && lMessage),
            QRosterDetailVideos = QRosterVideosCreate(lWorkItem.LWorkOutput.LEncodingVideo),
            QRosterDetailAudios = QRosterAudiosCreate(lWorkItem.LWorkOutput.LEncodingAudio)
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

    private static IReadOnlyList<QRosterCompareRow> QRosterComparesCreate(
        LWorkItem lWorkItem, long? lSourceBytes, long? lOutputBytes, IReadOnlyList<string> lExtensions)
    {
        LWorkMedia? lSourceInfo = lWorkItem.LWorkSourceMedia;
        LWorkMedia? lOutputInfo = lWorkItem.LWorkOutputMedia;
        LEncodingVideo lVideo = lWorkItem.LWorkOutput.LEncodingVideo;
        string lUnknown = QRosterFormatter.QRosterUnknownFormat(lWorkItem);
        return
        [
            new QRosterCompareRow(
                new QRosterLine(LLocalization.LLocalizationTextRead("Roster.Section.Source"), QRosterLineHead),
                new QRosterLine(LLocalization.LLocalizationTextRead("Roster.Section.Output"), QRosterLineHead)),
            QRosterCompareCreate(
                lSourceBytes is { } lSourceWhole && lSourceWhole >= 0
                    ? QRosterFormatter.QRosterMebiFormat(lSourceBytes)
                    : lUnknown,
                QRosterFormatter.QRosterMebiFormat(lOutputBytes)),
            QRosterCompareCreate(
                lSourceInfo is null ? lUnknown : QRosterFormatter.QRosterDimensionFormat(lSourceInfo),
                lOutputInfo is { LWorkMediaVideo: true }
                    ? $"{lOutputInfo.LWorkMediaWidth} x {lOutputInfo.LWorkMediaHeight}"
                    : QRosterFormatter.QRosterSizeFormat(lVideo.LEncodingSize)),
            QRosterCompareCreate(
                lSourceInfo is null ? lUnknown : QRosterFormatter.QRosterFpsFormat(lSourceInfo),
                lOutputInfo is { LWorkMediaVideo: true }
                    ? $"{lOutputInfo.LWorkMediaFramerate:0.###} fps"
                    : QRosterFormatter.QRosterFpsFormat(lVideo.LEncodingFps)),
            QRosterCompareCreate(
                lSourceInfo?.LWorkKeyframeInterval is { } lSourceInterval && lSourceInterval > 0
                    ? QRosterFormatter.QRosterRateFormat(lSourceInterval / 1000d)
                    : lUnknown,
                QRosterFormatter.QRosterRateFormat(
                    lOutputInfo?.LWorkKeyframeInterval is { } lOutputInterval && lOutputInterval > 0
                        ? lOutputInterval / 1000d
                        : null)),
            QRosterCompareCreate(
                lSourceInfo is null ? lUnknown : QRosterFormatter.QRosterClockFormat(lSourceInfo.LWorkMediaDuration),
                QRosterFormatter.QRosterClockFormat(lOutputInfo?.LWorkMediaDuration ?? lWorkItem.LWorkDuration)),
            QRosterCompareCreate(
                QRosterFormatter.QRosterContainerFormat(lExtensions[0]),
                QRosterFormatter.QRosterContainerFormat(lExtensions[1]))
        ];
    }

    private static IReadOnlyList<QRosterCompareRow> QRosterSoundsCreate(LWorkItem lWorkItem)
    {
        LWorkMedia? lSourceInfo = lWorkItem.LWorkSourceMedia;
        LWorkMedia? lOutputInfo = lWorkItem.LWorkOutputMedia;
        bool lSourceAudio = (lSourceInfo?.LWorkMediaSamplerate ?? 0) > 0;
        bool lOutputAudio = (lOutputInfo?.LWorkMediaSamplerate ?? 0) > 0;
        if (!lSourceAudio && !lOutputAudio)
        {
            return [];
        }

        double? lSourceLoudness = lSourceAudio ? lSourceInfo?.LWorkMediaLoudness : null;
        double? lOutputLoudness = lWorkItem.LWorkAudio.LWorkAudioActive
            ? (lOutputAudio ? lOutputInfo?.LWorkMediaLoudness : null)
            : lSourceLoudness;
        return
        [
            QRosterCompareCreate(
                QRosterFormatter.QRosterCodecFormat(lSourceInfo),
                QRosterFormatter.QRosterCodecFormat(lOutputInfo)),
            QRosterCompareCreate(
                QRosterFormatter.QRosterBitrateFormat(lSourceInfo),
                QRosterFormatter.QRosterBitrateFormat(lOutputInfo)),
            QRosterCompareCreate(
                QRosterFormatter.QRosterSampleFormat(lSourceInfo),
                QRosterFormatter.QRosterSampleFormat(lOutputInfo)),
            QRosterCompareCreate(
                lSourceAudio && lSourceLoudness is null
                    ? QRosterFormatter.QRosterUnknownFormat(lWorkItem)
                    : QRosterFormatter.QRosterLoudnessFormat(lSourceLoudness),
                QRosterFormatter.QRosterLoudnessFormat(lOutputLoudness))
        ];
    }

    public static QRosterCompareRow QRosterCompareCreate(string lSource, string lOutput) =>
        new(
            new QRosterLine(lSource, QRosterLinePlain),
            new QRosterLine(
                lOutput,
                string.Equals(lSource, lOutput, StringComparison.Ordinal) ? QRosterLinePlain : QRosterLineChanged));

    private static IReadOnlyList<QRosterDetailRow> QRosterRecordsCreate(
        LWorkItem lWorkItem, string lOwnerText, bool lMessage)
    {
        var lRows = new List<QRosterDetailRow>
        {
            QRosterRowCreate("Roster.Field.Queued", lWorkItem.LWorkCreateTime.ToString("yyyy-MM-dd HH:mm:ss")),
            QRosterRowCreate("Roster.Field.Started", QRosterFormatter.QRosterStampFormat(lWorkItem.LWorkStartTime)),
            QRosterRowCreate("Roster.Field.Finished", QRosterFormatter.QRosterStampFormat(lWorkItem.LWorkFinishTime)),
            QRosterRowCreate("Roster.Field.Attempts", lWorkItem.LWorkAttemptCount.ToString()),
            QRosterRowCreate("Roster.Field.Owner", lOwnerText),
            QRosterRowCreate(
                "Roster.Field.State",
                QRosterRow.QRosterPhaseFormat(lWorkItem.LWorkStateCurrent, lWorkItem.LWorkPhaseCurrent)),
            QRosterRowCreate("Roster.Field.Priority", QRosterRow.QRosterPriorityFormat(lWorkItem.LWorkPriority))
        };
        if (lMessage)
        {
            lRows.Add(QRosterRowCreate("Roster.Field.Message", lWorkItem.LWorkMessage));
        }

        return lRows;
    }

    private static IReadOnlyList<QRosterDetailRow> QRosterVideosCreate(LEncodingVideo lVideo)
    {
        var lRows = new List<QRosterDetailRow>
        {
            QRosterRowCreate("Roster.Field.Mode", $"{lVideo.LEncodingMode} ({lVideo.LEncodingStream})")
        };
        if (!QRosterFormatter.QRosterReencodeCheck(lVideo.LEncodingMode))
        {
            return lRows;
        }

        lRows.Add(QRosterRowCreate("Roster.Field.Encoder", lVideo.LEncodingEncoder));
        lRows.Add(QRosterRowCreate("Roster.Field.RateControl", lVideo.LEncodingRateControl));
        lRows.Add(QRosterRowCreate("Roster.Field.Quality", lVideo.LEncodingQuality));
        lRows.Add(QRosterRowCreate("Roster.Field.SpeedPreset", lVideo.LEncodingSpeedPreset));
        lRows.Add(QRosterRowCreate("Roster.Field.PixelFormat", lVideo.LEncodingPixel));
        if (lVideo.LEncodingExtras.Count > 0)
        {
            lRows.Add(
                QRosterRowCreate("Roster.Field.Extras", QRosterFormatter.QRosterExtrasFormat(lVideo.LEncodingExtras)));
        }

        return lRows;
    }

    private static IReadOnlyList<QRosterDetailRow> QRosterAudiosCreate(LEncodingAudio lAudio)
    {
        var lRows = new List<QRosterDetailRow>
        {
            QRosterRowCreate("Roster.Field.Mode", $"{lAudio.LEncodingMode} ({lAudio.LEncodingStream})")
        };
        if (!QRosterFormatter.QRosterReencodeCheck(lAudio.LEncodingMode))
        {
            return lRows;
        }

        lRows.Add(QRosterRowCreate("Roster.Field.Encoder", lAudio.LEncodingEncoder));
        lRows.Add(QRosterRowCreate("Roster.Field.RateControl", lAudio.LEncodingRateControl));
        lRows.Add(QRosterRowCreate("Roster.Field.Quality", lAudio.LEncodingQuality));
        if (!string.IsNullOrWhiteSpace(lAudio.LEncodingSpeed))
        {
            lRows.Add(QRosterRowCreate("Roster.Field.SpeedPreset", lAudio.LEncodingSpeed));
        }

        if (lAudio.LEncodingExtras.Count > 0)
        {
            lRows.Add(
                QRosterRowCreate("Roster.Field.Extras", QRosterFormatter.QRosterExtrasFormat(lAudio.LEncodingExtras)));
        }

        lRows.Add(QRosterRowCreate("Roster.Field.SampleRate", lAudio.LEncodingSampleRate));
        lRows.Add(QRosterRowCreate("Roster.Field.Channels", lAudio.LEncodingChannels));
        return lRows;
    }

    private static QRosterDetailRow QRosterRowCreate(
        string lLabelKey, string lValue, string lKey = QRosterValuePlain) =>
        new(LLocalization.LLocalizationTextRead(lLabelKey), lValue, lKey);
}
