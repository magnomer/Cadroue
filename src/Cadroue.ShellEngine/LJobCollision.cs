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

        return LJobStreamValidate();
    }

    private string LJobStreamValidate()
    {
        if (lJobItem.LWorkKind == LWorkKind.LWorkKindMerge)
        {
            return string.Empty;
        }

        LEncoding pOutput = lJobItem.LWorkOutput;
        bool pVideoCopy = string.Equals(
                pOutput.LEncodingVideo.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            || string.Equals(pOutput.LEncodingVideo.LEncodingMode, "Smart", StringComparison.OrdinalIgnoreCase);
        bool pAudioCopy = string.Equals(
                pOutput.LEncodingAudio.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeAudio.LEncodeExcludeCheck(pOutput);
        if (!pVideoCopy && !pAudioCopy)
        {
            return string.Empty;
        }

        lJobItem.LWorkSourceMedia ??= LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken);
        if (lJobItem.LWorkSourceMedia is not { } pSource)
        {
            return string.Empty;
        }

        string pContainer = pOutput.LEncodingContainer;
        if (pVideoCopy
            && pSource.LWorkMediaVideo
            && !LRepertoireCatalog.LRepertoireVideoCheck(pSource.LWorkMediaCodec, pContainer))
        {
            return $"the copied video stream ('{pSource.LWorkMediaCodec}') cannot be stored in the {pContainer} " +
                $"container; choose an encoder or another container";
        }

        if (pAudioCopy
            && pSource.LWorkMediaSamplerate > 0
            && !LRepertoireCatalog.LRepertoireAudioCheck(pSource.LWorkAudioCodec, pContainer))
        {
            return $"the copied audio stream ('{pSource.LWorkAudioCodec}') cannot be stored in the {pContainer} " +
                $"container; choose an encoder or another container";
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

        if (LJobClaim(pTarget))
        {
            return string.Empty;
        }

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
                    $"Output is the source; encoding to '{Path.GetFileName(pStagePath)}' " +
                    $"and renaming the source once finished");
                return string.Empty;
            }

            string pFreePath = LJobPathResolve(pTarget, pOutput.LEncodingCollisionSuffix);
            try
            {
                File.Move(pTarget, pFreePath, true);
                LRunner.LRunnerRecord($"Output exists; renaming existing file to '{Path.GetFileName(pFreePath)}'");
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
                string pJobFailure =
                    $"Could not rename the existing file '{Path.GetFileName(pTarget)}'; leaving it untouched";
                LRunner.LRunnerRecord(pJobFailure, pException);
                return pJobFailure;
            }

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
