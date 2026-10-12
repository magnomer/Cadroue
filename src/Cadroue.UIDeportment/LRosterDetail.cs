using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public enum LRosterDetailKind { LRosterDetailNone, LRosterDetailJob, LRosterDetailCard }

public sealed record LRosterDetailRow(string LRosterDetailLabel, string LRosterDetailValue, string LRosterDetailKey);

public sealed record LRosterLine(string LRosterLineText, string LRosterLineKey);

public sealed record LRosterCompareRow(LRosterLine LRosterCompareSource, LRosterLine LRosterCompareOutput);

public sealed record LRosterBar(
    double LRosterBarRest,
    double LRosterBarMark,
    bool LRosterBarOver,
    string LRosterBarPercent,
    string LRosterBarKey);

public sealed class LRosterDetail
{
    public const string LRosterLineHead = "Head";
    public const string LRosterLinePlain = "Plain";
    public const string LRosterLineChanged = "Changed";
    public const string LRosterValuePlain = "Plain";
    public const string LRosterValueFail = "Fail";
    public const string LRosterPercentOver = "Over";
    public const string LRosterPercentUnder = "Under";
    public const string LRosterPercentEqual = "Equal";

    private const double LRosterBarFloor = 0.0001;

    public required IReadOnlyList<LRosterDetailRow> LRosterDetailFailures { get; init; }

    public required IReadOnlyList<string> LRosterDetailTabs { get; init; }

    public required IReadOnlyList<string> LRosterDetailMeters { get; init; }

    public required IReadOnlyList<LRosterBar> LRosterDetailBars { get; init; }

    public required IReadOnlyList<LRosterCompareRow> LRosterDetailCompares { get; init; }

    public required bool LRosterDetailSound { get; init; }

    public required IReadOnlyList<LRosterCompareRow> LRosterDetailSounds { get; init; }

    public required string LRosterDetailSource { get; init; }

    public required string LRosterDetailOutput { get; init; }

    public required IReadOnlyList<string> LRosterDetailSources { get; init; }

    public required IReadOnlyList<string> LRosterDetailOutputs { get; init; }

    public required IReadOnlyList<LRosterDetailRow> LRosterDetailRecords { get; init; }

    public required IReadOnlyList<LRosterDetailRow> LRosterDetailVideos { get; init; }

    public required IReadOnlyList<LRosterDetailRow> LRosterDetailAudios { get; init; }

    public static LRosterDetailKind LRosterKindRead(LRoster lRoster)
    {
        if (lRoster.QRosterSelection.LRosterCardId != Guid.Empty)
        {
            return LRosterDetailKind.LRosterDetailCard;
        }

        return lRoster.LRosterSelectRead() is null
            ? LRosterDetailKind.LRosterDetailNone
            : LRosterDetailKind.LRosterDetailJob;
    }

    public static LRosterDetail LRosterDetailRead(LRoster lRoster)
    {
        if (lRoster.LRosterSelectRead() is not { } lWorkItem)
        {
            return LRosterDetailCreate(null, string.Empty, null, string.Empty, []);
        }

        CLineageStep lStep = lRoster.LRosterStepRead(lWorkItem.LWorkId);
        return LRosterDetailCreate(
            lWorkItem,
            lRoster.LRosterOwnerFormat(lWorkItem),
            lStep.CLineageStepSource,
            lRoster.LRosterTitleRead(lStep),
            [lStep.CLineageSourceExtension, lStep.CLineageOutputExtension]);
    }

