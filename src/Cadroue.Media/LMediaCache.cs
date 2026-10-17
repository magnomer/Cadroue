using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaCache
{
    public static string LMediaHomeRead()
    {
        string? lMediaHome = AppContext.GetData(LDepotHome.LDepotHomeKey) as string;
        if (!string.IsNullOrWhiteSpace(lMediaHome))
        {
            return lMediaHome.Trim();
        }

        string lMediaApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lMediaApplicationData, LDepotHome.LDepotHomeFolder);
    }

    internal static bool LMediaCacheSave(string lMediaCachePath, string lMediaJson)
    {
        string lMediaTempPath = lMediaCachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string? lMediaFolder = Path.GetDirectoryName(lMediaCachePath);
            if (!string.IsNullOrWhiteSpace(lMediaFolder))
            {
                Directory.CreateDirectory(lMediaFolder);
            }

            File.WriteAllText(lMediaTempPath, lMediaJson);
            File.Move(lMediaTempPath, lMediaCachePath, overwrite: true);
            return true;
        }
        catch (Exception lMediaException) when (lMediaException is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (File.Exists(lMediaTempPath))
                {
                    File.Delete(lMediaTempPath);
                }
            }
            catch (Exception lMediaCleanup) when (lMediaCleanup is IOException or UnauthorizedAccessException)
            {
            }

            return false;
        }
    }
}
