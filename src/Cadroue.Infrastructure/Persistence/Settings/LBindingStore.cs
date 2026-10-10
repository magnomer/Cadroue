using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LBindingStore
{
    private const string LBindingFileName = "keybindings.json";

    public static List<LBindingEntry> LBindingLoad()
    {
        string lBindingPath = LBindingPathCreate();
        if (!File.Exists(lBindingPath))
        {
            return new List<LBindingEntry>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<LBindingEntry>>(File.ReadAllText(lBindingPath))
                ?? new List<LBindingEntry>();
        }
        catch
        {
            return new List<LBindingEntry>();
        }
    }

    public static void LBindingSave(List<LBindingEntry> lBindingRecords)
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

    private static string LBindingPathCreate() => Path.Combine(LDepot.LDepotHomeRead(), LBindingFileName);
}
