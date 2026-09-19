using Cadroue.Core;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class LConsoleScene
{
    private string? lConsoleReloadName;
    private string lConsoleSceneName = string.Empty;
    private LWindow? lWindow;
    private LStrip? lStrip;

    public string? LConsoleReloadName => lConsoleReloadName;

    public string LConsoleSceneName => lConsoleSceneName;

    public event Action<IReadOnlyList<string>>? LConsoleNamesApply;

    public event Action<string?, string, bool>? LConsoleSceneApply;

    public event Action<bool>? LConsoleMarkApply;

    public event Action? LConsoleFocusClear;

    public event Action<string, string>? LConsoleNoticeShow;

    public event Action<string, string>? LConsoleWarningShow;

    public void LConsoleWindowAttach(LWindow lWindowOwner, LStrip lStripOwner)
    {
        lWindow = lWindowOwner;
        lStrip = lStripOwner;
    }

    public void LConsoleReloadSet(string? lReloadName) => lConsoleReloadName = lReloadName;

    public string? LConsoleReloadRead()
    {
        string? lReloadName = lConsoleReloadName;
        lConsoleReloadName = null;
        return lReloadName;
    }

    public void LConsoleSceneSet(string lSceneName) => lConsoleSceneName = lSceneName;

    public bool LConsoleSceneCheck(string lSceneName) =>
        string.Equals(lConsoleSceneName, lSceneName, StringComparison.OrdinalIgnoreCase);

    public void LConsoleSceneStart()
    {
        lConsoleSceneName = LScene.LSceneActiveName;
        LConsoleSceneUpdate();
    }

    public void LConsoleSceneRebuild()
    {
        LScene.LSceneCatalogueLoad();
        LConsoleNamesApply?.Invoke(LScene.LSceneNames);
        LConsoleSceneUpdate();
    }

    public void LConsoleSceneUpdate() =>
        LConsoleSceneApply?.Invoke(
            LScene.LSceneRead(lConsoleSceneName) is not null ? lConsoleSceneName : null,
            lConsoleSceneName,
            LConsoleDirtyCheck());

    public void LConsoleMarkUpdate() => LConsoleMarkApply?.Invoke(LConsoleDirtyCheck());

    public void LConsoleTickHandle(bool lFocusWithin)
    {
        if (lConsoleSceneName.Length > 0 && !lFocusWithin)
        {
            LConsoleMarkUpdate();
        }
    }

    public bool LConsoleDirtyCheck()
    {
        if (lConsoleSceneName.Length == 0
            || LScene.LSceneRead(lConsoleSceneName) is not { } lSceneStored
            || lWindow is null)
        {
            return false;
        }

        return !LScene.LSceneMatch(lSceneStored, lWindow.LWindowSceneRead(lConsoleSceneName));
    }

    public void LConsoleRowHandle(string? lSceneName)
    {
        if (lSceneName is not null)
        {
            lConsoleReloadName = lSceneName;
        }
    }

    public void LConsoleSelectHandle(object? lSelected, bool lDropOpen)
    {
        if (lSelected is not string lSceneName || LConsoleSceneCheck(lSceneName))
        {
            return;
        }

        if (lDropOpen)
        {
            lConsoleReloadName = lSceneName;
            return;
        }

        lConsoleReloadName = null;
        LConsoleSceneLoad(lSceneName);
    }

    public void LConsoleCloseHandle()
    {
        if (LConsoleReloadRead() is { } lSceneName)
        {
            LConsoleSceneLoad(lSceneName);
            return;
        }

        LConsoleSceneUpdate();
    }

    public void LConsoleSceneLoad(string lSceneName)
    {
        if (LScene.LSceneRead(lSceneName) is not { } lScene)
        {
            LConsoleSceneUpdate();
            return;
        }

        var lAsk = new LAsk(
            LLocalization.LLocalizationFormat("Console.Scene.LoadConfirm", lSceneName),
            LLocalization.LLocalizationTextRead("Terms.Load"))
        {
            LAskTitle = LLocalization.LLocalizationTextRead("Console.Scene.LoadTitle")
        };
        LAskNotice.LAskPublish(lAsk, lAnswer => LConsoleSceneRun(lAnswer, lSceneName, lScene));
    }

    private void LConsoleSceneRun(bool lAnswer, string lSceneName, LSceneRecord lScene)
    {
        if (lAnswer && lWindow is { } lWindowOwner && lStrip is { } lStripOwner
            && lWindowOwner.LWindowSceneApply(lScene, lStripOwner.LStripCloseConfirm(), lStripOwner.LStripAllClose))
        {
            LConsoleSceneSelect(lSceneName);
            return;
        }

        LConsoleSceneUpdate();
    }

    private void LConsoleSceneSelect(string lSceneName)
    {
        lConsoleSceneName = lSceneName;
        LScene.LSceneActiveSet(lSceneName);
        LConsoleSceneUpdate();
    }

    public bool LConsoleDeleteHandle(string? lSceneName)
    {
        if (lSceneName is null)
        {
            return false;
        }

        lConsoleReloadName = null;
        if (!LScene.LSceneDelete(lSceneName))
        {
            return true;
        }

        if (LConsoleSceneCheck(lSceneName))
        {
            lConsoleSceneName = string.Empty;
            LScene.LSceneActiveSet(string.Empty);
        }

        LConsoleSceneRebuild();
        LTraceLog.LTraceInfoRecord($"Scene deleted '{lSceneName}'");
        return true;
    }

    public void LConsoleSceneSave(string? lText)
    {
        string lSceneName = (lText ?? string.Empty).Trim();
        if (lSceneName.Length == 0)
        {
            LConsoleNoticeShow?.Invoke(
                LLocalization.LLocalizationTextRead("Console.Scene.SaveTitle"),
                LLocalization.LLocalizationTextRead("Console.Scene.NameRequired"));
            return;
        }

        if (lWindow is not { } lWindowOwner)
        {
            return;
        }

        LScene.LSceneSave(lWindowOwner.LWindowSceneRead(lSceneName));
        LConsoleSceneRebuild();
        LConsoleSceneSelect(lSceneName);
        LTraceLog.LTraceInfoRecord($"Scene saved '{lSceneName}'");
    }

    public void LConsolePressHandle(bool lDropOpen, bool lFocusWithin, bool lInsideCombo)
    {
        if (lDropOpen || !lFocusWithin || lInsideCombo)
        {
            return;
        }

        LConsoleFocusClear?.Invoke();
    }

    public void LConsoleDeactivateHandle(bool lFocusWithin)
    {
        if (lFocusWithin)
        {
            LConsoleFocusClear?.Invoke();
        }
    }

    private LSceneRecord? LConsoleExportResolve(string? lText)
    {
        string lSceneName = (lText ?? string.Empty).Trim();
        LSceneRecord? lScene = lSceneName.Length > 0 ? LScene.LSceneRead(lSceneName) : null;
        if (lScene is not null)
        {
            return lScene;
        }

        return lWindow?.LWindowSceneRead(
            lSceneName.Length > 0 ? lSceneName : LLocalization.LLocalizationTextRead("Console.Scene.DefaultName"));
    }

    public string LConsoleFileResolve(string? lText) =>
        LScene.LSceneFileResolve(
            LConsoleExportResolve(lText)?.LSceneName ?? string.Empty,
            LLocalization.LLocalizationTextRead("Console.Scene.DefaultName"));

    public void LConsoleExportCommit(bool? lConfirmed, string lPath, string? lText)
    {
        if (lConfirmed != true || LConsoleExportResolve(lText) is not { } lScene)
        {
            return;
        }

        try
        {
            LScene.LSceneFileSave(lScene, lPath);
        }
        catch (Exception lError)
        {
            LConsoleErrorRaise("Console.Scene.Error.Write", lError.Message, "Console.Scene.Dialog.Export");
        }
    }

    public void LConsoleImportCommit(bool? lConfirmed, string lPath)
    {
        if (lConfirmed != true)
        {
            return;
        }

        LSceneRecord? lScene;
        try
        {
            lScene = LScene.LSceneFileLoad(lPath);
        }
        catch (Exception lError)
        {
            LConsoleErrorRaise("Console.Scene.Error.Read", lError.Message, "Console.Scene.Dialog.Import");
            return;
        }

        if (lScene is null)
        {
            LConsoleErrorRaise("Console.Scene.Error.Invalid", string.Empty, "Console.Scene.Dialog.Import");
            return;
        }

        lScene.LSceneName = LConsoleNameCreate(LConsoleStemResolve(lScene.LSceneName, lPath), LScene.LSceneNames);
        LScene.LSceneSave(lScene);
        LConsoleSceneRebuild();
        LTraceLog.LTraceLoadingRecord($"Scene imported '{lScene.LSceneName}'");
    }

    public static string LConsoleStemResolve(string lSceneName, string lPath)
    {
        string lName = LScene.LSceneStemResolve(lSceneName, lPath);
        return lName.Length > 0 ? lName : LLocalization.LLocalizationTextRead("Console.Scene.ImportedName");
    }

    public static string LConsoleNameCreate(string lBaseName, IReadOnlyList<string> lNames)
    {
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

    private void LConsoleErrorRaise(string lMessageKey, string lDetail, string lTitleKey) =>
        LConsoleWarningShow?.Invoke(
            LLocalization.LLocalizationTextRead(lTitleKey),
            lDetail.Length > 0
                ? LLocalization.LLocalizationFormat(lMessageKey, lDetail)
                : LLocalization.LLocalizationTextRead(lMessageKey));
}
