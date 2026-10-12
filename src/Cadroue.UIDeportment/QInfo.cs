using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record QInfoRow(string QInfoRowKind, string QInfoRowText);

public sealed class QInfo
{
    public const string QInfoKindMuted = "Muted";
    public const string QInfoKindText = "Text";
    public const string QInfoKindGrey = "StatusMuted";
    public const string QInfoKindGood = "StatusGood";
    public const string QInfoKindBad = "StatusBad";
    public const string QInfoKindSeparator = "Separator";

    private const int QInfoTextBudget = 80;

    private LCargo? qInfoCargo;

    public event Action? QInfoChanged;

    public QInfo(QViewer lViewer)
    {
        lViewer.QViewerMediaChanged += QInfoMediaHandle;
        LMediaProbe.LMediaAvailabilityReady += QInfoAvailabilityHandle;
    }

    public void QInfoDetach() => LMediaProbe.LMediaAvailabilityReady -= QInfoAvailabilityHandle;

    public IReadOnlyList<QInfoRow> QInfoRowsRead()
    {
        var lRows = new List<QInfoRow>();
        if (qInfoCargo is not { } lCargo)
        {
            QInfoRowAdd(lRows, QInfoKindMuted, LLocalization.LLocalizationTextRead("Source.Empty.Notice"));
            return lRows;
        }

        if (string.IsNullOrEmpty(lCargo.LCargoSourcePath))
        {
            QInfoRowAdd(
                lRows,
                QInfoKindGrey,
                LLocalization.LLocalizationTextRead(
                    LMediaProbe.LMediaAvailabilityCurrent == true ? "Info.FFmpeg.Ready" : "Info.FFmpeg.Missing"));
            return lRows;
        }

        QInfoRowAdd(
            lRows,
            lCargo.LCargoProcessable ? QInfoKindGood : QInfoKindBad,
            LLocalization.LLocalizationTextRead(
                lCargo.LCargoProcessable ? "Info.FFmpeg.Processable" : "Info.FFmpeg.Unprocessable"));
        QInfoRowAdd(
            lRows,
            lCargo.LCargoPreviewAvailable ? QInfoKindGood : QInfoKindBad,
            LLocalization.LLocalizationTextRead(
                lCargo.LCargoPreviewAvailable ? "Info.Preview.Available" : "Info.Preview.Unavailable"));

        if (lCargo.LCargoMediaInfo is not { } lMediaInfo)
        {
            QInfoErrorAdd(lRows, lCargo);
            return lRows;
        }

        QInfoRowAdd(lRows, QInfoKindText, QInfoDurationFormat(lMediaInfo.LMediaInfoDuration));
        if (lMediaInfo.LMediaVideoPresent)
        {
            QInfoRowAdd(lRows, QInfoKindText, $"{lMediaInfo.LMediaVideoWidth}×{lMediaInfo.LMediaVideoHeight}");
            if (lMediaInfo.LMediaVideoRate > 0)
            {
                QInfoRowAdd(lRows, QInfoKindText, $"{lMediaInfo.LMediaVideoRate:0.##} fps");
            }

            QInfoRowAdd(lRows, QInfoKindText, QInfoCodecFormat(lMediaInfo.LMediaVideoCodec));
        }
        else
        {
            QInfoRowAdd(lRows, QInfoKindText, LLocalization.LLocalizationTextRead("Info.Audio.Only"));
        }

        if (lMediaInfo.LMediaAudioPresent)
        {
            string lKHz = (lMediaInfo.LMediaSampleRate / 1000.0).ToString("0.#");
            QInfoRowAdd(lRows, QInfoKindText, QInfoCodecFormat(lMediaInfo.LMediaAudioCodec));
            QInfoRowAdd(lRows, QInfoKindText, $"{lKHz} kHz");
            QInfoRowAdd(lRows, QInfoKindText, QInfoChannelFormat(lMediaInfo.LMediaAudioChannels));
        }
        else
        {
            QInfoRowAdd(lRows, QInfoKindText, LLocalization.LLocalizationTextRead("Info.Audio.None"));
        }

        return lRows;
    }

    private void QInfoMediaHandle(LCargo lCargo)
    {
        qInfoCargo = lCargo;
        if (string.IsNullOrEmpty(lCargo.LCargoSourcePath) && LMediaProbe.LMediaAvailabilityCurrent is null)
        {
            LMediaProbe.LMediaAvailabilityDefer();
        }

        QInfoChanged?.Invoke();
    }

    private void QInfoAvailabilityHandle(bool lReady)
    {
        if (qInfoCargo is { } lCargo && string.IsNullOrEmpty(lCargo.LCargoSourcePath))
        {
            QInfoChanged?.Invoke();
        }
    }

    private static void QInfoErrorAdd(List<QInfoRow> lRows, LCargo lCargo)
    {
        if (!string.IsNullOrWhiteSpace(lCargo.LCargoFfmpegError))
        {
            QInfoRowAdd(
                lRows,
                QInfoKindMuted,
                QInfoTextShorten(LLocalization.LLocalizationFormat("Info.Error.FFmpeg", lCargo.LCargoFfmpegError)));
        }

        if (!string.IsNullOrWhiteSpace(lCargo.LCargoPreviewError))
        {
            QInfoRowAdd(
                lRows,
                QInfoKindMuted,
                QInfoTextShorten(LLocalization.LLocalizationFormat("Info.Error.Preview", lCargo.LCargoPreviewError)));
        }
    }

    private static void QInfoRowAdd(List<QInfoRow> lRows, string lKind, string lText)
    {
        if (lRows.Count > 0)
        {
            lRows.Add(new QInfoRow(QInfoKindSeparator, string.Empty));
        }

        lRows.Add(new QInfoRow(lKind, lText));
    }

    private static string QInfoTextShorten(string lText) =>
        lText.Length <= QInfoTextBudget ? lText : lText[..QInfoTextBudget] + "…";

    private static string QInfoDurationFormat(TimeSpan lDuration) =>
        lDuration.TotalHours >= 1
            ? $"{(int)lDuration.TotalHours:D2}:{lDuration.Minutes:D2}:{lDuration.Seconds:D2}"
            : $"{lDuration.Minutes:D2}:{lDuration.Seconds:D2}";

    private static string QInfoCodecFormat(string lCodecName)
    {
        if (string.IsNullOrWhiteSpace(lCodecName))
        {
            return string.Empty;
        }

        return lCodecName.Trim().ToLowerInvariant() switch
        {
            "h264" or "avc" => "H.264",
            "hevc" or "h265" => "H.265",
            "aac" => "AAC",
            "mp3" => "MP3",
            "flac" => "FLAC",
            "pcm_s16le" => "PCM",
            _ => lCodecName.ToUpperInvariant()
        };
    }

    private static string QInfoChannelFormat(int lChannelCount) => lChannelCount switch
    {
        1 => LLocalization.LLocalizationTextRead("Encoder.Value.Mono"),
        2 => LLocalization.LLocalizationTextRead("Encoder.Value.Stereo"),
        _ => $"{lChannelCount}ch"
    };
}
