using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LScene
{
    private const string LSceneFolderName = "Cadroue";
    private const string LSceneFileName = "LScenePresets.json";
    private const string LSceneStateFileName = "session.json";
    private static readonly List<LSceneRecord> lSceneRecords = LSceneLoad();

    public static LSceneRecord LSceneStateLoad()
    {
        string lScenePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LSceneFolderName,
            LSceneStateFileName);
        if (!File.Exists(lScenePath))
        {
            return new LSceneRecord();
        }

        try
        {
            return JsonSerializer.Deserialize<LSceneRecord>(File.ReadAllText(lScenePath)) ?? new LSceneRecord();
        }
        catch
        {
            return new LSceneRecord();
        }
    }

    public static void LSceneStateSave(LSceneRecord lScene)
    {
        try
        {
            string lScenePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                LSceneFolderName,
                LSceneStateFileName);
            string? lSceneFolder = Path.GetDirectoryName(lScenePath);
            if (!string.IsNullOrWhiteSpace(lSceneFolder))
            {
                Directory.CreateDirectory(lSceneFolder);
            }

            File.WriteAllText(
                lScenePath,
                JsonSerializer.Serialize(lScene, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
        }
    }

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
        int lSceneIndexHeld = lScene.LSceneTabIndex;
        List<List<double>> lSceneWidthsHeld = lScene.LSceneTabLayouts
            .Select(lSceneTabLayout => lSceneTabLayout.LScenePanelWidths)
            .ToList();
        lScene.LSceneName = string.Empty;
        lScene.LSceneTabIndex = 0;
        foreach (LSceneTabRecord lSceneTabLayout in lScene.LSceneTabLayouts)
        {
            lSceneTabLayout.LScenePanelWidths = new List<double>();
        }

        string lSceneJson = JsonSerializer.Serialize(lScene);
        lScene.LSceneName = lSceneNameHeld;
        lScene.LSceneTabIndex = lSceneIndexHeld;
        for (int lSceneIndex = 0; lSceneIndex < lScene.LSceneTabLayouts.Count; lSceneIndex++)
        {
            lScene.LSceneTabLayouts[lSceneIndex].LScenePanelWidths = lSceneWidthsHeld[lSceneIndex];
        }

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
