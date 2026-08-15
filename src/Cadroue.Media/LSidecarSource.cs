using System.Security.Cryptography;

using Cadroue.Core;

namespace Cadroue.Media;

public static class LSidecarSource
{
    private const int LSidecarHashSize = 1024 * 1024;

    public static LSidecarSourceResult LSidecarSourceResolve(string lSidecarPath, LSidecar lSidecar)
    {
        string lSidecarFolder = Path.GetDirectoryName(Path.GetFullPath(lSidecarPath)) ?? string.Empty;

        foreach ((string lCandidatePath, LSidecarSourceKind lCandidateKind) in LSidecarCandidatesRead(lSidecarFolder, lSidecar))
        {
            if (string.IsNullOrWhiteSpace(lCandidatePath) || !File.Exists(lCandidatePath))
            {
                continue;
            }

            if (LSidecarSourceMatch(lCandidatePath, lSidecar.LSidecarSource))
            {
                return new LSidecarSourceResult(Path.GetFullPath(lCandidatePath), lCandidateKind, true, lSidecar.LSidecarSource.LSidecarFileName);
            }
        }

        foreach ((string lCandidatePath, LSidecarSourceKind lCandidateKind) in LSidecarCandidatesRead(lSidecarFolder, lSidecar))
        {
            if (!string.IsNullOrWhiteSpace(lCandidatePath) && File.Exists(lCandidatePath))
            {
                return new LSidecarSourceResult(Path.GetFullPath(lCandidatePath), lCandidateKind, false, lSidecar.LSidecarSource.LSidecarFileName);
            }
        }

        return new LSidecarSourceResult(string.Empty, LSidecarSourceKind.LSidecarSourceMissing, false, lSidecar.LSidecarSource.LSidecarFileName);
    }

    public static bool LSidecarSourceMatch(string lSidecarMediaPath, LSidecarSourceRecord lSidecarSource)
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
                return true;
            }

            return string.Equals(
                LSidecarHashCreate(lSidecarFile),
                lSidecarSource.LSidecarPartialHash,
                StringComparison.Ordinal);
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static IEnumerable<(string, LSidecarSourceKind)> LSidecarCandidatesRead(
        string lSidecarFolder,
        LSidecar lSidecar)
    {
        if (!string.IsNullOrWhiteSpace(lSidecar.LSidecarSource.LSidecarFileName) && !string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            yield return (Path.Combine(lSidecarFolder, lSidecar.LSidecarSource.LSidecarFileName), LSidecarSourceKind.LSidecarSourceSibling);
        }

        if (!string.IsNullOrWhiteSpace(lSidecar.LSidecarSource.LSidecarRelativePath) && !string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            string lSidecarRelative;
            try
            {
                lSidecarRelative = Path.GetFullPath(Path.Combine(lSidecarFolder, lSidecar.LSidecarSource.LSidecarRelativePath));
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

    private static string LSidecarHashCreate(FileInfo lSidecarFile)
    {
        using var lSidecarSha = SHA256.Create();
        using FileStream lSidecarStream = lSidecarFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] lSidecarBuffer = new byte[LSidecarHashSize];

        int lSidecarFirstRead = lSidecarStream.Read(lSidecarBuffer, 0, lSidecarBuffer.Length);
        if (lSidecarFirstRead > 0)
        {
            lSidecarSha.TransformBlock(lSidecarBuffer, 0, lSidecarFirstRead, null, 0);
        }

        if (lSidecarStream.Length > LSidecarHashSize)
        {
            lSidecarStream.Position = Math.Max(0, lSidecarStream.Length - LSidecarHashSize);
            int lSidecarLastRead = lSidecarStream.Read(lSidecarBuffer, 0, lSidecarBuffer.Length);
            if (lSidecarLastRead > 0)
            {
                lSidecarSha.TransformBlock(lSidecarBuffer, 0, lSidecarLastRead, null, 0);
            }
        }

        lSidecarSha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(lSidecarSha.Hash ?? Array.Empty<byte>());
    }
}
