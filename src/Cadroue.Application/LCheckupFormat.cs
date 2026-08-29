using System.Text;
using Cadroue.Core;

namespace Cadroue.Application;

public readonly record struct LCheckupStrings(
    string LCheckupUntested,
    string LCheckupScanning,
    string LCheckupClean,
    string LCheckupFailed,
    string LCheckupDefectLabel,
    string LCheckupEvidenceLabel,
    string LCheckupRepairLabel);

public static class LCheckupFormat
{
    public static string LCheckupBodyFormat(LCheckupResult lCheckupResult, LCheckupStrings lCheckupStrings)
    {
        switch (lCheckupResult.LCheckupOutcome)
        {
            case LCheckupOutcome.LCheckupOutcomeUntested:
                return lCheckupStrings.LCheckupUntested;
            case LCheckupOutcome.LCheckupOutcomeScanning:
                return lCheckupStrings.LCheckupScanning;
            case LCheckupOutcome.LCheckupOutcomeClean:
                return lCheckupStrings.LCheckupClean;
            case LCheckupOutcome.LCheckupOutcomeFailed:
                return lCheckupStrings.LCheckupFailed;
        }

        if (lCheckupResult.LCheckupDossier is not LDossier lCheckupDossier)
        {
            return lCheckupStrings.LCheckupFailed;
        }

        var lCheckupBuilder = new StringBuilder();
        LCheckupLineAppend(lCheckupBuilder, lCheckupStrings.LCheckupDefectLabel, lCheckupDossier.LDossierDefect);
        LCheckupLineAppend(lCheckupBuilder, lCheckupStrings.LCheckupEvidenceLabel, lCheckupDossier.LDossierEvidenceMechanism);
        LCheckupLineAppend(lCheckupBuilder, lCheckupStrings.LCheckupRepairLabel, lCheckupDossier.LDossierRepair);
        return lCheckupBuilder.ToString();
    }

    private static void LCheckupLineAppend(StringBuilder lCheckupBuilder, string lCheckupLabel, string lCheckupValue)
    {
        if (string.IsNullOrWhiteSpace(lCheckupValue))
        {
            return;
        }

        if (lCheckupBuilder.Length > 0)
        {
            lCheckupBuilder.Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(lCheckupLabel))
        {
            lCheckupBuilder.Append(lCheckupLabel).Append(": ");
        }

        lCheckupBuilder.Append(lCheckupValue);
    }
}
