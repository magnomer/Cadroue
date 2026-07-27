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
        string lRendererSettingsPath = LRendererPathCreate();
        if (!File.Exists(lRendererSettingsPath))
        {
            return LRendererSettings.LRendererDefaultCreate();
        }

        try
        {
            string lRendererSettingsJson = File.ReadAllText(lRendererSettingsPath);
            return JsonSerializer.Deserialize<LRendererSettings>(lRendererSettingsJson)
                ?? LRendererSettings.LRendererDefaultCreate();
        }
        catch
        {
            return LRendererSettings.LRendererDefaultCreate();
        }
    }

    public static void LRendererSettingsSave(LRendererSettings lRendererSettings)
    {
        try
        {
            string lRendererSettingsPath = LRendererPathCreate();
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
        catch
        {
        }
    }

    private static string LRendererPathCreate()
    {
        string lRendererApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lRendererApplicationDataFolder, LRendererSettingsFolderName, LRendererSettingsFileName);
    }
}
