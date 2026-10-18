using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The scene strip's engine side over the scene store. A scene is dirty when it is named and stored and the window
// no longer matches it.
public sealed class LConsoleSceneOutlet : LConsoleScenePort
{
    private readonly LSceneStore _lConsoleSceneStore;

    public LConsoleSceneOutlet(LSceneStore lStore)
    {
        ArgumentNullException.ThrowIfNull(lStore);
        _lConsoleSceneStore = lStore;
    }

    public LConsoleSceneAnswer LConsoleSceneStart(LConsoleWindow? lWindow) =>
        LConsoleSceneRead(_lConsoleSceneStore.LSceneStoreActive(), lWindow);

    public LConsoleSceneAnswer LConsoleSceneRebuild(string lName, LConsoleWindow? lWindow)
    {
        _lConsoleSceneStore.LSceneStoreCatalogue();
        return LConsoleSceneRead(lName, lWindow) with { LConsoleSceneNames = _lConsoleSceneStore.LSceneStoreNames() };
    }

    public LConsoleSceneAnswer LConsoleSceneRead(string lName, LConsoleWindow? lWindow)
    {
        LSceneDocument? lStored = _lConsoleSceneStore.LSceneStoreEntry(lName);
        bool lDirty = lName.Length > 0
            && lStored is not null
            && lWindow is not null
            && !_lConsoleSceneStore.LSceneStoreEquality(lStored, lWindow.LConsoleWindowSnapshot(lName));
        return new LConsoleSceneAnswer(lName, lStored is not null, lDirty, null);
    }

    public LConsoleSceneAnswer LConsoleSceneLoad(
        string lName, string lTarget, LConsoleWindow? lWindow, Func<bool> lLoadConfirm, Func<bool> lCloseConfirm)
    {
        ArgumentNullException.ThrowIfNull(lLoadConfirm);
        ArgumentNullException.ThrowIfNull(lCloseConfirm);
        if (_lConsoleSceneStore.LSceneStoreEntry(lTarget) is not { } lScene || !lLoadConfirm() || lWindow is null)
        {
            return LConsoleSceneRead(lName, lWindow);
        }

        bool lConfirmed = lCloseConfirm();
        if (!lConfirmed)
        {
            LTraceEngine.LTraceInfoSeam("Close declined: a worklist is still working");
        }

        if (!lWindow.LConsoleWindowRestorer(lScene, lConfirmed, lWindow.LConsoleWindowClosure))
        {
            return LConsoleSceneRead(lName, lWindow);
        }

        _lConsoleSceneStore.LSceneStoreActivation(lTarget);
        return LConsoleSceneRead(lTarget, lWindow);
    }

    public LConsoleSceneAnswer? LConsoleSceneDelete(string lName, string lTarget, LConsoleWindow? lWindow)
    {
        if (!_lConsoleSceneStore.LSceneStoreRemoval(lTarget))
        {
            return null;
        }

        string lKept = lName;
        if (string.Equals(lName, lTarget, StringComparison.OrdinalIgnoreCase))
        {
            lKept = string.Empty;
            _lConsoleSceneStore.LSceneStoreActivation(string.Empty);
        }

        LConsoleSceneAnswer lAnswer = LConsoleSceneRebuild(lKept, lWindow);
        LTraceEngine.LTraceInfoSeam($"Scene deleted '{lTarget}'");
        return lAnswer;
    }

    public LConsoleSceneAnswer? LConsoleSceneSave(string lName, LConsoleWindow? lWindow)
    {
        if (lWindow is null)
        {
            return null;
        }

        _lConsoleSceneStore.LSceneStoreKeeper(lWindow.LConsoleWindowSnapshot(lName));
        _lConsoleSceneStore.LSceneStoreCatalogue();
        _lConsoleSceneStore.LSceneStoreActivation(lName);
        LConsoleSceneAnswer lAnswer =
            LConsoleSceneRead(lName, lWindow) with { LConsoleSceneNames = _lConsoleSceneStore.LSceneStoreNames() };
        LTraceEngine.LTraceInfoSeam($"Scene saved '{lName}'");
        return lAnswer;
    }

    public string LConsoleSceneResolve(string lName, string lFallback, LConsoleWindow? lWindow) =>
        _lConsoleSceneStore.LSceneStoreFile(
            LConsoleSceneFind(lName, lFallback, lWindow)?.LSceneName ?? string.Empty, lFallback);

    public string? LConsoleScenePersist(string lName, string lPath, string lFallback, LConsoleWindow? lWindow)
    {
        if (LConsoleSceneFind(lName, lFallback, lWindow) is not { } lScene)
        {
            return null;
        }

        try
        {
            _lConsoleSceneStore.LSceneStoreExport(lScene, lPath);
        }
        catch (Exception lError)
        {
            return lError.Message;
        }

        return null;
    }

    public (LConsoleSceneAnswer? LConsoleSceneImported, string? LConsoleSceneFault) LConsoleSceneImport(
        string lName, string lPath, string lFallback, LConsoleWindow? lWindow)
    {
        LSceneDocument? lScene;
        try
        {
            lScene = _lConsoleSceneStore.LSceneStoreSource(lPath);
        }
        catch (Exception lError)
        {
            return (null, lError.Message);
        }

        if (lScene is null)
        {
            return (null, null);
        }

        string lStem = _lConsoleSceneStore.LSceneStoreStem(lScene.LSceneName, lPath);
        lScene.LSceneName = LConsoleSceneCreate(
            lStem.Length > 0 ? lStem : lFallback, _lConsoleSceneStore.LSceneStoreNames());
        _lConsoleSceneStore.LSceneStoreKeeper(lScene);
        LConsoleSceneAnswer lAnswer = LConsoleSceneRebuild(lName, lWindow);
        _lConsoleSceneStore.LSceneStoreLoading($"Scene imported '{lScene.LSceneName}'");
        return (lAnswer, null);
    }

    // The base name while no name matches it ignoring case, else the first free "base n" from 2 up.
    public static string LConsoleSceneCreate(string lBaseName, IReadOnlyList<string> lNames)
    {
        ArgumentNullException.ThrowIfNull(lNames);
        if (!lNames.Any(lName => string.Equals(lName, lBaseName, StringComparison.OrdinalIgnoreCase)))
        {
            return lBaseName;
        }

        for (int lIndex = 2; ; lIndex++)
        {
            string lCandidate = $"{lBaseName} {lIndex}";
            if (!lNames.Any(lName => string.Equals(lName, lCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return lCandidate;
            }
        }
    }

    // The stored scene for a given name, else the window's scene under the name or the fallback.
    private LSceneDocument? LConsoleSceneFind(string lName, string lFallback, LConsoleWindow? lWindow)
    {
        LSceneDocument? lScene = lName.Length > 0 ? _lConsoleSceneStore.LSceneStoreEntry(lName) : null;
        return lScene ?? lWindow?.LConsoleWindowSnapshot(lName.Length > 0 ? lName : lFallback);
    }
}
