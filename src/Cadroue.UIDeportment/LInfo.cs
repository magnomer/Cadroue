using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record LInfoRow(string LInfoRowKind, string LInfoRowText);

public sealed class LInfo
{
    public const string LInfoKindMuted = "Muted";
    public const string LInfoKindText = "Text";
    public const string LInfoKindGrey = "StatusMuted";
    public const string LInfoKindGood = "StatusGood";
    public const string LInfoKindBad = "StatusBad";
    public const string LInfoKindSeparator = "Separator";

    private const int LInfoTextBudget = 80;

    private LCargo? lInfoCargo;

    public event Action? LInfoChange;

    public LInfo(LViewer lViewer)
    {
        lViewer.LViewerMediaChange += LInfoMediaHandle;
        LMediaProbe.LMediaAvailabilityReady += LInfoAvailabilityHandle;
    }

    public void LInfoDetach() => LMediaProbe.LMediaAvailabilityReady -= LInfoAvailabilityHandle;

    public IReadOnlyList<LInfoRow> LInfoRowsRead()
    {
        var lRows = new List<LInfoRow>();
        if (lInfoCargo is not { } lCargo)
        {
            LInfoRowAdd(lRows, LInfoKindMuted, LLocalization.LLocalizationTextRead("Source.Empty.Notice"));
            return lRows;
        }

        if (string.IsNullOrEmpty(lCargo.LCargoSourcePath))
        {
            LInfoRowAdd(
                lRows,
                LInfoKindGrey,
                LLocalization.LLocalizationTextRead(
                    LMediaProbe.LMediaAvailabilityCurrent == true ? "Info.FFmpeg.Ready" : "Info.FFmpeg.Missing"));
            return lRows;
        }

        LInfoRowAdd(
            lRows,
            lCargo.LCargoProcessable ? LInfoKindGood : LInfoKindBad,
            LLocalization.LLocalizationTextRead(
                lCargo.LCargoProcessable ? "Info.FFmpeg.Processable" : "Info.FFmpeg.Unprocessable"));
        LInfoRowAdd(
            lRows,
            lCargo.LCargoPreviewAvailable ? LInfoKindGood : LInfoKindBad,
            LLocalization.LLocalizationTextRead(
                lCargo.LCargoPreviewAvailable ? "Info.Preview.Available" : "Info.Preview.Unavailable"));

        if (lCargo.LCargoMediaInfo is not { } lMediaInfo)
        {
            LInfoErrorAdd(lRows, lCargo);
            return lRows;
        }

        LInfoRowAdd(lRows, LInfoKindText, LInfoDurationFormat(lMediaInfo.LMediaInfoDuration));
        if (lMediaInfo.LMediaVideoPresent)
        {
            LInfoRowAdd(lRows, LInfoKindText, $"{lMediaInfo.LMediaVideoWidth}×{lMediaInfo.LMediaVideoHeight}");
            if (lMediaInfo.LMediaVideoRate > 0)
            {
                LInfoRowAdd(lRows, LInfoKindText, $"{lMediaInfo.LMediaVideoRate:0.##} fps");
            }

            LInfoRowAdd(lRows, LInfoKindText, LInfoCodecFormat(lMediaInfo.LMediaVideoCodec));
        }
        else
        {
            LInfoRowAdd(lRows, LInfoKindText, LLocalization.LLocalizationTextRead("Info.Audio.Only"));
        }

        if (lMediaInfo.LMediaAudioPresent)
        {
            string lKHz = (lMediaInfo.LMediaSampleRate / 1000.0).ToString("0.#");
            LInfoRowAdd(lRows, LInfoKindText, LInfoCodecFormat(lMediaInfo.LMediaAudioCodec));
            LInfoRowAdd(lRows, LInfoKindText, $"{lKHz} kHz");
            LInfoRowAdd(lRows, LInfoKindText, LInfoChannelFormat(lMediaInfo.LMediaAudioChannels));
        }
        else
        {
            LInfoRowAdd(lRows, LInfoKindText, LLocalization.LLocalizationTextRead("Info.Audio.None"));
        }

        return lRows;
    }

    private void LInfoMediaHandle(LCargo lCargo)
    {
        lInfoCargo = lCargo;
        if (string.IsNullOrEmpty(lCargo.LCargoSourcePath) && LMediaProbe.LMediaAvailabilityCurrent is null)
        {
            LMediaProbe.LMediaAvailabilityDefer();
        }

        LInfoChange?.Invoke();
    }

    private void LInfoAvailabilityHandle(bool lReady)
    {
        if (lInfoCargo is { } lCargo && string.IsNullOrEmpty(lCargo.LCargoSourcePath))
        {
            LInfoChange?.Invoke();
        }
    }

    private static void LInfoErrorAdd(List<LInfoRow> lRows, LCargo lCargo)
    {
        if (!string.IsNullOrWhiteSpace(lCargo.LCargoFfmpegError))
        {
            LInfoRowAdd(
                lRows,
                LInfoKindMuted,
                LInfoTextShorten(LLocalization.LLocalizationFormat("Info.Error.FFmpeg", lCargo.LCargoFfmpegError)));
        }

        if (!string.IsNullOrWhiteSpace(lCargo.LCargoPreviewError))
        {
            LInfoRowAdd(
                lRows,
                LInfoKindMuted,
                LInfoTextShorten(LLocalization.LLocalizationFormat("Info.Error.Preview", lCargo.LCargoPreviewError)));
        }
    }

    private static void LInfoRowAdd(List<LInfoRow> lRows, string lKind, string lText)
    {
        if (lRows.Count > 0)
        {
            lRows.Add(new LInfoRow(LInfoKindSeparator, string.Empty));
        }

        lRows.Add(new LInfoRow(lKind, lText));
    }

    private static string LInfoTextShorten(string lText) =>
        lText.Length <= LInfoTextBudget ? lText : lText[..LInfoTextBudget] + "…";

    private static string LInfoDurationFormat(TimeSpan lDuration) =>
        lDuration.TotalHours >= 1
            ? $"{(int)lDuration.TotalHours:D2}:{lDuration.Minutes:D2}:{lDuration.Seconds:D2}"
            : $"{lDuration.Minutes:D2}:{lDuration.Seconds:D2}";

    private static string LInfoCodecFormat(string lCodecName)
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

    private static string LInfoChannelFormat(int lChannelCount) => lChannelCount switch
    {
        1 => LLocalization.LLocalizationTextRead("Encoder.Value.Mono"),
        2 => LLocalization.LLocalizationTextRead("Encoder.Value.Stereo"),
        _ => $"{lChannelCount}ch"
    };
}
