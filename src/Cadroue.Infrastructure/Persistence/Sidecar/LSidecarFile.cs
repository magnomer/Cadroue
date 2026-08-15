namespace Cadroue.Infrastructure;

internal static class LSidecarFile
{
    internal static bool LSidecarFileSave(string lSidecarPath, string lSidecarContent)
    {
        if (Path.GetDirectoryName(lSidecarPath) is { Length: > 0 } lSidecarFolder)
        {
            Directory.CreateDirectory(lSidecarFolder);
        }

        string lSidecarTempPath = lSidecarPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(lSidecarTempPath, lSidecarContent);
            File.Move(lSidecarTempPath, lSidecarPath, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(lSidecarTempPath))
                {
                    File.Delete(lSidecarTempPath);
                }
            }
            catch (Exception lSidecarCleanup) when (lSidecarCleanup is IOException or UnauthorizedAccessException)
            {
            }

            throw;
        }

        return true;
    }

    internal static string? LSidecarFileRead(string lSidecarPath)
    {
        if (!File.Exists(lSidecarPath))
        {
            return null;
        }

        for (int lSidecarAttempt = 0; ; lSidecarAttempt++)
        {
            try
            {
                return File.ReadAllText(lSidecarPath);
            }
            catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
            {
                if (lSidecarAttempt >= 2)
                {
                    return null;
                }

                Thread.Sleep(15);
            }
        }
    }
}