    public static LRosterDetail LRosterDetailCreate(
        LWorkItem? lWorkItem,
        string lOwnerText,
        long? lSourceBytes,
        string lTabName,
        IReadOnlyList<string> lExtensions)
    {
        if (lWorkItem is null)
        {
            return new LRosterDetail
            {
                LRosterDetailFailures = [],
                LRosterDetailTabs = [],
                LRosterDetailMeters = [],
                LRosterDetailBars = [],
                LRosterDetailCompares = [],
                LRosterDetailSound = false,
                LRosterDetailSounds = [],
                LRosterDetailSource = string.Empty,
                LRosterDetailOutput = string.Empty,
                LRosterDetailSources = [],
                LRosterDetailOutputs = [],
                LRosterDetailRecords = [],
                LRosterDetailVideos = [],
                LRosterDetailAudios = []
            };
        }

        long? lOutputBytes = lWorkItem.LWorkOutputBytes;
        bool lFailed = lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateFailed;
        bool lMessage = !string.IsNullOrWhiteSpace(lWorkItem.LWorkMessage);
        IReadOnlyList<LRosterBar> lBars = LRosterBarsCreate(lSourceBytes, lOutputBytes);
        IReadOnlyList<LRosterCompareRow> lSounds = LRosterSoundsCreate(lWorkItem);
        return new LRosterDetail
        {
            LRosterDetailFailures = lFailed && lMessage
                ? [LRosterRowCreate("Roster.Field.FailureReason", lWorkItem.LWorkMessage, LRosterValueFail)]
                : [],
            LRosterDetailTabs = string.IsNullOrWhiteSpace(lTabName) ? [] : [lTabName],
            LRosterDetailMeters = lBars.Count == 0 || LRosterFormatter.LRosterSpentRead(lWorkItem) is null
                ? []
                : [
                    LRosterFormatter.LRosterMeterFormat(
                        LRosterFormatter.LRosterSpentFormat(lWorkItem),
                        LRosterFormatter.LRosterSpeedFormat(lWorkItem))
                ],
            LRosterDetailBars = lBars,
            LRosterDetailCompares = LRosterComparesCreate(lWorkItem, lSourceBytes, lOutputBytes, lExtensions),
            LRosterDetailSound = lSounds.Count > 0,
            LRosterDetailSounds = lSounds,
            LRosterDetailSource = lWorkItem.LWorkSourcePath,
            LRosterDetailOutput = lWorkItem.LWorkOutputPath,
            LRosterDetailSources = lWorkItem.LWorkMergeSources.Count > 1
                ? lWorkItem.LWorkMergeSources
                : [lWorkItem.LWorkSourcePath],
            LRosterDetailOutputs = [lWorkItem.LWorkOutputPath],
            LRosterDetailRecords = LRosterRecordsCreate(lWorkItem, lOwnerText, !lFailed && lMessage),
            LRosterDetailVideos = LRosterVideosCreate(lWorkItem.LWorkOutput.LEncodingVideo),
            LRosterDetailAudios = LRosterAudiosCreate(lWorkItem.LWorkOutput.LEncodingAudio)
        };
    }

