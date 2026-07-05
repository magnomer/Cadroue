using System;
using System.IO;
using System.Text.Json;

namespace Cadroue.UIShell;

public static class LPreferenceStateStore
{
    private const string LPreferenceFolderName = "Cadroue";
    private const string LPreferenceFileName = "LPreferenceState.json";

    public static LPreferenceState LPreferenceStateLoad()
    {
        string lPreferencePath = LPreferencePathCreate();
        if (!File.Exists(lPreferencePath))
        {
            return LPreferenceState.LPreferenceDefaultCreate();
        }

        try
        {
            string lPreferenceJson = File.ReadAllText(lPreferencePath);
            LPreferenceState lPreferenceState = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)
                ?? LPreferenceState.LPreferenceDefaultCreate();
            lPreferenceState.LPreferenceNormalize();
            return lPreferenceState;
        }
        catch
        {
            return LPreferenceState.LPreferenceDefaultCreate();
        }
    }

    public static void LPreferenceStateSave(LPreferenceState lPreferenceState)
    {
        string lPreferencePath = LPreferencePathCreate();
        string? lPreferenceFolder = Path.GetDirectoryName(lPreferencePath);
        if (!string.IsNullOrWhiteSpace(lPreferenceFolder))
        {
            Directory.CreateDirectory(lPreferenceFolder);
        }

        lPreferenceState.LPreferenceNormalize();
        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPreferencePath, lPreferenceJson);
    }

    private static string LPreferencePathCreate()
    {
        string lPreferenceApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lPreferenceApplicationDataFolder, LPreferenceFolderName, LPreferenceFileName);
    }
}
