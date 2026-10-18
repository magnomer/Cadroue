using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The scene strip's wiring: hands the list's and each command's input to the scene gate, asks through the window's
// envoy, paints the gate's names, scene and mark and words the gate's notices; QConsoleFile owns the file dialogs.
// Each member below the paint handlers that forwards one call stays for its surface caller.
public sealed class QConsoleScene
{
    private readonly CConsoleScene qConsoleSceneGate;
    private readonly CEnvoy qConsoleSceneEnvoy;

    public QConsoleScene(CConsoleScene lGate, CEnvoy lEnvoy)
    {
        qConsoleSceneGate = lGate;
        qConsoleSceneEnvoy = lEnvoy;
        QConsoleFile = new QConsoleFile(lGate.CConsoleSceneFile);
        lGate.CConsoleSceneListed += QConsoleNamesRaise;
        lGate.CConsoleSceneChanged += QConsoleSceneRaise;
        lGate.CConsoleSceneMarked += QConsoleMarkRaise;
    }

    public event Action<IReadOnlyList<string>>? QConsoleNamesApplied;

    public event Action<string?, string, bool>? QConsoleSceneApplied;

    public event Action<bool>? QConsoleMarkApplied;

    public event Action? QConsoleFocusCleared;

    public event Action<string, string>? QConsoleNoticeShown;

    public QConsoleFile QConsoleFile { get; }

    public void QConsoleWindowAttach(QWindowScene lSceneOwner, QStrip lStripOwner) =>
        qConsoleSceneGate.CConsoleSceneAttach(
            lSceneOwner.QWindowSceneRead,
            lSceneOwner.QWindowSceneApply,
            lStripOwner.QStripClosure.QStripAllClose,
            () => lStripOwner.QStripBusyCheck());

    private void QConsoleNamesRaise() => QConsoleNamesApplied?.Invoke(qConsoleSceneGate.CConsoleSceneNames);

    private void QConsoleSceneRaise() =>
        QConsoleSceneApplied?.Invoke(
            qConsoleSceneGate.CConsoleSceneSelected,
            qConsoleSceneGate.CConsoleSceneName,
            qConsoleSceneGate.CConsoleSceneDirty);

    private void QConsoleMarkRaise() => QConsoleMarkApplied?.Invoke(qConsoleSceneGate.CConsoleSceneDirty);

    public void QConsoleReloadSet(string? lReloadName) => qConsoleSceneGate.CConsoleSceneSet(lReloadName);

    public void QConsoleSceneStart() => qConsoleSceneGate.CConsoleSceneStart();

    public void QConsoleSceneRebuild() => qConsoleSceneGate.CConsoleSceneRebuild();

    public void QConsoleMarkUpdate() => qConsoleSceneGate.CConsoleSceneCheck();

    public void QConsoleTickHandle(bool lFocusWithin) => qConsoleSceneGate.CConsoleSceneTick(lFocusWithin);

    public void QConsoleRowHandle(string? lSceneName) => qConsoleSceneGate.CConsoleSceneAccept(lSceneName);

    public void QConsoleSelectHandle(object? lSelected, bool lDropOpen) =>
        qConsoleSceneGate.CConsoleSceneSelect(lSelected, lDropOpen, qConsoleSceneEnvoy);

    public void QConsoleCloseHandle() => qConsoleSceneGate.CConsoleSceneClose(qConsoleSceneEnvoy);

    public bool QConsoleDeleteHandle(string? lSceneName) => qConsoleSceneGate.CConsoleSceneDelete(lSceneName);

    public void QConsoleSceneSave(string? lText)
    {
        if (!qConsoleSceneGate.CConsoleSceneSave(lText))
        {
            QConsoleNoticeShown?.Invoke(
                QLocalizationCatalog.QLocalizationTextRead("Console.Scene.SaveTitle"),
                QLocalizationCatalog.QLocalizationTextRead("Console.Scene.NameRequired"));
        }
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
}
