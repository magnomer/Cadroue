using System.IO;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private string LJobValidate()
    {
        if (lJobItem.LWorkKind == LWorkKind.LWorkKindMerge)
        {
            if (lJobItem.LWorkMergeSources.Count == 0)
            {
                return "the merge has no source files (the stored job is incomplete or corrupt)";
            }

            foreach (string pMergeSource in lJobItem.LWorkMergeSources)
            {
                if (string.IsNullOrWhiteSpace(pMergeSource) || !File.Exists(pMergeSource))
                {
                    return $"a merge source is missing: '{pMergeSource}'";
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(lJobItem.LWorkSourcePath) || !File.Exists(lJobItem.LWorkSourcePath))
        {
            return $"the source file is missing: '{lJobItem.LWorkSourcePath}'";
        }

        if (string.IsNullOrWhiteSpace(lJobItem.LWorkOutputPath))
        {
            return "the output path is empty (the stored job is incomplete or corrupt)";
        }

        string pPolicy = lJobItem.LWorkOutput.LEncodingCollision;
        bool pPreserved = string.Equals(pPolicy, "Rename existing", StringComparison.Ordinal)
            || string.Equals(pPolicy, "Rename output", StringComparison.Ordinal);
        if (LJobCollisionCheck(lJobItem.LWorkOutputPath, LJobInputsRead()) && !pPreserved)
        {
            return "the output path is the same as an input file; the source will not be overwritten";
        }

        return string.Empty;
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

    private string LJobCollisionApply()
    {
        LEncoding pOutput = lJobItem.LWorkOutput;
        string pTarget = lJobItem.LWorkOutputPath;
        if (string.IsNullOrWhiteSpace(pTarget))
        {
            return string.Empty;
        }

        // Claim the intended name atomically. Success means it was free and is now ours,
        // so no second instance can pick the same "free" name and clobber this output.
        if (LJobClaim(pTarget))
        {
            return string.Empty;
        }

        // The name is taken — a pre-existing file or another instance. Apply the policy.
        if (string.Equals(pOutput.LEncodingCollision, "Rename output", StringComparison.Ordinal))
        {
            string pFreePath = LJobPathResolve(pTarget, pOutput.LEncodingCollisionSuffix);
            lJobItem.LWorkOutputSet(pFreePath, Path.GetFileName(pFreePath));
            LRunner.LRunnerRecord($"Output exists; renaming output to '{Path.GetFileName(pFreePath)}'");
            return string.Empty;
        }

        if (string.Equals(pOutput.LEncodingCollision, "Rename existing", StringComparison.Ordinal))
        {
            if (LJobCollisionCheck(pTarget, LJobInputsRead()))
            {
                string pStagePath = LJobPathResolve(pTarget, ".cadstage");
                lJobFinalPath = pTarget;
                lJobItem.LWorkOutputSet(pStagePath, Path.GetFileName(pTarget));
                LRunner.LRunnerRecord(
                    $"Output is the source; encoding to '{Path.GetFileName(pStagePath)}' and renaming the source once finished");
                return string.Empty;
            }

            string pFreePath = LJobPathResolve(pTarget, pOutput.LEncodingCollisionSuffix);
            try
            {
                // pFreePath is our own reservation placeholder; overwriting it with the
                // existing file is intended. A genuine failure (locked/denied) must abort
                // so the pre-existing file is never destroyed by the encode that follows.
                File.Move(pTarget, pFreePath, true);
                LRunner.LRunnerRecord($"Output exists; renaming existing file to '{Path.GetFileName(pFreePath)}'");
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
                string pJobFailure = $"Could not rename the existing file '{Path.GetFileName(pTarget)}'; leaving it untouched";
                LRunner.LRunnerRecord(pJobFailure, pException);
                return pJobFailure;
            }

            // The existing file has moved aside; reclaim the now-free target so no other
            // instance grabs it before the encode writes.
            LJobClaim(pTarget);
        }

        return string.Empty;
    }

    private void LJobStageCommit()
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
            string pKeepPath = LJobPathResolve(pFinalPath, lJobItem.LWorkOutput.LEncodingCollisionSuffix);
            File.Move(pFinalPath, pKeepPath, true);
            LRunner.LRunnerRecord($"Renamed the existing source to '{Path.GetFileName(pKeepPath)}'");
        }

        File.Move(pStagePath, pFinalPath, true);
        lJobItem.LWorkOutputSet(pFinalPath, Path.GetFileName(pFinalPath));
        LRunner.LRunnerRecord($"Moved the encoded output into place at '{Path.GetFileName(pFinalPath)}'");
    }
}
