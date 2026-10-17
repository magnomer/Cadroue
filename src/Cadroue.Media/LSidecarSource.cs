using Cadroue.Core;

namespace Cadroue.Media;

public static class LSidecarSource
{
    public static LSidecarSourceResult LSidecarSourceResolve(string lSidecarPath, LSidecar lSidecar)
    {
        string lSidecarFolder = Path.GetDirectoryName(Path.GetFullPath(lSidecarPath)) ?? string.Empty;

        foreach ((string lCandidatePath, LSidecarSourceKind lCandidateKind)
            in LSidecarCandidatesRead(lSidecarFolder, lSidecar))
        {
            if (string.IsNullOrWhiteSpace(lCandidatePath) || !File.Exists(lCandidatePath))
            {
                continue;
            }

            if (LSidecarSourceMatch(lCandidatePath, lSidecar.LSidecarSource))
            {
                return new LSidecarSourceResult(
                    Path.GetFullPath(lCandidatePath),
                    lCandidateKind,
                    true,
                    lSidecar.LSidecarSource.LSidecarFileName);
            }
        }

        foreach ((string lCandidatePath, LSidecarSourceKind lCandidateKind)
            in LSidecarCandidatesRead(lSidecarFolder, lSidecar))
        {
            if (!string.IsNullOrWhiteSpace(lCandidatePath) && File.Exists(lCandidatePath))
            {
                return new LSidecarSourceResult(
                    Path.GetFullPath(lCandidatePath),
                    lCandidateKind,
                    false,
                    lSidecar.LSidecarSource.LSidecarFileName);
            }
        }

        return new LSidecarSourceResult(
            string.Empty,
            LSidecarSourceKind.LSidecarSourceMissing,
            false,
            lSidecar.LSidecarSource.LSidecarFileName);
    }

    public static bool LSidecarSourceMatch(string lSidecarMediaPath, LSidecarSourceEntry lSidecarSource)
    {
        try
        {
            var lSidecarFile = new FileInfo(lSidecarMediaPath);
            if (!lSidecarFile.Exists || lSidecarFile.Length != lSidecarSource.LSidecarLength)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(lSidecarSource.LSidecarPartialHash))
            {
                return lSidecarSource.LSidecarWriteTicks != 0
                    && lSidecarFile.LastWriteTimeUtc.Ticks == lSidecarSource.LSidecarWriteTicks;
            }

            return string.Equals(
                LSidecarHash.LSidecarHashCreate(lSidecarFile),
                lSidecarSource.LSidecarPartialHash,
                StringComparison.Ordinal);
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static bool LSidecarCacheMatch(
        LSidecarSourceEntry lSidecarSource,
        long lSidecarLength,
        string lSidecarHash) =>
        lSidecarSource.LSidecarLength == lSidecarLength
        && !string.IsNullOrWhiteSpace(lSidecarHash)
        && string.Equals(lSidecarSource.LSidecarPartialHash, lSidecarHash, StringComparison.Ordinal);

    private static IEnumerable<(string, LSidecarSourceKind)> LSidecarCandidatesRead(
        string lSidecarFolder,
        LSidecar lSidecar)
    {
        if (!string.IsNullOrWhiteSpace(lSidecar.LSidecarSource.LSidecarFileName)
            && !string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            yield return (
                Path.Combine(lSidecarFolder, lSidecar.LSidecarSource.LSidecarFileName),
                LSidecarSourceKind.LSidecarSourceSibling);
        }

        if (!string.IsNullOrWhiteSpace(lSidecar.LSidecarSource.LSidecarRelativePath)
            && !string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            string lSidecarRelative;
            try
            {
                lSidecarRelative = Path.GetFullPath(
                    Path.Combine(lSidecarFolder, lSidecar.LSidecarSource.LSidecarRelativePath));
            }
            catch (Exception lException) when (lException is ArgumentException or NotSupportedException)
            {
                lSidecarRelative = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(lSidecarRelative))
            {
                yield return (lSidecarRelative, LSidecarSourceKind.LSidecarSourceRelative);
            }
        }

        if (!string.IsNullOrWhiteSpace(lSidecar.LSidecarSource.LSidecarAbsolutePath))
        {
            yield return (lSidecar.LSidecarSource.LSidecarAbsolutePath, LSidecarSourceKind.LSidecarSourceAbsolute);
        }
    }
}
