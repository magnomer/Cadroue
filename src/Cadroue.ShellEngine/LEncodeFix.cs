using System.Globalization;
using System.IO;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LEncode
{
    public static IReadOnlyList<LEncodeStage> LEncodeFixBuild(
        LWorkItem lWorkItem, IReadOnlyList<LDossier> lFixRepairable, bool lFixCopy)
    {
        var lFixStages = new List<LEncodeStage>();
        if (lFixCopy)
        {
            lFixStages.Add(new LEncodeStage(
                lWorkItem.LWorkSourcePath,
                LWorkStage.LWorkStageDuplicate,
                "Copying",
                lWorkItem.LWorkOutputPath,
                false));
        }

        LRemedyPlan lFixPlan = LRemedy.LRemedyPlanCreate(lFixRepairable);
        foreach (LRemedyAction lFixAction in lFixPlan.LRemedyActions)
        {
            if (lFixAction.LRemedyDossier.LDossierRepair == LFlawFfvone.LFlawReport)
            {
                continue;
            }

            string lFixArguments = lFixAction.LRemedyCategory == LDossierCategory.LDossierCategoryReencode
                ? LEncodeRecoverBuild(lWorkItem)
                : LEncodeRemedyBuild(
                    lFixAction.LRemedyCategory,
                    lWorkItem.LWorkOutputPath,
                    lFixAction.LRemedyDossier.LDossierRepairArgument);
            lFixStages.Add(new LEncodeStage(
                lFixArguments,
                LWorkStage.LWorkStageRepair, "Repairing", lWorkItem.LWorkOutputPath, false,
                LEncodeStageInput: lFixAction.LRemedyDossier.LDossierRepairInput));
        }

        lFixStages.Add(new LEncodeStage(
            lWorkItem.LWorkOutputPath, LWorkStage.LWorkStageVerify, "Validating", lWorkItem.LWorkOutputPath, false));
        return lFixStages;
    }

    internal static string LEncodeRecoverBuild(LWorkItem lWorkItem)
    {
        string lRecoverEncoder =
            LRepertoireCatalog.LRepertoireEncoderResolve(lWorkItem.LWorkSourceMedia?.LWorkMediaCodec)
            ?? "libx264";
        string lRecoverExtension = Path.GetExtension(lWorkItem.LWorkOutputPath).TrimStart('.').ToLowerInvariant();
        string lRecoverFlags = lRecoverExtension is "mp4" or "m4v" or "m4a" or "mov"
            ? " -movflags +faststart"
            : string.Empty;
        return $"-map 0 -c copy -c:v {lRecoverEncoder} -fps_mode passthrough{lRecoverFlags}";
    }

    internal static string LEncodeRemedyBuild(
        LDossierCategory lRemedyCategory, string lRemedyOutputPath, string? lRemedyArgument = null)
    {
        if (lRemedyCategory == LDossierCategory.LDossierCategoryTransport)
        {
            return "-map 0 -c copy -f mpegts";
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

    internal static string LEncodeRepairBuild(string lInputPath, string lInput, string lRemedy, string lOutputPath)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        if (!string.IsNullOrWhiteSpace(lInput))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lInput.Trim()}");
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lInputPath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" {lRemedy}");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lOutputPath)}");
        return lArguments.ToString();
    }
}
