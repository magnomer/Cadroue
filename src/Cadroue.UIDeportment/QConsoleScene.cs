using Cadroue.Core;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QConsoleScene
{
    private string? qConsoleReloadName;
    private string qConsoleSceneName = string.Empty;
    private QWindow? qWindow;
    private QStrip? qStrip;

    public string? QConsoleReloadName => qConsoleReloadName;

    public string QConsoleSceneName => qConsoleSceneName;

    public event Action<IReadOnlyList<string>>? QConsoleNamesApplied;

    public event Action<string?, string, bool>? QConsoleSceneApplied;

    public event Action<bool>? QConsoleMarkApplied;

    public event Action? QConsoleFocusCleared;

    public event Action<string, string>? QConsoleNoticeShown;

    public event Action<string, string>? QConsoleWarningShown;

    public void QConsoleWindowAttach(QWindow lWindowOwner, QStrip lStripOwner)
    {
        qWindow = lWindowOwner;
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
        qConsoleSceneName = LScene.LSceneActiveName;
        QConsoleSceneUpdate();
    }

    public void QConsoleSceneRebuild()
    {
        LScene.LSceneCatalogueLoad();
        QConsoleNamesApplied?.Invoke(LScene.LSceneNames);
        QConsoleSceneUpdate();
    }

    public void QConsoleSceneUpdate() =>
        QConsoleSceneApplied?.Invoke(
            LScene.LSceneRead(qConsoleSceneName) is not null ? qConsoleSceneName : null,
            qConsoleSceneName,
            QConsoleDirtyCheck());

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
            || qWindow is null)
        {
            return false;
        }

        return !LScene.LSceneMatch(lSceneStored, qWindow.QWindowSceneRead(qConsoleSceneName));
    }

    public void QConsoleRowHandle(string? lSceneName)
    {
        if (lSceneName is not null)
        {
            qConsoleReloadName = lSceneName;
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
            qConsoleReloadName = lSceneName;
            return;
        }

        qConsoleReloadName = null;
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

        var lAsk = new QAsk(
            LLocalization.LLocalizationFormat("Console.Scene.LoadConfirm", lSceneName),
            LLocalization.LLocalizationTextRead("Terms.Load"))
        {
            QAskTitle = LLocalization.LLocalizationTextRead("Console.Scene.LoadTitle")
        };
        QAskNotice.QAskPublish(lAsk, lAnswer => QConsoleSceneRun(lAnswer, lSceneName, lScene));
    }

    private void QConsoleSceneRun(bool lAnswer, string lSceneName, LSceneDocument lScene)
    {
        if (lAnswer && qWindow is { } lWindowOwner && qStrip is { } lStripOwner
            && lWindowOwner.QWindowSceneApply(
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
        qConsoleSceneName = lSceneName;
        LScene.LSceneActiveSet(lSceneName);
        QConsoleSceneUpdate();
    }

    public bool QConsoleDeleteHandle(string? lSceneName)
    {
        if (lSceneName is null)
        {
            return false;
        }

        qConsoleReloadName = null;
        if (!LScene.LSceneDelete(lSceneName))
        {
            return true;
        }

        if (QConsoleSceneCheck(lSceneName))
        {
            qConsoleSceneName = string.Empty;
            LScene.LSceneActiveSet(string.Empty);
        }

        QConsoleSceneRebuild();
        LTraceLog.LTraceInfoRecord($"Scene deleted '{lSceneName}'");
        return true;
    }

    public void QConsoleSceneSave(string? lText)
    {
        string lSceneName = (lText ?? string.Empty).Trim();
        if (lSceneName.Length == 0)
        {
            QConsoleNoticeShown?.Invoke(
                LLocalization.LLocalizationTextRead("Console.Scene.SaveTitle"),
                LLocalization.LLocalizationTextRead("Console.Scene.NameRequired"));
            return;
        }

        if (qWindow is not { } lWindowOwner)
        {
            return;
        }

        LScene.LSceneSave(lWindowOwner.QWindowSceneRead(lSceneName));
        QConsoleSceneRebuild();
        QConsoleSceneSelect(lSceneName);
        LTraceLog.LTraceInfoRecord($"Scene saved '{lSceneName}'");
    }

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
        string lSceneName = (lText ?? string.Empty).Trim();
        LSceneDocument? lScene = lSceneName.Length > 0 ? LScene.LSceneRead(lSceneName) : null;
        if (lScene is not null)
        {
            return lScene;
        }

        return qWindow?.QWindowSceneRead(
            lSceneName.Length > 0 ? lSceneName : LLocalization.LLocalizationTextRead("Console.Scene.DefaultName"));
    }

    public string QConsoleFileResolve(string? lText) =>
        LScene.LSceneFileResolve(
            QConsoleExportResolve(lText)?.LSceneName ?? string.Empty,
            LLocalization.LLocalizationTextRead("Console.Scene.DefaultName"));

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

        lScene.LSceneName = QConsoleNameCreate(QConsoleStemResolve(lScene.LSceneName, lPath), LScene.LSceneNames);
        LScene.LSceneSave(lScene);
        QConsoleSceneRebuild();
        LTraceLog.LTraceLoadingRecord($"Scene imported '{lScene.LSceneName}'");
    }

    public static string QConsoleStemResolve(string lSceneName, string lPath)
    {
        string lName = LScene.LSceneStemResolve(lSceneName, lPath);
        return lName.Length > 0 ? lName : LLocalization.LLocalizationTextRead("Console.Scene.ImportedName");
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
            LLocalization.LLocalizationTextRead(lTitleKey),
            lDetail.Length > 0
                ? LLocalization.LLocalizationFormat(lMessageKey, lDetail)
                : LLocalization.LLocalizationTextRead(lMessageKey));
}
