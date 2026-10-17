using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QConsoleScene
{
    private string? qConsoleReloadName;
    private string qConsoleSceneName = string.Empty;
    private QWindowScene? qWindowScene;
    private QStrip? qStrip;

    public string? QConsoleReloadName => qConsoleReloadName;

    public string QConsoleSceneName => qConsoleSceneName;

    public event Action<IReadOnlyList<string>>? QConsoleNamesApplied;

    public event Action<string?, string, bool>? QConsoleSceneApplied;

    public event Action<bool>? QConsoleMarkApplied;

    public event Action? QConsoleFocusCleared;

    public event Action<string, string>? QConsoleNoticeShown;

    public event Action<string, string>? QConsoleWarningShown;

    public void QConsoleWindowAttach(QWindowScene lSceneOwner, QStrip lStripOwner)
    {
        qWindowScene = lSceneOwner;
        qStrip = lStripOwner;
    }

    public void QConsoleReloadSet(string? lReloadName) => qConsoleReloadName = lReloadName;

    public string? QConsoleReloadRead()
    {
        string? lReloadName = qConsoleReloadName;
        qConsoleReloadName = null;
        return lReloadName;
    }

    public void QConsoleSceneSet(string lSceneName) => qConsoleSceneName = lSceneName;

    public bool QConsoleSceneCheck(string lSceneName) =>
        string.Equals(qConsoleSceneName, lSceneName, StringComparison.OrdinalIgnoreCase);

    public void QConsoleSceneStart()
    {
        QConsoleSceneSet(LScene.LSceneActiveName);
        QConsoleSceneUpdate();
    }

    public void QConsoleSceneRebuild()
    {
        LScene.LSceneCatalogueLoad();
        QConsoleNamesApplied?.Invoke(LScene.LSceneNames);
        QConsoleSceneUpdate();
    }

    public void QConsoleSceneUpdate() =>
        QConsoleSceneApplied?.Invoke(QConsoleStoredRead(), qConsoleSceneName, QConsoleDirtyCheck());

    private string? QConsoleStoredRead() =>
        LScene.LSceneRead(qConsoleSceneName) is not null ? qConsoleSceneName : null;

    public void QConsoleMarkUpdate() => QConsoleMarkApplied?.Invoke(QConsoleDirtyCheck());

    public void QConsoleTickHandle(bool lFocusWithin)
    {
        if (qConsoleSceneName.Length > 0 && !lFocusWithin)
        {
            QConsoleMarkUpdate();
        }
    }

    public bool QConsoleDirtyCheck()
    {
        if (qConsoleSceneName.Length == 0
            || LScene.LSceneRead(qConsoleSceneName) is not { } lSceneStored
            || qWindowScene is null)
        {
            return false;
        }

        return !LScene.LSceneMatch(lSceneStored, qWindowScene.QWindowSceneRead(qConsoleSceneName));
    }

    public void QConsoleRowHandle(string? lSceneName)
    {
        if (lSceneName is not null)
        {
            QConsoleReloadSet(lSceneName);
        }
    }

    public void QConsoleSelectHandle(object? lSelected, bool lDropOpen)
    {
        if (lSelected is not string lSceneName || QConsoleSceneCheck(lSceneName))
        {
            return;
        }

        if (lDropOpen)
        {
            QConsoleReloadSet(lSceneName);
            return;
        }

        QConsoleReloadSet(null);
        QConsoleSceneLoad(lSceneName);
    }

    public void QConsoleCloseHandle()
    {
        if (QConsoleReloadRead() is { } lSceneName)
        {
            QConsoleSceneLoad(lSceneName);
            return;
        }

        QConsoleSceneUpdate();
    }

    public void QConsoleSceneLoad(string lSceneName)
    {
        if (LScene.LSceneRead(lSceneName) is not { } lScene)
        {
            QConsoleSceneUpdate();
            return;
        }

        QAskNotice.QAskPublish(
            QConsoleLoadCreate(lSceneName), lAnswer => QConsoleSceneRun(lAnswer, lSceneName, lScene));
    }

    private static QAsk QConsoleLoadCreate(string lSceneName) =>
        new(
            QLocalizationCatalog.QLocalizationFormat("Console.Scene.LoadConfirm", lSceneName),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Load"))
        {
            QAskTitle = QLocalizationCatalog.QLocalizationTextRead("Console.Scene.LoadTitle")
        };

    private void QConsoleSceneRun(bool lAnswer, string lSceneName, LSceneDocument lScene)
    {
        if (lAnswer && qWindowScene is { } lSceneOwner && qStrip is { } lStripOwner
            && lSceneOwner.QWindowSceneApply(
                lScene,
                lStripOwner.QStripClosure.QStripCloseConfirm(),
                lStripOwner.QStripClosure.QStripAllClose))
        {
            QConsoleSceneSelect(lSceneName);
            return;
        }

        QConsoleSceneUpdate();
    }

    private void QConsoleSceneSelect(string lSceneName)
    {
        QConsoleSceneSet(lSceneName);
        LScene.LSceneActiveSet(lSceneName);
        QConsoleSceneUpdate();
    }

    public bool QConsoleDeleteHandle(string? lSceneName)
    {
        if (lSceneName is null)
        {
            return false;
        }

        QConsoleReloadSet(null);
        if (!LScene.LSceneDelete(lSceneName))
        {
            return true;
        }

        if (QConsoleSceneCheck(lSceneName))
        {
            QConsoleSceneSet(string.Empty);
            LScene.LSceneActiveSet(string.Empty);
        }

        QConsoleSceneRebuild();
        LTraceLog.LTraceInfoRecord($"Scene deleted '{lSceneName}'");
        return true;
    }

    public void QConsoleSceneSave(string? lText)
    {
        string lSceneName = QConsoleNameParse(lText);
        if (lSceneName.Length == 0)
        {
            QConsoleNoticeRaise();
            return;
        }

        if (qWindowScene is not { } lSceneOwner)
        {
            return;
        }

        LScene.LSceneSave(lSceneOwner.QWindowSceneRead(lSceneName));
        QConsoleSceneRebuild();
        QConsoleSceneSelect(lSceneName);
        LTraceLog.LTraceInfoRecord($"Scene saved '{lSceneName}'");
    }

    private static string QConsoleNameParse(string? lText) => (lText ?? string.Empty).Trim();

    private void QConsoleNoticeRaise() =>
        QConsoleNoticeShown?.Invoke(
            QLocalizationCatalog.QLocalizationTextRead("Console.Scene.SaveTitle"),
            QLocalizationCatalog.QLocalizationTextRead("Console.Scene.NameRequired"));

    public void QConsolePressHandle(bool lDropOpen, bool lFocusWithin, bool lInsideCombo)
    {
        if (lDropOpen || !lFocusWithin || lInsideCombo)
        {
            return;
        }

        QConsoleFocusCleared?.Invoke();
    }

    public void QConsoleDeactivateHandle(bool lFocusWithin)
    {
        if (lFocusWithin)
        {
            QConsoleFocusCleared?.Invoke();
        }
    }

    private LSceneDocument? QConsoleExportResolve(string? lText)
    {
        string lSceneName = QConsoleNameParse(lText);
        LSceneDocument? lScene = lSceneName.Length > 0 ? LScene.LSceneRead(lSceneName) : null;
        if (lScene is not null)
        {
            return lScene;
        }

        return qWindowScene?.QWindowSceneRead(
            lSceneName.Length > 0 ? lSceneName : QLocalizationCatalog.QLocalizationTextRead(
                "Console.Scene.DefaultName"));
    }

    public string QConsoleFileResolve(string? lText) =>
        LScene.LSceneFileResolve(
            QConsoleExportResolve(lText)?.LSceneName ?? string.Empty,
            QLocalizationCatalog.QLocalizationTextRead("Console.Scene.DefaultName"));

    public void QConsoleExportCommit(bool? lConfirmed, string lPath, string? lText)
    {
        if (lConfirmed != true || QConsoleExportResolve(lText) is not { } lScene)
        {
            return;
        }

        try
        {
            LScene.LSceneFileSave(lScene, lPath);
        }
        catch (Exception lError)
        {
            QConsoleErrorRaise("Console.Scene.Error.Write", lError.Message, "Console.Scene.Dialog.Export");
        }
    }

    public void QConsoleImportCommit(bool? lConfirmed, string lPath)
    {
        if (lConfirmed != true)
        {
            return;
        }

        LSceneDocument? lScene;
        try
        {
            lScene = LScene.LSceneFileLoad(lPath);
        }
        catch (Exception lError)
        {
            QConsoleErrorRaise("Console.Scene.Error.Read", lError.Message, "Console.Scene.Dialog.Import");
            return;
        }

        if (lScene is null)
        {
            QConsoleErrorRaise("Console.Scene.Error.Invalid", string.Empty, "Console.Scene.Dialog.Import");
            return;
        }

        QConsoleNameSet(lScene, lPath);
        LScene.LSceneSave(lScene);
        QConsoleSceneRebuild();
        LTraceLog.LTraceLoadingRecord($"Scene imported '{lScene.LSceneName}'");
    }

    private static void QConsoleNameSet(LSceneDocument lScene, string lPath) =>
        lScene.LSceneName = QConsoleNameCreate(QConsoleStemResolve(lScene.LSceneName, lPath), LScene.LSceneNames);

    public static string QConsoleStemResolve(string lSceneName, string lPath)
    {
        string lName = LScene.LSceneStemResolve(lSceneName, lPath);
        return lName.Length > 0 ? lName : QLocalizationCatalog.QLocalizationTextRead("Console.Scene.ImportedName");
    }

    public static string QConsoleNameCreate(string lBaseName, IReadOnlyList<string> lNames)
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

    private void QConsoleErrorRaise(string lMessageKey, string lDetail, string lTitleKey) =>
        QConsoleWarningShown?.Invoke(
            QLocalizationCatalog.QLocalizationTextRead(lTitleKey), QConsoleErrorFormat(lMessageKey, lDetail));

    private static string QConsoleErrorFormat(string lMessageKey, string lDetail) =>
        lDetail.Length > 0
            ? QLocalizationCatalog.QLocalizationFormat(lMessageKey, lDetail)
            : QLocalizationCatalog.QLocalizationTextRead(lMessageKey);
}
