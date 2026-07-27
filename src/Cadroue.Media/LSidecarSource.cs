using System.Security.Cryptography;

namespace Cadroue.Media;

public enum LSidecarSourceKind
{
    LSidecarSourceSibling,
    LSidecarSourceRelative,
    LSidecarSourceAbsolute,
    LSidecarSourceMissing
}

public sealed record LSidecarSourceResult(
    string LSidecarResultPath,
    LSidecarSourceKind LSidecarResultKind,
    bool LSidecarResultVerified);

public static class LSidecarSource
{
    private const int LSidecarHashSampleSize = 1024 * 1024;

    public static LSidecarSourceResult LSidecarSourceResolve(string lSidecarPath, LSidecar lSidecar)
    {
        string lSidecarFolder = Path.GetDirectoryName(Path.GetFullPath(lSidecarPath)) ?? string.Empty;

        foreach ((string lCandidatePath, LSidecarSourceKind lCandidateKind) in LSidecarCandidatesRead(lSidecarFolder, lSidecar))
        {
            if (string.IsNullOrWhiteSpace(lCandidatePath) || !File.Exists(lCandidatePath))
            {
                continue;
            }

            if (LSidecarVerifyCheck(lCandidatePath, lSidecar.Source))
            {
                return new LSidecarSourceResult(Path.GetFullPath(lCandidatePath), lCandidateKind, true);
            }
        }

        foreach ((string lCandidatePath, LSidecarSourceKind lCandidateKind) in LSidecarCandidatesRead(lSidecarFolder, lSidecar))
        {
            if (!string.IsNullOrWhiteSpace(lCandidatePath) && File.Exists(lCandidatePath))
            {
                return new LSidecarSourceResult(Path.GetFullPath(lCandidatePath), lCandidateKind, false);
            }
        }

        return new LSidecarSourceResult(string.Empty, LSidecarSourceKind.LSidecarSourceMissing, false);
    }

    public static bool LSidecarVerifyCheck(string lSidecarMediaPath, LSidecarSourceRecord lSidecarSource)
    {
        try
        {
            var lSidecarFile = new FileInfo(lSidecarMediaPath);
            if (!lSidecarFile.Exists || lSidecarFile.Length != lSidecarSource.Length)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(lSidecarSource.PartialHash))
            {
                return true;
            }

            return string.Equals(
                LSidecarHashCreate(lSidecarFile),
                lSidecarSource.PartialHash,
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
        if (!string.IsNullOrWhiteSpace(lSidecar.Source.FileName) && !string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            yield return (Path.Combine(lSidecarFolder, lSidecar.Source.FileName), LSidecarSourceKind.LSidecarSourceSibling);
        }

        if (!string.IsNullOrWhiteSpace(lSidecar.Source.RelativePath) && !string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            string lSidecarRelative;
            try
            {
                lSidecarRelative = Path.GetFullPath(Path.Combine(lSidecarFolder, lSidecar.Source.RelativePath));
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

        if (!string.IsNullOrWhiteSpace(lSidecar.Source.AbsolutePath))
        {
            yield return (lSidecar.Source.AbsolutePath, LSidecarSourceKind.LSidecarSourceAbsolute);
        }
    }

    private static string LSidecarHashCreate(FileInfo lSidecarFile)
    {
        using var lSidecarSha = SHA256.Create();
        using FileStream lSidecarStream = lSidecarFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] lSidecarBuffer = new byte[LSidecarHashSampleSize];

        int lSidecarFirstRead = lSidecarStream.Read(lSidecarBuffer, 0, lSidecarBuffer.Length);
        if (lSidecarFirstRead > 0)
        {
            lSidecarSha.TransformBlock(lSidecarBuffer, 0, lSidecarFirstRead, null, 0);
        }

        if (lSidecarStream.Length > LSidecarHashSampleSize)
        {
            lSidecarStream.Position = Math.Max(0, lSidecarStream.Length - LSidecarHashSampleSize);
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
