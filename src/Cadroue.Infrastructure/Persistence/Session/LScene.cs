using System.IO;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LScene
{
    private const string LSceneStateName = "session.json";
    private static bool lSceneStateReadable = true;
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
        LSceneCatalogue.LSceneCatalogueReset();
        LSceneCurrent = new LSceneDocument();
        LSceneActiveName = string.Empty;
    }

    internal static string? LSceneRootRead() => lSceneRoot;

    public static LSceneDocument LSceneStateLoad()
    {
        LVaultResult<LSceneDocument> lSceneResult =
            LVault.LVaultRead<LSceneDocument>(Path.Combine(LSceneFolderRead(), LSceneStateName));
        lSceneStateReadable = lSceneResult.LVaultOutcome != LVaultOutcome.LVaultUnreadable;
        return LSceneCanonical.LSceneNormalize(lSceneResult.LVaultValue);
    }

    public static bool LSceneStateSave(LSceneDocument lScene)
    {
        if (!lSceneStateReadable)
        {
            return false;
        }

        return LVault.LVaultSave(Path.Combine(LSceneFolderRead(), LSceneStateName), lScene);
    }

    internal static string LSceneFolderRead() => LSceneRootRead() ?? LDepot.LDepotHomeRead();
}
