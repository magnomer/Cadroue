using System.Globalization;
using System.IO;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public sealed record LEncodeStage(
    string LEncodeStageArguments,
    LWorkStage LEncodeStageKind,
    string LEncodeStageLabel,
    string LEncodeStagePath,
    bool LEncodeStageTemporary,
    bool LEncodeStageMeasure = false);

internal enum LEncodeChainMode
{
    LEncodeChainPlain,
    LEncodeChainAnalyze,
    LEncodeChainCorrection
}

public static partial class LEncode
{
    public const string LEncodeMeasureToken = "@@MEASURED@@";

    public const double LEncodeStatsPeriod = 0.5;

    public static IReadOnlyList<LEncodeStage> LEncodeStagesBuild(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindFix)
        {
            LRemedyPlan lFixPlan = LRemedy.LRemedyPlanCreate(lWorkItem.LWorkDossiers);
            var lFixStages = new List<LEncodeStage>
            {
                new(lWorkItem.LWorkSourcePath, LWorkStage.LWorkStageDuplicate, "Copying", lWorkItem.LWorkOutputPath, false)
            };

            foreach (LRemedyAction lFixAction in lFixPlan.LRemedyActions)
            {
                lFixStages.Add(new LEncodeStage(
                    LEncodeRemedyBuild(
                        lFixAction.LRemedyCategory,
                        lWorkItem.LWorkOutputPath,
                        lFixAction.LRemedyDossier.LDossierRepairArgument),
                    LWorkStage.LWorkStageRepair, "Repairing", lWorkItem.LWorkOutputPath, false));
            }

            lFixStages.Add(new LEncodeStage(
                lWorkItem.LWorkOutputPath, LWorkStage.LWorkStageVerify, "Validating", lWorkItem.LWorkOutputPath, false));
            return lFixStages;
        }

        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindAudio)
        {
            return LEncodeStepsBuild(lWorkItem);
        }

        return new[]
        {
            new LEncodeStage(LEncodeArgumentBuild(lWorkItem), LWorkStage.LWorkStageEncode, "Encoding", lWorkItem.LWorkOutputPath, false)
        };
    }

    internal static string LEncodeArgumentBuild(LWorkItem lWorkItem, string? lOutputArguments = null)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        var lArguments = new StringBuilder();

        LEncodeHeaderAppend(lArguments);

        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -f concat -safe 0 -i {LEncodeFormat(LEncodeMergeSave(lWorkItem))}");
        }
        else
        {
            bool lVideoCopy = string.Equals(
                    lOutput.LEncodingVideo.LEncodingMode,
                    "Copy",
                    StringComparison.OrdinalIgnoreCase)
                && !LEncodeVideo.LEncodeVideoCheck(lWorkItem, lOutput);
            lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lWorkItem.LWorkOrigin)}");
            lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
            if (lVideoCopy && lWorkItem.LWorkOrigin > TimeSpan.Zero)
            {
                // Stream-copy seeking otherwise retains packets before the requested
                // boundary. At a keyframe cut that exposes the preceding section in
                // the output even though the user selected an exact interval.
                lArguments.Append(" -copypriorss 0");
            }
            if (!lVideoCopy && lWorkItem.LWorkOrigin > TimeSpan.Zero)
            {
                // Fast input seeking can expose preroll packets from copied companion
                // streams. Discard them at the output boundary when video is decoded.
                lArguments.Append(" -ss 0");
            }
            if (lWorkItem.LWorkEnd > lWorkItem.LWorkOrigin)
            {
                lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lWorkItem.LWorkDuration)}");
            }
        }

        LEncodeVideo.LEncodeVideoAppend(lArguments, lWorkItem, lOutput);
        LEncodeAudio.LEncodeAudioAppend(lArguments, lOutput);

        if (!string.IsNullOrWhiteSpace(lOutputArguments))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lOutputArguments}");
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        return lArguments.ToString();
    }

    internal static string LEncodeRemedyBuild(
        LDossierCategory lRemedyCategory, string lRemedyOutputPath, string? lRemedyArgument = null)
    {
        if (lRemedyCategory == LDossierCategory.LDossierCategoryReencode)
        {
            return "-map 0";
        }

        string lRemedyExtension = Path.GetExtension(lRemedyOutputPath).TrimStart('.').ToLowerInvariant();
        bool lRemedyFaststart = lRemedyExtension is "mp4" or "m4v" or "m4a" or "mov";
        string lRemedyBitstream = string.IsNullOrWhiteSpace(lRemedyArgument)
            ? string.Empty
            : $" {lRemedyArgument.Trim()}";
        return lRemedyFaststart
            ? $"-map 0 -c copy{lRemedyBitstream} -movflags +faststart"
            : $"-map 0 -c copy{lRemedyBitstream}";
    }

    internal static string LEncodeRepairBuild(string lInputPath, string lRemedy, string lOutputPath)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lInputPath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" {lRemedy}");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lOutputPath)}");
        return lArguments.ToString();
    }

    private static string LEncodeMergeSave(LWorkItem lWorkItem)
    {
        string lMergeListPath = Path.Combine(LDepot.LDepotMergeRead(), $"{lWorkItem.LWorkId:N}.txt");
        var lMergeList = new StringBuilder();
        foreach (string lMergeSource in lWorkItem.LWorkMergeSources)
        {
            string lMergeEscaped = lMergeSource.Replace("\\", "/", StringComparison.Ordinal).Replace("'", "'\\''", StringComparison.Ordinal);
            lMergeList.Append(CultureInfo.InvariantCulture, $"file '{lMergeEscaped}'\n");
        }

        File.WriteAllText(lMergeListPath, lMergeList.ToString());
        return lMergeListPath;
    }

    private static IReadOnlyList<LEncodeStage> LEncodeStepsBuild(LWorkItem lWorkItem)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        string lAudioFolder = LDepot.LDepotAudioRead();
        string lRawWav = Path.Combine(lAudioFolder, $"{lWorkItem.LWorkId:N}.raw.wav");
        string lProcessedWav = Path.Combine(lAudioFolder, $"{lWorkItem.LWorkId:N}.proc.wav");

        var lStages = new List<LEncodeStage>();

        var lExtract = new StringBuilder();
        LEncodeHeaderAppend(lExtract);
        lExtract.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lExtract.Append(" -vn -c:a pcm_s16le");
        lExtract.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lRawWav)}");
        lStages.Add(new LEncodeStage(lExtract.ToString(), LWorkStage.LWorkStageExtract, "Extracting audio", lRawWav, true));

        string lAudioInputWav = lRawWav;
        int lTwoPassIndex = LEncodeChain.LEncodePassRead(lWorkItem.LWorkAudio);

        if (lWorkItem.LWorkAudio.LWorkAudioActive)
        {
            if (lTwoPassIndex >= 0)
            {
                string? lAnalyzeChain = LEncodeChain.LEncodeChainBuild(
                    lWorkItem.LWorkAudio, LEncodeChainMode.LEncodeChainAnalyze, lTwoPassIndex);
                var lAnalyze = new StringBuilder();
                LEncodeHeaderAppend(lAnalyze);
                lAnalyze.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lRawWav)}");
                lAnalyze.Append(CultureInfo.InvariantCulture, $" -af {LEncodeFormat(lAnalyzeChain!)}");
                lAnalyze.Append(" -f null -");
                lStages.Add(new LEncodeStage(lAnalyze.ToString(), LWorkStage.LWorkStageAnalyze, "Analyzing audio", string.Empty, false, true));
            }

            string? lChain = LEncodeChain.LEncodeChainBuild(
                lWorkItem.LWorkAudio,
                lTwoPassIndex >= 0 ? LEncodeChainMode.LEncodeChainCorrection : LEncodeChainMode.LEncodeChainPlain,
                lTwoPassIndex);
            if (lChain is not null)
            {
                var lProcess = new StringBuilder();
                LEncodeHeaderAppend(lProcess);
                lProcess.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lRawWav)}");
                lProcess.Append(CultureInfo.InvariantCulture, $" -af {LEncodeFormat(lChain)}");
                lProcess.Append(" -c:a pcm_s16le");
                lProcess.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lProcessedWav)}");
                lStages.Add(new LEncodeStage(lProcess.ToString(), LWorkStage.LWorkStageProcess, "Processing audio", lProcessedWav, true));
                lAudioInputWav = lProcessedWav;
            }
        }

        var lMux = new StringBuilder();
        LEncodeHeaderAppend(lMux);
        lMux.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lMux.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lAudioInputWav)}");

        lMux.Append(" -map 0:v:0?");
        lMux.Append(" -map 1:a:0");

        if (string.Equals(lOutput.LEncodingVideo.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideo.LEncodeVideoCheck(lWorkItem, lOutput))
        {
            lMux.Append(" -c:v copy -avoid_negative_ts make_zero");
        }
        else
        {
            LEncodeVideo.LEncodeEncoderAppend(lMux, lWorkItem, lOutput);
        }

        LEncodeAudio.LEncodeMuxAppend(lMux, lOutput);
        lMux.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        lStages.Add(new LEncodeStage(lMux.ToString(), LWorkStage.LWorkStageMux, "Encoding output", lWorkItem.LWorkOutputPath, false));

        return lStages;
    }

    private static void LEncodeHeaderAppend(StringBuilder lArguments)
    {
        lArguments.Append("-hide_banner -nostdin -y");
        lArguments.Append(" -progress pipe:1 -nostats");
        lArguments.Append(CultureInfo.InvariantCulture,
            $" -stats_period {LEncodeStatsPeriod.ToString("0.###", CultureInfo.InvariantCulture)}");
    }

    internal static bool LEncodeSourceCheck(string lValue) =>
        string.IsNullOrWhiteSpace(lValue) || string.Equals(lValue, "Same as source", StringComparison.OrdinalIgnoreCase);

    private static string LEncodeTimeFormat(TimeSpan lTime) =>
        lTime.TotalSeconds.ToString("0.#######", CultureInfo.InvariantCulture);

    internal static string LEncodeFormat(string lPath) => $"\"{lPath}\"";
}
