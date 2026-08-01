using System.IO;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LPresetStore
{
    private const string LPresetFolderName = "Cadroue";
    private const string LPresetFileName = "LExportSpecificPresets.json";

    public static IReadOnlyList<LPresetRecord>? LPresetLoad()
    {
        string lPresetPath = LPresetPathCreate();
        if (!File.Exists(lPresetPath))
        {
            return null;
        }

        try
        {
            string lPresetJson = File.ReadAllText(lPresetPath);
            return JsonSerializer.Deserialize<List<LPresetRecord>>(lPresetJson);
        }
        catch
        {
            return null;
        }
    }

    public static void LPresetSave(IReadOnlyList<LPresetRecord> lRecords)
    {
        string lPresetPath = LPresetPathCreate();
        string? lPresetFolder = Path.GetDirectoryName(lPresetPath);
        if (!string.IsNullOrWhiteSpace(lPresetFolder))
        {
            Directory.CreateDirectory(lPresetFolder);
        }

        string lPresetJson = JsonSerializer.Serialize(lRecords, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPresetPath, lPresetJson);
    }

    public static void LPresetFileSave(LPresetRecord lRecord, string lPresetFilePath)
    {
        string lPresetJson = JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPresetFilePath, lPresetJson);
    }

    public static LPresetRecord? LPresetFileLoad(string lPresetFilePath)
    {
        string lPresetJson = File.ReadAllText(lPresetFilePath);
        return JsonSerializer.Deserialize<LPresetRecord>(lPresetJson);
    }

    private static string LPresetPathCreate()
    {
        string lAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lAppData, LPresetFolderName, LPresetFileName);
    }
}
