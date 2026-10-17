namespace Cadroue.Infrastructure;

public static class LUsher
{
    public static bool LUsherFileExist(string? lUsherPath) =>
        !string.IsNullOrWhiteSpace(lUsherPath) && File.Exists(lUsherPath);

    public static bool LUsherFolderExist(string? lUsherFolder) =>
        !string.IsNullOrWhiteSpace(lUsherFolder) && Directory.Exists(lUsherFolder);

    public static string LUsherNameRead(string lUsherPath) => Path.GetFileName(lUsherPath);

    public static string LUsherStemRead(string lUsherPath) => Path.GetFileNameWithoutExtension(lUsherPath);

    public static string LUsherExtensionRead(string lUsherPath) => Path.GetExtension(lUsherPath).TrimStart('.');

    public static string LUsherProgramResolve(string lUsherName) =>
        Path.Combine(AppContext.BaseDirectory, lUsherName);

    public static string LUsherPathResolve(string lUsherPath)
    {
        try
        {
            return Path.GetFullPath(lUsherPath);
        }
        catch (Exception lPathError) when (
            lPathError is ArgumentException or IOException or NotSupportedException)
        {
            return lUsherPath;
        }
    }
}
