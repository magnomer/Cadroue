using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cadroue.UIShell;

public static class LScene
{
    private const string LSceneFolderName = "Cadroue";
    private const string LSceneFileName = "LScenePresets.json";
    private static readonly List<LSceneRecord> lSceneRecords = LSceneLoad();

    public static IReadOnlyList<string> LSceneNames =>
        lSceneRecords.Select(lSceneRecord => lSceneRecord.LSceneName).ToList();

    public static LSceneRecord? LSceneRead(string lSceneName) =>
        lSceneRecords.FirstOrDefault(lSceneRecord =>
            string.Equals(lSceneRecord.LSceneName, lSceneName, StringComparison.OrdinalIgnoreCase));

    public static void LSceneSave(LSceneRecord lScene)
    {
        int lSceneIndex = lSceneRecords.FindIndex(lSceneRecord =>
            string.Equals(lSceneRecord.LSceneName, lScene.LSceneName, StringComparison.OrdinalIgnoreCase));
        if (lSceneIndex >= 0)
        {
            lSceneRecords[lSceneIndex] = lScene;
        }
        else
        {
            lSceneRecords.Add(lScene);
        }

        LScenePersist();
    }

    public static bool LSceneDelete(string lSceneName)
    {
        int lSceneIndex = lSceneRecords.FindIndex(lSceneRecord =>
            string.Equals(lSceneRecord.LSceneName, lSceneName, StringComparison.OrdinalIgnoreCase));
        if (lSceneIndex < 0)
        {
            return false;
        }

        lSceneRecords.RemoveAt(lSceneIndex);
        LScenePersist();
        return true;
    }

    public static void LSceneFileSave(LSceneRecord lScene, string lScenePath)
    {
        string lSceneJson = JsonSerializer.Serialize(lScene, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lScenePath, lSceneJson);
    }

    public static LSceneRecord? LSceneFileLoad(string lScenePath)
    {
        string lSceneJson = File.ReadAllText(lScenePath);
        return JsonSerializer.Deserialize<LSceneRecord>(lSceneJson);
    }

    public static bool LSceneMatch(LSceneRecord lSceneLeft, LSceneRecord lSceneRight) =>
        string.Equals(LSceneCanonicalRead(lSceneLeft), LSceneCanonicalRead(lSceneRight), StringComparison.Ordinal);

    private static string LSceneCanonicalRead(LSceneRecord lScene)
    {
        string lSceneNameHeld = lScene.LSceneName;
        lScene.LSceneName = string.Empty;
        string lSceneJson = JsonSerializer.Serialize(lScene);
        lScene.LSceneName = lSceneNameHeld;
        return lSceneJson;
    }

    private static List<LSceneRecord> LSceneLoad()
    {
        string lScenePath = LScenePathCreate();
        if (!File.Exists(lScenePath))
        {
            return new List<LSceneRecord>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<LSceneRecord>>(File.ReadAllText(lScenePath))
                ?? new List<LSceneRecord>();
        }
        catch
        {
            return new List<LSceneRecord>();
        }
    }

    private static void LScenePersist()
    {
        string lScenePath = LScenePathCreate();
        string? lSceneFolder = Path.GetDirectoryName(lScenePath);
        if (!string.IsNullOrWhiteSpace(lSceneFolder))
        {
            Directory.CreateDirectory(lSceneFolder);
        }

        File.WriteAllText(
            lScenePath,
            JsonSerializer.Serialize(lSceneRecords, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string LScenePathCreate() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LSceneFolderName,
            LSceneFileName);
}
