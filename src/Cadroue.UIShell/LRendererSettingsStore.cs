using System;
using System.IO;
using System.Text.Json;

namespace Cadroue.UIShell;

public static class LRendererSettingsStore
{
    private const string LRendererSettingsFolderName = "Cadroue";
    private const string LRendererSettingsFileName = "LRendererSettings.json";

    public static LRendererSettings LRendererSettingsLoad()
    {
        string lRendererSettingsPath = LRendererSettingsPathCreate();
        if (!File.Exists(lRendererSettingsPath))
        {
            return LRendererSettings.LRendererSettingsDefaultCreate();
        }

        try
        {
            string lRendererSettingsJson = File.ReadAllText(lRendererSettingsPath);
            return JsonSerializer.Deserialize<LRendererSettings>(lRendererSettingsJson)
                ?? LRendererSettings.LRendererSettingsDefaultCreate();
        }
        catch
        {
            return LRendererSettings.LRendererSettingsDefaultCreate();
        }
    }

    public static void LRendererSettingsSave(LRendererSettings lRendererSettings)
    {
        string lRendererSettingsPath = LRendererSettingsPathCreate();
        string? lRendererSettingsFolder = Path.GetDirectoryName(lRendererSettingsPath);
        if (!string.IsNullOrWhiteSpace(lRendererSettingsFolder))
        {
            Directory.CreateDirectory(lRendererSettingsFolder);
        }

        string lRendererSettingsJson = JsonSerializer.Serialize(
            lRendererSettings,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lRendererSettingsPath, lRendererSettingsJson);
    }

    private static string LRendererSettingsPathCreate()
    {
        string lRendererApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lRendererApplicationDataFolder, LRendererSettingsFolderName, LRendererSettingsFileName);
    }
}
