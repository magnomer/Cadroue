using System.Globalization;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LEncodePlan
{
    private const string LEncodeIntermediate = "pcm_s32le";

    public static IReadOnlyList<LEncodeStage> LEncodeStagesBuild(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindFix)
        {
            IReadOnlyList<LDossier> lFixRepairable =
                LFixPlan.LFixRepairResolve(lWorkItem.LWorkDossiers, lWorkItem.LWorkFixPlan);
            return LEncodeFix.LEncodeFixBuild(lWorkItem, lFixRepairable, true);
        }

        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindAudio)
        {
            return LEncodeStepsBuild(lWorkItem);
        }

        return LEncodeWholeBuild(lWorkItem, lWorkItem.LWorkOutput);
    }

    private static bool LEncodeCopyCheck(LWorkItem lWorkItem, LEncoding lOutput)
    {
        bool lVideoCopy = string.Equals(
                lOutput.LEncodingVideo.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideo.LEncodeVideoCheck(lWorkItem, lOutput);
        bool lAudioCopy = string.Equals(
                lOutput.LEncodingAudio.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LEncodingAudio.LEncodingMode, "Exclude", StringComparison.OrdinalIgnoreCase);
        return lVideoCopy && lAudioCopy;
    }

    internal static string LEncodeArgumentBuild(
        LWorkItem lWorkItem, string? lOutputArguments = null, int lPass = 0)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        var lArguments = new StringBuilder();

        LEncode.LEncodeHeaderAppend(lArguments);
        LEncodeInputAppend(lArguments, lWorkItem, lOutput);

        LEncodeVideo.LEncodeVideoAppend(lArguments, lWorkItem, lOutput, lPass);
        if (LEncodePassAppend(lArguments, lPass))
        {
            return lArguments.ToString();
        }

        LEncodeAudio.LEncodeAudioAppend(lArguments, lOutput);

        if (!string.IsNullOrWhiteSpace(lOutputArguments))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lOutputArguments}");
        }

        LEncode.LEncodeMuxerAppend(lArguments, lWorkItem);
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncode.LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        return lArguments.ToString();
    }

    private static void LEncodeInputAppend(StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput)
    {
        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge)
        {
            lArguments.Append(
                CultureInfo.InvariantCulture,
                $" -f concat -safe 0 -i {LEncode.LEncodeFormat(LEncodeJoin.LEncodeMergeSave(lWorkItem))}");
            return;
        }

        bool lVideoCopy = string.Equals(
                lOutput.LEncodingVideo.LEncodingMode,
                "Copy",
                StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideo.LEncodeVideoCheck(lWorkItem, lOutput);
        lArguments.Append(
            CultureInfo.InvariantCulture, $" -ss {LEncode.LEncodeTimeFormat(lWorkItem.LWorkOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncode.LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        if (lVideoCopy && lWorkItem.LWorkOrigin > TimeSpan.Zero)
        {
            lArguments.Append(" -copypriorss 0");
        }
        if (!lVideoCopy && lWorkItem.LWorkOrigin > TimeSpan.Zero)
        {
            lArguments.Append(" -ss 0");
        }
        if (lWorkItem.LWorkEnd > lWorkItem.LWorkOrigin)
        {
            lArguments.Append(
                CultureInfo.InvariantCulture, $" -t {LEncode.LEncodeTimeFormat(lWorkItem.LWorkDuration)}");
        }
    }

    private static IReadOnlyList<LEncodeStage> LEncodeStepsBuild(LWorkItem lWorkItem)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        string lAudioFolder = LDepotEngine.LDepotAudioSeam();
        string lRawWav = Path.Combine(lAudioFolder, $"{lWorkItem.LWorkId:N}.raw.wav");
        string lProcessedWav = Path.Combine(lAudioFolder, $"{lWorkItem.LWorkId:N}.proc.wav");

        int lSourceRate = lWorkItem.LWorkSourceMedia?.LWorkMediaSamplerate ?? 0;
        var lStages = new List<LEncodeStage>();

        if (LEncodeAudio.LEncodeExcludeCheck(lOutput))
        {
            return LEncodeWholeBuild(lWorkItem, lOutput);
        }

        int lTwoPassIndex = LEncodeChain.LEncodePassRead(lWorkItem.LWorkAudio);
        bool lProcessed = lWorkItem.LWorkAudio.LWorkAudioActive
            && LEncodeChain.LEncodeChainBuild(
                lWorkItem.LWorkAudio,
                lTwoPassIndex >= 0 ? LEncodeChainMode.LEncodeChainCorrection : LEncodeChainMode.LEncodeChainPlain,
                lTwoPassIndex,
                lSourceRate) is not null;

        if (!lProcessed)
        {
            return LEncodeWholeBuild(lWorkItem, lOutput);
        }

        var lExtract = new StringBuilder();
        LEncode.LEncodeHeaderAppend(lExtract);
        lExtract.Append(CultureInfo.InvariantCulture, $" -i {LEncode.LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lExtract.Append(CultureInfo.InvariantCulture, $" -map 0:a:0 -vn -c:a {LEncodeIntermediate}");
        lExtract.Append(CultureInfo.InvariantCulture, $" {LEncode.LEncodeFormat(lRawWav)}");
        lStages.Add(new LEncodeStage(
            lExtract.ToString(), LWorkStage.LWorkStageExtract, "Extracting audio", lRawWav, true));

        if (lTwoPassIndex >= 0)
        {
            string? lAnalyzeChain = LEncodeChain.LEncodeChainBuild(
                lWorkItem.LWorkAudio, LEncodeChainMode.LEncodeChainAnalyze, lTwoPassIndex, lSourceRate);
            var lAnalyze = new StringBuilder();
            LEncode.LEncodeHeaderAppend(lAnalyze);
            lAnalyze.Append(CultureInfo.InvariantCulture, $" -i {LEncode.LEncodeFormat(lRawWav)}");
            lAnalyze.Append(CultureInfo.InvariantCulture, $" -af {LEncode.LEncodeFormat(lAnalyzeChain!)}");
            lAnalyze.Append(" -f null -");
            lStages.Add(new LEncodeStage(
                lAnalyze.ToString(), LWorkStage.LWorkStageAnalyze, "Analyzing audio", string.Empty, false, true));
        }

        string lChain = LEncodeChain.LEncodeChainBuild(
            lWorkItem.LWorkAudio,
            lTwoPassIndex >= 0 ? LEncodeChainMode.LEncodeChainCorrection : LEncodeChainMode.LEncodeChainPlain,
            lTwoPassIndex,
            lSourceRate)!;
        var lProcess = new StringBuilder();
        LEncode.LEncodeHeaderAppend(lProcess);
        lProcess.Append(CultureInfo.InvariantCulture, $" -i {LEncode.LEncodeFormat(lRawWav)}");
        lProcess.Append(CultureInfo.InvariantCulture, $" -af {LEncode.LEncodeFormat(lChain)}");
        lProcess.Append(CultureInfo.InvariantCulture, $" -c:a {LEncodeIntermediate}");
        if (lSourceRate > 0)
        {
            lProcess.Append(CultureInfo.InvariantCulture, $" -ar {lSourceRate}");
        }

        lProcess.Append(CultureInfo.InvariantCulture, $" {LEncode.LEncodeFormat(lProcessedWav)}");
        lStages.Add(new LEncodeStage(
            lProcess.ToString(), LWorkStage.LWorkStageProcess, "Processing audio", lProcessedWav, true));
        string lAudioInputWav = lProcessedWav;

        var lMux = new StringBuilder();
        LEncode.LEncodeHeaderAppend(lMux);
        lMux.Append(CultureInfo.InvariantCulture, $" -i {LEncode.LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lMux.Append(CultureInfo.InvariantCulture, $" -i {LEncode.LEncodeFormat(lAudioInputWav)}");
        string lMuxInputs = lMux.ToString();

        if (LEncodeVideo.LEncodePassRead(lWorkItem, lOutput) > 1)
        {
            lStages.Add(LEncodePassBuild(
                lWorkItem, LEncodeMuxBuild(lWorkItem, lOutput, lMuxInputs, 1)));
        }

        lStages.Add(new LEncodeStage(
            LEncodeMuxBuild(lWorkItem, lOutput, lMuxInputs, LEncodePassRead(lWorkItem, lOutput)),
            LWorkStage.LWorkStageMux, "Encoding output", lWorkItem.LWorkOutputPath, false));

        return lStages;
    }

    private static string LEncodeMuxBuild(LWorkItem lWorkItem, LEncoding lOutput, string lMuxInputs, int lPass)
    {
        var lMux = new StringBuilder(lMuxInputs);
        bool lAllTracks = LEncodeAudio.LEncodeAllCheck(lOutput);
        lMux.Append(" -map 0:v:0?");
        lMux.Append(" -map 1:a:0");
        if (lAllTracks)
        {
            lMux.Append(" -map 0:a? -map -0:a:0?");
        }

        LEncodeVideo.LEncodeVideoAppend(lMux, lWorkItem, lOutput, lPass);
        if (LEncodePassAppend(lMux, lPass))
        {
            return lMux.ToString();
        }

        LEncodeAudio.LEncodeMuxAppend(lMux, lOutput, lAllTracks);
        LEncode.LEncodeMuxerAppend(lMux, lWorkItem);
        lMux.Append(CultureInfo.InvariantCulture, $" {LEncode.LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        return lMux.ToString();
    }

    internal static int LEncodePassRead(LWorkItem lWorkItem, LEncoding lOutput) =>
        LEncodeVideo.LEncodePassRead(lWorkItem, lOutput) > 1 ? 2 : 0;

    private static bool LEncodePassAppend(StringBuilder lArguments, int lPass)
    {
        if (lPass != 1)
        {
            return false;
        }

        lArguments.Append(" -an -f null -");
        return true;
    }

    internal static LEncodeStage LEncodePassBuild(LWorkItem lWorkItem, string lPassArguments) =>
        new(
            lPassArguments,
            LWorkStage.LWorkStageAnalyze,
            "Analyzing video",
            Path.GetDirectoryName(LEncodeVideo.LEncodePassFormat(lWorkItem)) ?? string.Empty,
            true);

    private static IReadOnlyList<LEncodeStage> LEncodeWholeBuild(LWorkItem lWorkItem, LEncoding lOutput)
    {
        bool lWholeCopy = LEncodeCopyCheck(lWorkItem, lOutput);
        int lPass = LEncodePassRead(lWorkItem, lOutput);
        var lStages = new List<LEncodeStage>();
        if (lPass > 0)
        {
            lStages.Add(LEncodePassBuild(lWorkItem, LEncodeArgumentBuild(lWorkItem, null, 1)));
        }

        lStages.Add(new LEncodeStage(
            LEncodeArgumentBuild(lWorkItem, null, lPass),
            lWholeCopy ? LWorkStage.LWorkStagePassthrough : LWorkStage.LWorkStageEncode,
            lWholeCopy ? "Copying" : "Encoding",
            lWorkItem.LWorkOutputPath,
            false));
        return lStages;
    }
}
