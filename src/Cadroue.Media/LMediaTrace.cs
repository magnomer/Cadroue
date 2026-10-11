using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaTrace
{
    public static Action<string, Exception?>? LMediaErrorSeam { get; set; }

    public static Action<string>? LMediaOpenSeam { get; set; }

    public static void LMediaOpenRecord(LCargo lCargo, bool lPlayerReady)
    {
        string lSourcePath = lCargo.LCargoSourcePath ?? "(no path)";
        string lFileName = Path.GetFileName(lSourcePath);
        if (lCargo.LCargoMediaInfo is not LMediaInfo lMediaInfo)
        {
            LMediaErrorSeam?.Invoke(
                $"Media rejected '{lFileName}': {lCargo.LCargoFfmpegError ?? "unreadable"} [{lSourcePath}]",
                null);
            return;
        }

        string lStreams = lMediaInfo.LMediaVideoPresent
            ? $"video {lMediaInfo.LMediaVideoWidth}x{lMediaInfo.LMediaVideoHeight} "
                + $"{lMediaInfo.LMediaVideoCodec} {lMediaInfo.LMediaVideoRate:0.###}fps"
            : "no video";
        if (lMediaInfo.LMediaAudioPresent)
        {
            lStreams += $", audio {lMediaInfo.LMediaAudioCodec} "
                + $"{lMediaInfo.LMediaSampleRate}Hz {lMediaInfo.LMediaAudioChannels}ch";
        }

        LMediaOpenSeam?.Invoke(
            $"Media opened '{lFileName}': {lMediaInfo.LMediaInfoDuration:hh\\:mm\\:ss\\.fff}, "
            + $"{lStreams} [{lSourcePath}]");

        if (!lPlayerReady)
        {
            LMediaErrorSeam?.Invoke(
                $"Preview unavailable for '{lFileName}': "
                + $"{lCargo.LCargoPreviewError ?? "the player did not start"}",
                null);
        }
    }

    public static void LMediaMpvRecord(LCargo lCargo)
    {
        string lPath = lCargo.LCargoSourcePath ?? "(no path)";
        string lFileName = Path.GetFileName(lPath);
        if (lCargo.LCargoMediaInfo is { } lInfo)
        {
            LMediaOpenSeam?.Invoke(
                $"Media opened '{lFileName}': {lInfo.LMediaInfoDuration:hh\\:mm\\:ss\\.fff} (mpv preview) [{lPath}]");
        }
        else
        {
            LMediaErrorSeam?.Invoke(
                $"Media rejected '{lFileName}': {lCargo.LCargoFfmpegError ?? "unreadable"} [{lPath}]",
                null);
        }
    }

    public static void LMediaRebuildRecord(string lSourcePath, string lMpvReason, bool lEditEligible)
    {
        string lFollow = lEditEligible
            ? "preview unavailable (mpv is the Edit preview engine, no Flyleaf fallback)"
            : "falling back to the existing engine for this file";
        LMediaErrorSeam?.Invoke(
            $"mpv could not open '{Path.GetFileName(lSourcePath)}': {lMpvReason}; {lFollow} [{lSourcePath}]",
            null);
    }
}