    public static IReadOnlyList<LRosterBar> LRosterBarsCreate(long? lSourceBytes, long? lOutputBytes)
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
        string lKey = lPercent > 100 ? LRosterPercentOver : lPercent < 100 ? LRosterPercentUnder : LRosterPercentEqual;
        return [new LRosterBar(
            Math.Max(lRest, LRosterBarFloor), Math.Max(lMark, LRosterBarFloor), lOver, $"{lPercent:0}%", lKey)];
    }

    private static IReadOnlyList<LRosterCompareRow> LRosterComparesCreate(
        LWorkItem lWorkItem, long? lSourceBytes, long? lOutputBytes, IReadOnlyList<string> lExtensions)
    {
        LWorkMedia? lSourceInfo = lWorkItem.LWorkSourceMedia;
        LWorkMedia? lOutputInfo = lWorkItem.LWorkOutputMedia;
        LEncodingVideo lVideo = lWorkItem.LWorkOutput.LEncodingVideo;
        string lUnknown = LRosterFormatter.LRosterUnknownFormat(lWorkItem);
        return
        [
            new LRosterCompareRow(
                new LRosterLine(LLocalization.LLocalizationTextRead("Roster.Section.Source"), LRosterLineHead),
                new LRosterLine(LLocalization.LLocalizationTextRead("Roster.Section.Output"), LRosterLineHead)),
            LRosterCompareCreate(
                lSourceBytes is { } lSourceWhole && lSourceWhole >= 0
                    ? LRosterFormatter.LRosterMebiFormat(lSourceBytes)
                    : lUnknown,
                LRosterFormatter.LRosterMebiFormat(lOutputBytes)),
            LRosterCompareCreate(
                lSourceInfo is null ? lUnknown : LRosterFormatter.LRosterDimensionFormat(lSourceInfo),
                lOutputInfo is { LWorkMediaVideo: true }
                    ? $"{lOutputInfo.LWorkMediaWidth} x {lOutputInfo.LWorkMediaHeight}"
                    : LRosterFormatter.LRosterSizeFormat(lVideo.LEncodingSize)),
            LRosterCompareCreate(
                lSourceInfo is null ? lUnknown : LRosterFormatter.LRosterFpsFormat(lSourceInfo),
                lOutputInfo is { LWorkMediaVideo: true }
                    ? $"{lOutputInfo.LWorkMediaFramerate:0.###} fps"
                    : LRosterFormatter.LRosterFpsFormat(lVideo.LEncodingFps)),
            LRosterCompareCreate(
                lSourceInfo?.LWorkKeyframeInterval is { } lSourceInterval && lSourceInterval > 0
                    ? LRosterFormatter.LRosterRateFormat(lSourceInterval / 1000d)
                    : lUnknown,
                LRosterFormatter.LRosterRateFormat(
                    lOutputInfo?.LWorkKeyframeInterval is { } lOutputInterval && lOutputInterval > 0
                        ? lOutputInterval / 1000d
                        : null)),
            LRosterCompareCreate(
                lSourceInfo is null ? lUnknown : LRosterFormatter.LRosterClockFormat(lSourceInfo.LWorkMediaDuration),
                LRosterFormatter.LRosterClockFormat(lOutputInfo?.LWorkMediaDuration ?? lWorkItem.LWorkDuration)),
            LRosterCompareCreate(
                LRosterFormatter.LRosterContainerFormat(lExtensions[0]),
                LRosterFormatter.LRosterContainerFormat(lExtensions[1]))
        ];
    }

    private static IReadOnlyList<LRosterCompareRow> LRosterSoundsCreate(LWorkItem lWorkItem)
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
            LRosterCompareCreate(
                LRosterFormatter.LRosterCodecFormat(lSourceInfo),
                LRosterFormatter.LRosterCodecFormat(lOutputInfo)),
            LRosterCompareCreate(
                LRosterFormatter.LRosterBitrateFormat(lSourceInfo),
                LRosterFormatter.LRosterBitrateFormat(lOutputInfo)),
            LRosterCompareCreate(
                LRosterFormatter.LRosterSampleFormat(lSourceInfo),
                LRosterFormatter.LRosterSampleFormat(lOutputInfo)),
            LRosterCompareCreate(
                lSourceAudio && lSourceLoudness is null
                    ? LRosterFormatter.LRosterUnknownFormat(lWorkItem)
                    : LRosterFormatter.LRosterLoudnessFormat(lSourceLoudness),
                LRosterFormatter.LRosterLoudnessFormat(lOutputLoudness))
        ];
    }

    public static LRosterCompareRow LRosterCompareCreate(string lSource, string lOutput) =>
        new(
            new LRosterLine(lSource, LRosterLinePlain),
            new LRosterLine(
                lOutput,
                string.Equals(lSource, lOutput, StringComparison.Ordinal) ? LRosterLinePlain : LRosterLineChanged));

    private static IReadOnlyList<LRosterDetailRow> LRosterRecordsCreate(
        LWorkItem lWorkItem, string lOwnerText, bool lMessage)
    {
        var lRows = new List<LRosterDetailRow>
        {
            LRosterRowCreate("Roster.Field.Queued", lWorkItem.LWorkCreateTime.ToString("yyyy-MM-dd HH:mm:ss")),
            LRosterRowCreate("Roster.Field.Started", LRosterFormatter.LRosterStampFormat(lWorkItem.LWorkStartTime)),
            LRosterRowCreate("Roster.Field.Finished", LRosterFormatter.LRosterStampFormat(lWorkItem.LWorkFinishTime)),
            LRosterRowCreate("Roster.Field.Attempts", lWorkItem.LWorkAttemptCount.ToString()),
            LRosterRowCreate("Roster.Field.Owner", lOwnerText),
            LRosterRowCreate(
                "Roster.Field.State",
                LRosterRow.LRosterPhaseFormat(lWorkItem.LWorkStateCurrent, lWorkItem.LWorkPhaseCurrent)),
            LRosterRowCreate("Roster.Field.Priority", LRosterRow.LRosterPriorityFormat(lWorkItem.LWorkPriority))
        };
        if (lMessage)
        {
            lRows.Add(LRosterRowCreate("Roster.Field.Message", lWorkItem.LWorkMessage));
        }

        return lRows;
    }

    private static IReadOnlyList<LRosterDetailRow> LRosterVideosCreate(LEncodingVideo lVideo)
    {
        var lRows = new List<LRosterDetailRow>
        {
            LRosterRowCreate("Roster.Field.Mode", $"{lVideo.LEncodingMode} ({lVideo.LEncodingStream})")
        };
        if (!LRosterFormatter.LRosterReencodeCheck(lVideo.LEncodingMode))
        {
            return lRows;
        }

        lRows.Add(LRosterRowCreate("Roster.Field.Encoder", lVideo.LEncodingEncoder));
        lRows.Add(LRosterRowCreate("Roster.Field.RateControl", lVideo.LEncodingRateControl));
        lRows.Add(LRosterRowCreate("Roster.Field.Quality", lVideo.LEncodingQuality));
        lRows.Add(LRosterRowCreate("Roster.Field.SpeedPreset", lVideo.LEncodingSpeedPreset));
        lRows.Add(LRosterRowCreate("Roster.Field.PixelFormat", lVideo.LEncodingPixel));
        if (lVideo.LEncodingExtras.Count > 0)
        {
            lRows.Add(
                LRosterRowCreate("Roster.Field.Extras", LRosterFormatter.LRosterExtrasFormat(lVideo.LEncodingExtras)));
        }

        return lRows;
    }

    private static IReadOnlyList<LRosterDetailRow> LRosterAudiosCreate(LEncodingAudio lAudio)
    {
        var lRows = new List<LRosterDetailRow>
        {
            LRosterRowCreate("Roster.Field.Mode", $"{lAudio.LEncodingMode} ({lAudio.LEncodingStream})")
        };
        if (!LRosterFormatter.LRosterReencodeCheck(lAudio.LEncodingMode))
        {
            return lRows;
        }

        lRows.Add(LRosterRowCreate("Roster.Field.Encoder", lAudio.LEncodingEncoder));
        lRows.Add(LRosterRowCreate("Roster.Field.RateControl", lAudio.LEncodingRateControl));
        lRows.Add(LRosterRowCreate("Roster.Field.Quality", lAudio.LEncodingQuality));
        if (!string.IsNullOrWhiteSpace(lAudio.LEncodingSpeed))
        {
            lRows.Add(LRosterRowCreate("Roster.Field.SpeedPreset", lAudio.LEncodingSpeed));
        }

        if (lAudio.LEncodingExtras.Count > 0)
        {
            lRows.Add(
                LRosterRowCreate("Roster.Field.Extras", LRosterFormatter.LRosterExtrasFormat(lAudio.LEncodingExtras)));
        }

        lRows.Add(LRosterRowCreate("Roster.Field.SampleRate", lAudio.LEncodingSampleRate));
        lRows.Add(LRosterRowCreate("Roster.Field.Channels", lAudio.LEncodingChannels));
        return lRows;
    }

    private static LRosterDetailRow LRosterRowCreate(
        string lLabelKey, string lValue, string lKey = LRosterValuePlain) =>
        new(LLocalization.LLocalizationTextRead(lLabelKey), lValue, lKey);
}
