using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LBindingStore
{
    private const string LBindingFolderName = "Cadroue";
    private const string LBindingFileName = "keybindings.json";

    public static List<LBindingRecord> LBindingLoad()
    {
        string lBindingPath = LBindingPathCreate();
        if (!File.Exists(lBindingPath))
        {
            return new List<LBindingRecord>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<LBindingRecord>>(File.ReadAllText(lBindingPath))
                ?? new List<LBindingRecord>();
        }
        catch
        {
            return new List<LBindingRecord>();
        }
    }

    public static void LBindingSave(List<LBindingRecord> lBindingRecords)
    {
        try
        {
            string lBindingPath = LBindingPathCreate();
            string? lBindingFolder = Path.GetDirectoryName(lBindingPath);
            if (!string.IsNullOrWhiteSpace(lBindingFolder))
            {
                Directory.CreateDirectory(lBindingFolder);
            }

            File.WriteAllText(
                lBindingPath,
                JsonSerializer.Serialize(lBindingRecords, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
        }
    }

    private static string LBindingPathCreate()
    {
        string lBindingApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lBindingApplicationDataFolder, LBindingFolderName, LBindingFileName);
    }
}
