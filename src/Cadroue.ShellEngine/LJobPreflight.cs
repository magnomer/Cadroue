using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobPreflight
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;
    private readonly LJobCollision lJobCollision;

    internal LJobPreflight(
        LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobStopToken, LJobCollision lJobWorkCollision)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobToken = lJobStopToken;
        lJobCollision = lJobWorkCollision;
    }

    internal async Task<string> LJobValidate()
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

        if (lJobCollision.LJobOverwriteCheck())
        {
            return "the output path is the same as an input file; the source will not be overwritten";
        }

        return await LJobStreamValidate().ConfigureAwait(false);
    }

    private async Task<string> LJobStreamValidate()
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

        lJobItem.LWorkSourceMedia ??=
            await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken, lJobOwner.LRunnerLevel)
                .ConfigureAwait(false);
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
}
