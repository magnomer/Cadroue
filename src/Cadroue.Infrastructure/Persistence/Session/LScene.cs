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
    private const string LSceneStateName = "session.json";
    private static LVaultOutcome lSceneCatalogueOutcome;
    private static bool lSceneStateReadable = true;
    private static readonly List<LSceneRecord> lSceneRecords = LSceneLoad();
    private static string? lSceneRoot;

    public static LSceneRecord LSceneCurrent { get; private set; } = new();

    public static string LSceneActiveName { get; private set; } = string.Empty;

    public static void LSceneCurrentLoad()
    {
        LSceneCurrent = LSceneStateLoad();
        LSceneActiveName = LSceneCurrent.LSceneName;
    }

    public static void LSceneActiveSet(string lSceneActiveName) =>
        LSceneActiveName = lSceneActiveName ?? string.Empty;

    internal static void LSceneRootSet(string? lSceneRootPath)
    {
        lSceneRoot = lSceneRootPath;
        lSceneRecords.Clear();
        lSceneRecords.AddRange(LSceneLoad());
        LSceneCurrent = new LSceneRecord();
        LSceneActiveName = string.Empty;
    }

    public static LSceneRecord LSceneStateLoad()
    {
        LVaultResult<LSceneRecord> lSceneResult =
            LVault.LVaultRead<LSceneRecord>(Path.Combine(LSceneFolderRead(), LSceneStateName));
        lSceneStateReadable = lSceneResult.LVaultOutcome != LVaultOutcome.LVaultUnreadable;
        return lSceneResult.LVaultValue ?? new LSceneRecord();
    }

    public static bool LSceneStateSave(LSceneRecord lScene)
    {
        if (!lSceneStateReadable)
        {
            return false;
        }

        return LVault.LVaultSave(Path.Combine(LSceneFolderRead(), LSceneStateName), lScene);
    }

    public static IReadOnlyList<string> LSceneNames =>
        lSceneRecords.Select(lSceneRecord => lSceneRecord.LSceneName).ToList();

    public static LSceneRecord? LSceneRead(string lSceneName) =>
        lSceneRecords.FirstOrDefault(lSceneRecord =>
            string.Equals(lSceneRecord.LSceneName, lSceneName, StringComparison.OrdinalIgnoreCase));

    public static void LSceneCatalogueLoad()
    {
        string lScenePath = LScenePathCreate();
        using LLatchScope lSceneLatch = LLatch.LLatchClaim(lScenePath);
        LVaultResult<List<LSceneRecord>> lSceneDisk = LVault.LVaultRead<List<LSceneRecord>>(lScenePath);
        lSceneCatalogueOutcome = lSceneDisk.LVaultOutcome;
        if (lSceneDisk.LVaultOutcome != LVaultOutcome.LVaultUnreadable)
        {
            lSceneRecords.Clear();
            lSceneRecords.AddRange(lSceneDisk.LVaultValue ?? new List<LSceneRecord>());
        }
    }

    public static bool LSceneSave(LSceneRecord lScene) =>
        LSceneChange(lSceneNext =>
        {
            int lSceneIndex = lSceneNext.FindIndex(lSceneRecord =>
                string.Equals(lSceneRecord.LSceneName, lScene.LSceneName, StringComparison.OrdinalIgnoreCase));
            if (lSceneIndex >= 0)
            {
                lSceneNext[lSceneIndex] = lScene;
            }
            else
            {
                lSceneNext.Add(lScene);
            }

            return true;
        });

    public static bool LSceneDelete(string lSceneName) =>
        LSceneChange(lSceneNext =>
        {
            int lSceneIndex = lSceneNext.FindIndex(lSceneRecord =>
                string.Equals(lSceneRecord.LSceneName, lSceneName, StringComparison.OrdinalIgnoreCase));
            if (lSceneIndex < 0)
            {
                return false;
            }

            lSceneNext.RemoveAt(lSceneIndex);
            return true;
        });

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
        LVaultResult<List<LSceneRecord>> lSceneResult =
            LVault.LVaultRead<List<LSceneRecord>>(LScenePathCreate());
        lSceneCatalogueOutcome = lSceneResult.LVaultOutcome;
        return lSceneResult.LVaultValue ?? new List<LSceneRecord>();
    }

    private static bool LSceneChange(Func<List<LSceneRecord>, bool> lSceneApply)
    {
        string lScenePath = LScenePathCreate();
        using LLatchScope lSceneLatch = LLatch.LLatchClaim(lScenePath);
        LVaultResult<List<LSceneRecord>> lSceneDisk = LVault.LVaultRead<List<LSceneRecord>>(lScenePath);
        if (lSceneDisk.LVaultOutcome == LVaultOutcome.LVaultUnreadable)
        {
            lSceneCatalogueOutcome = LVaultOutcome.LVaultUnreadable;
            return false;
        }

        List<LSceneRecord> lSceneNext = lSceneDisk.LVaultValue ?? new List<LSceneRecord>();
        if (!lSceneApply(lSceneNext))
        {
            LSceneMirrorSet(lSceneNext);
            return false;
        }

        if (!LVault.LVaultSave(lScenePath, lSceneNext))
        {
            return false;
        }

        LSceneMirrorSet(lSceneNext);
        return true;
    }

    private static void LSceneMirrorSet(List<LSceneRecord> lSceneCatalogue)
    {
        lSceneRecords.Clear();
        lSceneRecords.AddRange(lSceneCatalogue);
        lSceneCatalogueOutcome = LVaultOutcome.LVaultLoaded;
    }

    private static string LScenePathCreate() =>
        Path.Combine(LSceneFolderRead(), LSceneFileName);

    private static string LSceneFolderRead() =>
        lSceneRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LSceneFolderName);
}
