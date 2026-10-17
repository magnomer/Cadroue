using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LSceneCatalogue
{
    private const string LSceneFileName = "LScenePresets.json";
    private static readonly List<LSceneDocument> lSceneRecords = LSceneLoad();

    public static IReadOnlyList<string> LSceneNames =>
        lSceneRecords.Select(lSceneRecord => lSceneRecord.LSceneName).ToList();

    public static LSceneDocument? LSceneRead(string lSceneName) =>
        lSceneRecords.FirstOrDefault(lSceneRecord =>
            string.Equals(lSceneRecord.LSceneName, lSceneName, StringComparison.OrdinalIgnoreCase));

    public static void LSceneCatalogueLoad()
    {
        string lScenePath = LScenePathCreate();
        using LLatchScope lSceneLatch = LLatch.LLatchClaim(lScenePath);
        LVaultResult<List<LSceneDocument>> lSceneDisk = LVault.LVaultRead<List<LSceneDocument>>(lScenePath);
        if (lSceneDisk.LVaultOutcome != LVaultOutcome.LVaultUnreadable)
        {
            lSceneRecords.Clear();
            lSceneRecords.AddRange(LSceneCatalogueNormalize(lSceneDisk.LVaultValue));
        }
    }

    internal static void LSceneCatalogueReset()
    {
        lSceneRecords.Clear();
        lSceneRecords.AddRange(LSceneLoad());
    }

    public static bool LSceneSave(LSceneDocument lScene) =>
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

    public static List<LSceneDocument> LSceneCatalogueNormalize(List<LSceneDocument>? lSceneCatalogue)
    {
        var lSceneSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSceneNormalized = new List<LSceneDocument>();
        foreach (LSceneDocument lSceneRecord in lSceneCatalogue ?? new List<LSceneDocument>())
        {
            LSceneDocument lSceneReady = LSceneCanonical.LSceneNormalize(lSceneRecord);
            if (lSceneReady.LSceneName.Length == 0 || !lSceneSeen.Add(lSceneReady.LSceneName))
            {
                continue;
            }

            lSceneNormalized.Add(lSceneReady);
        }

        return lSceneNormalized;
    }

    private static List<LSceneDocument> LSceneLoad()
    {
        LVaultResult<List<LSceneDocument>> lSceneResult =
            LVault.LVaultRead<List<LSceneDocument>>(LScenePathCreate());
        return LSceneCatalogueNormalize(lSceneResult.LVaultValue);
    }

    private static bool LSceneChange(Func<List<LSceneDocument>, bool> lSceneApply)
    {
        string lScenePath = LScenePathCreate();
        using LLatchScope lSceneLatch = LLatch.LLatchClaim(lScenePath);
        LVaultResult<List<LSceneDocument>> lSceneDisk = LVault.LVaultRead<List<LSceneDocument>>(lScenePath);
        if (lSceneDisk.LVaultOutcome == LVaultOutcome.LVaultUnreadable)
        {
            return false;
        }

        List<LSceneDocument> lSceneNext = lSceneDisk.LVaultValue ?? new List<LSceneDocument>();
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

    private static void LSceneMirrorSet(List<LSceneDocument> lSceneCatalogue)
    {
        lSceneRecords.Clear();
        lSceneRecords.AddRange(lSceneCatalogue);
    }

    private static string LScenePathCreate() =>
        Path.Combine(LScene.LSceneFolderRead(), LSceneFileName);
}
