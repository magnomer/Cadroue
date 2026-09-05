using System.IO;
using System.Text.Json;

namespace Cadroue.Infrastructure;

public static class LRelayStore
{
    private const string LRelayFolderName = "Cadroue";
    private const string LRelaySubfolderName = "relay";

    private static readonly TimeSpan LRelayStaleAge = TimeSpan.FromHours(12);

    public static string LRelayFileSave(LRelay lRelay)
    {
        string lRelayFolder = LRelayFolderCreate();
        Directory.CreateDirectory(lRelayFolder);
        string lRelayFilePath = Path.Combine(lRelayFolder, $"{lRelay.LRelayId}.json");
        string lRelayJson = JsonSerializer.Serialize(lRelay, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lRelayFilePath, lRelayJson);
        return lRelayFilePath;
    }

    public static LRelay? LRelayFileLoad(string lRelayFilePath)
    {
        if (!LRelayPathCheck(lRelayFilePath) || !File.Exists(lRelayFilePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LRelay>(File.ReadAllText(lRelayFilePath));
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Relay payload unreadable: {lRelayFilePath}", lException);
            return null;
        }
    }

    public static void LRelayFileClear(string lRelayFilePath)
    {
        if (!LRelayPathCheck(lRelayFilePath))
        {
            LTraceLog.LTraceErrorRecord($"Relay payload outside the relay folder was kept: {lRelayFilePath}", null);
            return;
        }

        try
        {
            if (File.Exists(lRelayFilePath))
            {
                File.Delete(lRelayFilePath);
            }
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Relay payload could not be deleted: {lRelayFilePath}", lException);
        }
    }

    public static void LRelayStaleClear()
    {
        string lRelayFolder = LRelayFolderCreate();
        if (!Directory.Exists(lRelayFolder))
        {
            return;
        }

        DateTime lRelayCutoff = DateTime.UtcNow - LRelayStaleAge;
        try
        {
            foreach (string lRelayFilePath in Directory.EnumerateFiles(lRelayFolder, "*.json"))
            {
                if (File.GetLastWriteTimeUtc(lRelayFilePath) < lRelayCutoff)
                {
                    LRelayFileClear(lRelayFilePath);
                }
            }
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Relay folder sweep failed", lException);
        }
    }

    private static bool LRelayPathCheck(string lRelayFilePath)
    {
        if (string.IsNullOrWhiteSpace(lRelayFilePath))
        {
            return false;
        }

        try
        {
            string lRelayFolderFull = Path.GetFullPath(LRelayFolderCreate());
            string lRelayFileFull = Path.GetFullPath(lRelayFilePath);
            return lRelayFileFull.StartsWith(
                    lRelayFolderFull.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path.GetExtension(lRelayFileFull), ".json", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Relay path unusable: {lRelayFilePath}", lException);
            return false;
        }
    }

    private static string LRelayFolderCreate()
    {
        string lLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(lLocalAppData, LRelayFolderName, LRelaySubfolderName);
    }
}
