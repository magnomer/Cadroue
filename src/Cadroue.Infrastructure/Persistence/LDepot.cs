using Cadroue.Core;

namespace Cadroue.Infrastructure;

public enum LDepotFolder
{
    LDepotFolderScheduled,
    LDepotFolderRunning,
    LDepotFolderDone,
    LDepotFolderFailed,
    LDepotFolderCancelled
}

public static class LDepot
{
    private const string LDepotWorkFolder = "workspace";

    internal const string LDepotPaletteFolder = "palettes";

    private const string LDepotAudioFolder = "audiowork";

    private const string LDepotMergeFolder = "mergework";

    private const string LDepotBridgeFolder = "bridgework";

    private const string LDepotPassFolder = "passwork";

    public const string LDepotIndexFile = "work.db";

    private static string? lDepotRootOverride;

    public static void LDepotRootSet(string? lDepotRoot)
    {
        lDepotRootOverride = string.IsNullOrWhiteSpace(lDepotRoot) ? null : lDepotRoot.Trim();
    }

    public static string LDepotRootResolve(string? lDepotRoot) =>
        string.IsNullOrWhiteSpace(lDepotRoot) ? LDepotDefaultRead() : lDepotRoot.Trim();

    public static string LDepotRootRead()
    {
        if (lDepotRootOverride is { } lDepotRoot)
        {
            return lDepotRoot;
        }

        return LDepotDefaultRead();
    }

    public static string LDepotDefaultRead() => Path.Combine(LDepotHomeRead(), LDepotWorkFolder);

    public static string LDepotHomeRead()
    {
        string? lDepotHome = AppContext.GetData(LDepotHome.LDepotHomeKey) as string;
        if (!string.IsNullOrWhiteSpace(lDepotHome))
        {
            return lDepotHome.Trim();
        }

        string lDepotApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lDepotApplicationData, LDepotHome.LDepotHomeFolder);
    }

    public static string LDepotLocalRead()
    {
        string? lDepotHome = AppContext.GetData(LDepotHome.LDepotHomeKey) as string;
        if (!string.IsNullOrWhiteSpace(lDepotHome))
        {
            return Path.Combine(lDepotHome.Trim(), LDepotHome.LDepotHomeLocal);
        }

        string lDepotLocalData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(lDepotLocalData, LDepotHome.LDepotHomeFolder);
    }

    public static string LDepotIndexFind(string? lDepotRoot = null) =>
        Path.Combine(lDepotRoot ?? LDepotRootRead(), LDepotIndexFile);

    public static string LDepotPaletteRead()
    {
        string lDepotPalettes = Path.Combine(LDepotRootRead(), LDepotPaletteFolder);
        Directory.CreateDirectory(lDepotPalettes);
        return lDepotPalettes;
    }

    public static string LDepotAudioRead()
    {
        string lDepotAudio = Path.Combine(LDepotRootRead(), LDepotAudioFolder);
        Directory.CreateDirectory(lDepotAudio);
        return lDepotAudio;
    }

    public static string LDepotMergeRead()
    {
        string lDepotMerge = Path.Combine(LDepotRootRead(), LDepotMergeFolder);
        Directory.CreateDirectory(lDepotMerge);
        return lDepotMerge;
    }

    public static string LDepotBridgeRead()
    {
        string lDepotBridge = Path.Combine(LDepotRootRead(), LDepotBridgeFolder);
        Directory.CreateDirectory(lDepotBridge);
        return lDepotBridge;
    }

    public static string LDepotPassRead()
    {
        string lDepotPass = Path.Combine(LDepotRootRead(), LDepotPassFolder);
        Directory.CreateDirectory(lDepotPass);
        return lDepotPass;
    }

    public static string LDepotFolderRead(LDepotFolder lDepotFolder, string? lDepotRoot = null) =>
        Path.Combine(lDepotRoot ?? LDepotRootRead(), LDepotNameRead(lDepotFolder));

    public static string LDepotFileRead(LDepotFolder lDepotFolder, Guid lWorkId) =>
        Path.Combine(LDepotFolderRead(lDepotFolder), $"{lWorkId:N}.json");

    public static void LDepotCreate(string? lDepotRoot = null)
    {
        lDepotRoot ??= LDepotRootRead();
        Directory.CreateDirectory(lDepotRoot);
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            Directory.CreateDirectory(LDepotFolderRead(lDepotFolder, lDepotRoot));
        }
    }

    public static IEnumerable<string> LDepotFilesRead(LDepotFolder lDepotFolder, string? lDepotRoot = null)
    {
        string lDepotFolderPath = LDepotFolderRead(lDepotFolder, lDepotRoot);
        return Directory.Exists(lDepotFolderPath)
            ? Directory.EnumerateFiles(lDepotFolderPath, "*.json")
            : Array.Empty<string>();
    }

    public static bool LDepotRunningCheck(string lDepotRoot)
    {
        string lDepotRunning = LDepotFolderRead(LDepotFolder.LDepotFolderRunning, lDepotRoot);
        return Directory.Exists(lDepotRunning) && Directory.EnumerateFiles(lDepotRunning).Any();
    }

    private static string LDepotNameRead(LDepotFolder lDepotFolder) => lDepotFolder switch
    {
        LDepotFolder.LDepotFolderRunning => "running",
        LDepotFolder.LDepotFolderDone => "done",
        LDepotFolder.LDepotFolderFailed => "failed",
        LDepotFolder.LDepotFolderCancelled => "cancelled",
        _ => "scheduled"
    };
}
