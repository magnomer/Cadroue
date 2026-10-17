using System.Security.Cryptography;

namespace Cadroue.Media;

internal static class LSidecarHash
{
    private const int LSidecarHashSize = 1024 * 1024;

    internal static string LSidecarHashCreate(FileInfo lSidecarFile)
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
