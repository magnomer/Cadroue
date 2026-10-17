using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal static class LSidecarLegacy
{
    internal static void LSidecarLegacyMove(string lSidecarFullPath, string lSidecarPreciousPath)
    {
        string lSidecarLegacyPath = Path.ChangeExtension(lSidecarFullPath, LSidecarPath.LSidecarExtension);
        if (string.Equals(lSidecarLegacyPath, lSidecarPreciousPath, StringComparison.OrdinalIgnoreCase)
            || File.Exists(lSidecarPreciousPath)
            || !File.Exists(lSidecarLegacyPath))
        {
            return;
        }

        try
        {
            using (LLatch.LLatchClaim(lSidecarLegacyPath))
            {
                if (File.Exists(lSidecarPreciousPath) || !File.Exists(lSidecarLegacyPath))
                {
                    return;
                }

                string? lSidecarLegacyJson = LSidecarFile.LSidecarFileRead(lSidecarLegacyPath);
                if (lSidecarLegacyJson is null
                    || LSidecarSerializer.LSidecarCoreParse(lSidecarLegacyJson) is not { } lSidecarLegacy
                    || !string.Equals(
                        lSidecarLegacy.LSidecarSource.LSidecarFileName,
                        Path.GetFileName(lSidecarFullPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string lSidecarLegacyCache = LSidecarPath.LSidecarCacheResolve(lSidecarLegacyPath);
                if (File.Exists(lSidecarLegacyCache))
                {
                    File.Move(
                        lSidecarLegacyCache,
                        LSidecarPath.LSidecarCacheResolve(lSidecarPreciousPath),
                        overwrite: false);
                }

                File.Move(lSidecarLegacyPath, lSidecarPreciousPath, overwrite: false);
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or TimeoutException)
        {
        }
    }
}
