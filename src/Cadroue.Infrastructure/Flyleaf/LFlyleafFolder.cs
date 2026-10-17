using System.IO;
using System.Text.Json;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal sealed class LFlyleafDocument
{
    public string LFlyleafAssemblyFolder { get; set; } = string.Empty;

    public string LFlyleafSourceFolder { get; set; } = string.Empty;
}

public static class LFlyleafFolder
{
    internal const string LFlyleafSourceName = "source";
    internal const string LFlyleafRuntimeName = "runtime";

    private const string LFlyleafRootFolder = "local-flyleaf";
    private const string LFlyleafRecordName = "local-flyleaf.json";

    public static string LFlyleafRootRead() => Path.Combine(LDepot.LDepotRootRead(), LFlyleafRootFolder);

    public static bool LFlyleafInstalledCheck() => LFlyleafFolderValidate(LFlyleafFolderRead());

    public static string? LFlyleafFolderRead()
    {
        try
        {
            string lRecordPath = LFlyleafRecordFind();
            if (!File.Exists(lRecordPath))
            {
                return null;
            }

            LFlyleafDocument? lRecord = JsonSerializer.Deserialize<LFlyleafDocument>(File.ReadAllText(lRecordPath));
            if (LFlyleafFolderValidate(lRecord?.LFlyleafAssemblyFolder))
            {
                return lRecord!.LFlyleafAssemblyFolder;
            }

            if (!string.IsNullOrWhiteSpace(lRecord?.LFlyleafSourceFolder)
                && Directory.Exists(lRecord.LFlyleafSourceFolder)
                && LFlyleafInstall.LFlyleafSourceFind(lRecord.LFlyleafSourceFolder) is not null)
            {
                lRecord.LFlyleafAssemblyFolder = LFlyleafInstall.LFlyleafRuntimeCreate(lRecord.LFlyleafSourceFolder);
                LFlyleafRecordSave(lRecord);
                return lRecord.LFlyleafAssemblyFolder;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool LFlyleafFolderValidate(string? lAssemblyFolder)
    {
        if (string.IsNullOrWhiteSpace(lAssemblyFolder)
            || !File.Exists(Path.Combine(lAssemblyFolder, "FlyleafLib.dll"))
            || !File.Exists(Path.Combine(lAssemblyFolder, "FlyleafLib.deps.json")))
        {
            return false;
        }

        string lFolder = Path.GetFullPath(lAssemblyFolder);
        string lRuntimeFolder = Path.GetFullPath(Path.Combine(LFlyleafRootRead(), LFlyleafRuntimeName));
        return string.Equals(lFolder, lRuntimeFolder, StringComparison.OrdinalIgnoreCase)
            && !lFolder.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase)
            && !lFolder.EndsWith($"{Path.DirectorySeparatorChar}ref", StringComparison.OrdinalIgnoreCase)
            && !lFolder.EndsWith($"{Path.DirectorySeparatorChar}refint", StringComparison.OrdinalIgnoreCase);
    }

    private static string LFlyleafRecordFind() =>
        Path.Combine(LFlyleafRootRead(), LFlyleafRecordName);

    internal static void LFlyleafRecordSave(LFlyleafDocument lRecord)
    {
        File.WriteAllText(
            LFlyleafRecordFind(),
            JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true }));
    }
}
