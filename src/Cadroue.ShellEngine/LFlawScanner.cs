using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LFlawScanner
{
    internal static Task<IReadOnlyList<LDossier>> LFlawScanRun(
        LWorkItem lFlawItem, CancellationToken lFlawToken = default) =>
        LFlawScanRun(lFlawItem.LWorkSourcePath, Array.Empty<LFlawKind>(), lFlawToken);

    public static async Task<IReadOnlyList<LDossier>> LFlawScanRun(
        string lFlawSource,
        IReadOnlyCollection<LFlawKind> lFlawKinds,
        CancellationToken lFlawToken = default,
        IProgress<double>? lFlawProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lFlawSource) || !File.Exists(lFlawSource))
        {
            throw new FileNotFoundException(
                "Diagnosis source is missing or unreadable.", lFlawSource ?? string.Empty);
        }

        try
        {
            lFlawProgress?.Report(0);
            LFlawStage lFlawStage =
                await LFlawStage.LFlawStageCreate(lFlawSource, lFlawToken, lFlawProgress).ConfigureAwait(false);
            LFlawEvidence lFlawEvidence = await LFlawProbe.LFlawProbeRun(lFlawStage, lFlawSource);
            IReadOnlyList<LDossier> lFlawDossiers = LFlawDiagnosis.LFlawDiagnosisResolve(lFlawEvidence);
            if (lFlawEvidence.LFlawEvidenceOpened)
            {
                lFlawProgress?.Report(1);
            }

            return LFlawSelection.LFlawKindsResolve(lFlawDossiers, lFlawKinds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception lFlawException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Container structure could not be examined '{Path.GetFileName(lFlawSource)}'",
                lFlawException);
            throw;
        }
    }
}
