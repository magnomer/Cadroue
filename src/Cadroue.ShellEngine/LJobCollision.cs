using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobCollision
{
    private readonly LWorkItem lJobItem;
    private readonly LJobReservation lJobReservation;
    private string lJobFinalPath = string.Empty;

    internal LJobCollision(LWorkItem lJobWorkItem, LJobReservation lJobWorkReservation)
    {
        lJobItem = lJobWorkItem;
        lJobReservation = lJobWorkReservation;
    }

    private IEnumerable<string> LJobInputsRead() =>
        lJobItem.LWorkKind == LWorkKind.LWorkKindMerge
            ? lJobItem.LWorkMergeSources
            : new[] { lJobItem.LWorkSourcePath };

    internal static bool LJobCollisionCheck(string pOutputPath, IEnumerable<string> pInputPaths)
    {
        string pOutputFullPath = Path.GetFullPath(pOutputPath);
        return pInputPaths.Any(pInputPath => string.Equals(
            pOutputFullPath,
            Path.GetFullPath(pInputPath),
            StringComparison.OrdinalIgnoreCase));
    }

    internal bool LJobOverwriteCheck()
    {
        string pPolicy = lJobItem.LWorkOutput.LEncodingCollision;
        bool pPreserved = string.Equals(pPolicy, "Rename existing", StringComparison.Ordinal)
            || string.Equals(pPolicy, "Rename output", StringComparison.Ordinal);
        return LJobCollisionCheck(lJobItem.LWorkOutputPath, LJobInputsRead()) && !pPreserved;
    }

    internal bool LJobExistingCheck(string pOutput) =>
        LJobCollisionCheck(pOutput, LJobInputsRead())
        || (lJobFinalPath.Length > 0 && string.Equals(
            Path.GetFullPath(pOutput),
            Path.GetFullPath(lJobFinalPath),
            StringComparison.OrdinalIgnoreCase));

    internal string LJobCollisionApply()
    {
        LEncoding pOutput = lJobItem.LWorkOutput;
        string pTarget = lJobItem.LWorkOutputPath;
        if (string.IsNullOrWhiteSpace(pTarget))
        {
            return string.Empty;
        }

        if (lJobReservation.LJobClaim(pTarget))
        {
            return string.Empty;
        }

        if (string.Equals(pOutput.LEncodingCollision, "Rename output", StringComparison.Ordinal))
        {
            string pFreePath = lJobReservation.LJobPathResolve(pTarget, pOutput.LEncodingCollisionSuffix);
            lJobItem.LWorkOutputSet(pFreePath, Path.GetFileName(pFreePath));
            LRunnerTrace.LRunnerRecord($"Output exists; renaming output to '{Path.GetFileName(pFreePath)}'");
            return string.Empty;
        }

        if (string.Equals(pOutput.LEncodingCollision, "Rename existing", StringComparison.Ordinal))
        {
            if (LJobCollisionCheck(pTarget, LJobInputsRead()))
            {
                string pStagePath = lJobReservation.LJobPathResolve(pTarget, ".cadstage");
                lJobFinalPath = pTarget;
                lJobItem.LWorkOutputSet(pStagePath, Path.GetFileName(pTarget));
                LRunnerTrace.LRunnerRecord(
                    $"Output is the source; encoding to '{Path.GetFileName(pStagePath)}' " +
                    $"and renaming the source once finished");
                return string.Empty;
            }

            string pFreePath = lJobReservation.LJobPathResolve(pTarget, pOutput.LEncodingCollisionSuffix);
            try
            {
                File.Move(pTarget, pFreePath, true);
                LRunnerTrace.LRunnerRecord(
                    $"Output exists; renaming existing file to '{Path.GetFileName(pFreePath)}'");
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
                string pJobFailure =
                    $"Could not rename the existing file '{Path.GetFileName(pTarget)}'; leaving it untouched";
                LRunnerTrace.LRunnerRecord(pJobFailure, pException);
                return pJobFailure;
            }

            lJobReservation.LJobClaim(pTarget);
        }

        return string.Empty;
    }

    internal void LJobStageCommit()
    {
        if (lJobFinalPath.Length == 0)
        {
            return;
        }

        string pStagePath = lJobItem.LWorkOutputPath;
        string pFinalPath = lJobFinalPath;
        lJobFinalPath = string.Empty;

        if (File.Exists(pFinalPath))
        {
            string pKeepPath = lJobReservation.LJobPathResolve(
                pFinalPath, lJobItem.LWorkOutput.LEncodingCollisionSuffix);
            File.Move(pFinalPath, pKeepPath, true);
            LRunnerTrace.LRunnerRecord($"Renamed the existing source to '{Path.GetFileName(pKeepPath)}'");
        }

        File.Move(pStagePath, pFinalPath, true);
        lJobItem.LWorkOutputSet(pFinalPath, Path.GetFileName(pFinalPath));
        LRunnerTrace.LRunnerRecord($"Moved the encoded output into place at '{Path.GetFileName(pFinalPath)}'");
    }
}
