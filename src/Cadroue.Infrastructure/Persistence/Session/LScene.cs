using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static partial class LScene
{
    public const int LSceneVersionCurrent = 1;

    private const string LSceneFileName = "LScenePresets.json";
    private const string LSceneStateName = "session.json";
    private static LVaultOutcome lSceneCatalogueOutcome;
    private static bool lSceneStateReadable = true;
    private static readonly List<LSceneDocument> lSceneRecords = LSceneLoad();
    private static string? lSceneRoot;

    public static LSceneDocument LSceneCurrent { get; private set; } = new();

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
        LSceneCurrent = new LSceneDocument();
        LSceneActiveName = string.Empty;
    }

    internal static string? LSceneRootRead() => lSceneRoot;

    public static LSceneDocument LSceneStateLoad()
    {
        LVaultResult<LSceneDocument> lSceneResult =
            LVault.LVaultRead<LSceneDocument>(Path.Combine(LSceneFolderRead(), LSceneStateName));
        lSceneStateReadable = lSceneResult.LVaultOutcome != LVaultOutcome.LVaultUnreadable;
        return LSceneNormalize(lSceneResult.LVaultValue);
    }

    public static bool LSceneStateSave(LSceneDocument lScene)
    {
        if (!lSceneStateReadable)
        {
            return false;
        }

        return LVault.LVaultSave(Path.Combine(LSceneFolderRead(), LSceneStateName), lScene);
    }

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
        lSceneCatalogueOutcome = lSceneDisk.LVaultOutcome;
        if (lSceneDisk.LVaultOutcome != LVaultOutcome.LVaultUnreadable)
        {
            lSceneRecords.Clear();
            lSceneRecords.AddRange(LSceneCatalogueNormalize(lSceneDisk.LVaultValue));
        }
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

    public static string LSceneFileResolve(string lSceneName, string lSceneDefault)
    {
        char[] lInvalid = Path.GetInvalidFileNameChars();
        string lClean = new string(lSceneName.Trim()
            .Select(lCharacter => lInvalid.Contains(lCharacter) ? '_' : lCharacter)
            .ToArray());
        return string.IsNullOrWhiteSpace(lClean) ? $"{lSceneDefault}.json" : $"{lClean}.json";
    }

    public static void LSceneFileSave(LSceneDocument lScene, string lScenePath)
    {
        string lSceneJson = JsonSerializer.Serialize(lScene, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lScenePath, lSceneJson);
    }

    public static string LSceneStemResolve(string lSceneName, string lScenePath)
    {
        string lSceneTrimmed = lSceneName.Trim();
        return lSceneTrimmed.Length > 0 ? lSceneTrimmed : Path.GetFileNameWithoutExtension(lScenePath).Trim();
    }

    public static LSceneDocument? LSceneFileLoad(string lScenePath)
    {
        string lSceneJson = File.ReadAllText(lScenePath);
        return LSceneNormalize(JsonSerializer.Deserialize<LSceneDocument>(lSceneJson));
    }

    public static bool LSceneMatch(LSceneDocument lSceneLeft, LSceneDocument lSceneRight) =>
        string.Equals(LSceneCanonicalRead(lSceneLeft), LSceneCanonicalRead(lSceneRight), StringComparison.Ordinal);

    private static string LSceneCanonicalRead(LSceneDocument lScene)
    {
        var lSceneCanonical = new LSceneDocument
        {
            LSceneLayoutKeys = lScene.LSceneLayoutKeys,
            LSceneTabNames = lScene.LSceneTabNames,
            LSceneTabExports = lScene.LSceneTabExports,
            LSceneTabLayouts = lScene.LSceneTabLayouts
                .Select(lSceneTab =>
                {
                    LSceneTabEntry lSceneTabCanonical = lSceneTab.LSceneTabClone();
                    lSceneTabCanonical.LScenePanelWidths =
                        LSceneWidthsRead(lSceneTabCanonical.LScenePanelWidths);
                    return lSceneTabCanonical;
                })
                .ToList(),
            LSceneTabRelays = lScene.LSceneTabRelays
        };

        return JsonSerializer.Serialize(lSceneCanonical);
    }

    private static List<double> LSceneWidthsRead(IReadOnlyList<double> lScenePanelWidths)
    {
        double lSceneWidthTotal = lScenePanelWidths.Sum(lSceneWidth => Math.Max(0, lSceneWidth));
        if (lSceneWidthTotal <= 0)
        {
            return lScenePanelWidths.ToList();
        }

        return lScenePanelWidths
            .Select(lSceneWidth => Math.Round(Math.Max(0, lSceneWidth) / lSceneWidthTotal, 12))
            .ToList();
    }

    private static List<LSceneDocument> LSceneLoad()
    {
        LVaultResult<List<LSceneDocument>> lSceneResult =
            LVault.LVaultRead<List<LSceneDocument>>(LScenePathCreate());
        lSceneCatalogueOutcome = lSceneResult.LVaultOutcome;
        return LSceneCatalogueNormalize(lSceneResult.LVaultValue);
    }

    private static bool LSceneChange(Func<List<LSceneDocument>, bool> lSceneApply)
    {
        string lScenePath = LScenePathCreate();
        using LLatchScope lSceneLatch = LLatch.LLatchClaim(lScenePath);
        LVaultResult<List<LSceneDocument>> lSceneDisk = LVault.LVaultRead<List<LSceneDocument>>(lScenePath);
        if (lSceneDisk.LVaultOutcome == LVaultOutcome.LVaultUnreadable)
        {
            lSceneCatalogueOutcome = LVaultOutcome.LVaultUnreadable;
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
        lSceneCatalogueOutcome = LVaultOutcome.LVaultLoaded;
    }

    private static string LScenePathCreate() =>
        Path.Combine(LSceneFolderRead(), LSceneFileName);

    private static string LSceneFolderRead() => LSceneRootRead() ?? LDepot.LDepotHomeRead();
}
