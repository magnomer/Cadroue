using Cadroue.Core;

namespace Cadroue.Application;

public static class LInfoReport
{
    public static IReadOnlyList<LInfoRow> LInfoReportRead(LCargo? lCargo, bool? lAvailability)
    {
        var lRows = new List<LInfoRow>();
        if (lCargo is null)
        {
            string lNotice = LLocalization.LLocalizationTextRead("Source.Empty.Notice");
            LInfoReportAppend(lRows, LInfoRow.LInfoRowMuted, lNotice);
            return lRows;
        }

        if (string.IsNullOrEmpty(lCargo.LCargoSourcePath))
        {
            string lAvailabilityKey = lAvailability == true ? "Info.FFmpeg.Ready" : "Info.FFmpeg.Missing";
            LInfoReportAppend(lRows, LInfoRow.LInfoRowGrey, LLocalization.LLocalizationTextRead(lAvailabilityKey));
            return lRows;
        }

        LInfoReportAppend(
            lRows,
            lCargo.LCargoProcessable ? LInfoRow.LInfoRowGood : LInfoRow.LInfoRowBad,
            LLocalization.LLocalizationTextRead(
                lCargo.LCargoProcessable ? "Info.FFmpeg.Processable" : "Info.FFmpeg.Unprocessable"));
        LInfoReportAppend(
            lRows,
            lCargo.LCargoPreviewAvailable ? LInfoRow.LInfoRowGood : LInfoRow.LInfoRowBad,
            LLocalization.LLocalizationTextRead(
                lCargo.LCargoPreviewAvailable ? "Info.Preview.Available" : "Info.Preview.Unavailable"));

        if (lCargo.LCargoMediaInfo is not { } lMediaInfo)
        {
            if (!string.IsNullOrWhiteSpace(lCargo.LCargoFfmpegError))
            {
                LInfoReportAppend(
                    lRows,
                    LInfoRow.LInfoRowMuted,
                    LInfoText.LInfoTextShorten(
                        LLocalization.LLocalizationFormat("Info.Error.FFmpeg", lCargo.LCargoFfmpegError)));
            }

            if (!string.IsNullOrWhiteSpace(lCargo.LCargoPreviewError))
            {
                LInfoReportAppend(
                    lRows,
                    LInfoRow.LInfoRowMuted,
                    LInfoText.LInfoTextShorten(
                        LLocalization.LLocalizationFormat("Info.Error.Preview", lCargo.LCargoPreviewError)));
            }

            return lRows;
        }

        LInfoReportAppend(lRows, LInfoRow.LInfoRowText, LInfoText.LInfoDurationFormat(lMediaInfo.LMediaInfoDuration));
        if (lMediaInfo.LMediaVideoPresent)
        {
            LInfoReportAppend(
                lRows, LInfoRow.LInfoRowText, $"{lMediaInfo.LMediaVideoWidth}×{lMediaInfo.LMediaVideoHeight}");
            if (lMediaInfo.LMediaVideoRate > 0)
            {
                LInfoReportAppend(lRows, LInfoRow.LInfoRowText, $"{lMediaInfo.LMediaVideoRate:0.##} fps");
            }

            LInfoReportAppend(lRows, LInfoRow.LInfoRowText, LInfoText.LInfoCodecFormat(lMediaInfo.LMediaVideoCodec));
        }
        else
        {
            LInfoReportAppend(lRows, LInfoRow.LInfoRowText, LLocalization.LLocalizationTextRead("Info.Audio.Only"));
        }

        if (lMediaInfo.LMediaAudioPresent)
        {
            string lKHz = (lMediaInfo.LMediaSampleRate / 1000.0).ToString("0.#");
            LInfoReportAppend(lRows, LInfoRow.LInfoRowText, LInfoText.LInfoCodecFormat(lMediaInfo.LMediaAudioCodec));
            LInfoReportAppend(lRows, LInfoRow.LInfoRowText, $"{lKHz} kHz");
            LInfoReportAppend(
                lRows, LInfoRow.LInfoRowText, LInfoText.LInfoChannelFormat(lMediaInfo.LMediaAudioChannels));
        }
        else
        {
            LInfoReportAppend(lRows, LInfoRow.LInfoRowText, LLocalization.LLocalizationTextRead("Info.Audio.None"));
        }

        return lRows;
    }

    private static void LInfoReportAppend(List<LInfoRow> lRows, string lKind, string lCaption)
    {
        if (lRows.Count > 0)
        {
            lRows.Add(new LInfoRow(LInfoRow.LInfoRowSeparator, string.Empty));
        }

        lRows.Add(new LInfoRow(lKind, lCaption));
    }
}
