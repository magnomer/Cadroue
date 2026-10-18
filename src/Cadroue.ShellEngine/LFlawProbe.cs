using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed record LFlawEvidence(
    string LFlawEvidenceProbe,
    string LFlawEvidenceCopying,
    string LFlawEvidenceTransport,
    string LFlawEvidenceMeta,
    string LFlawEvidenceIgnidx,
    string LFlawEvidenceSeeking,
    string LFlawEvidenceDecode,
    string LFlawEvidencePacket,
    string LFlawEvidenceChapter,
    string LFlawEvidenceSecondary,
    string LFlawEvidenceCoded,
    string LFlawEvidenceCrc)
{
    internal bool LFlawEvidenceOpened =>
        LFlawEvidenceMeta.Contains("[FORMAT]", StringComparison.Ordinal)
        || LFlawEvidenceMeta.Contains("[STREAM]", StringComparison.Ordinal);
}

internal static class LFlawProbe
{
    internal static async Task<LFlawEvidence> LFlawProbeRun(LFlawStage lFlawStage, string lFlawSource)
    {
        (_, string lFlawProbeError) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfprobeSeam(),
            $"-hide_banner -v error -show_error -show_format -i {LEncode.LEncodeFormat(lFlawSource)}");
        (_, string lFlawCopyError) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfmpegSeam(),
            $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -");
        (_, string lFlawTransportError) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfmpegSeam(),
            $"-hide_banner -nostdin -v warning -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -");
        (string lFlawMetaReport, _) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfprobeSeam(),
            "-hide_banner -v error -show_streams -show_format -count_packets " +
            $"-i {LEncode.LEncodeFormat(lFlawSource)}");
        (_, string lFlawIgnidxError) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfmpegSeam(),
            $"-hide_banner -nostdin -v error -fflags +ignidx -i {LEncode.LEncodeFormat(lFlawSource)} " +
            "-map 0 -c copy -f null -");
        (_, string lFlawSeekError) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfmpegSeam(),
            $"-hide_banner -nostdin -v error -sseof -1 -i {LEncode.LEncodeFormat(lFlawSource)} " +
            "-map 0 -c copy -f null -");
        (_, string lFlawDecodeError) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfmpegSeam(),
            $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} -an -map 0:v? -f null -");
        (string lFlawPacketReport, _) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfprobeSeam(),
            "-hide_banner -v error -show_packets -show_entries packet=stream_index,pts,dts,duration " +
            $"-i {LEncode.LEncodeFormat(lFlawSource)}");
        (string lFlawChapterReport, _) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfprobeSeam(),
            $"-hide_banner -v error -show_chapters -i {LEncode.LEncodeFormat(lFlawSource)}");
        (_, string lFlawSecondaryError) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfmpegSeam(),
            $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} " +
            "-map 0:s? -map 0:d? -c copy -f null -");
        (_, string lFlawCodedError) = await lFlawStage.LFlawStageRun(
            LToolEngine.LToolFfmpegSeam(),
            $"-hide_banner -nostdin -v error -err_detect +explode -i {LEncode.LEncodeFormat(lFlawSource)} " +
            "-an -map 0:v? -f null -");
        string lFlawCrcError = string.Empty;
        if (LFlawFfvone.LFlawFfvoneCheck(lFlawMetaReport))
        {
            (_, lFlawCrcError) = await lFlawStage.LFlawStageRun(
                LToolEngine.LToolFfmpegSeam(),
                $"-hide_banner -nostdin -v error -err_detect +crccheck -i {LEncode.LEncodeFormat(lFlawSource)} " +
                "-an -map 0:v? -f null -");
        }

        return new LFlawEvidence(
            lFlawProbeError,
            lFlawCopyError,
            lFlawTransportError,
            lFlawMetaReport,
            lFlawIgnidxError,
            lFlawSeekError,
            lFlawDecodeError,
            lFlawPacketReport,
            lFlawChapterReport,
            lFlawSecondaryError,
            lFlawCodedError,
            lFlawCrcError);
    }
}
