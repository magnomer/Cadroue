using System.Windows;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The console's wiring: hands each command and the shown tab's station to the console gate, asks through the
// window's envoy, and words the gate's status, progress and warnings for the surface.
public sealed class QConsole
{
    private readonly CConsole qConsoleGate;
    private readonly CEnvoy qConsoleEnvoy;
    private readonly QStrip qConsoleStrip;

    public QConsole(CConsole lGate, CEnvoy lEnvoy, QWindowScene lSceneOwner, QStrip lStripOwner)
    {
        qConsoleGate = lGate;
        qConsoleEnvoy = lEnvoy;
        qConsoleStrip = lStripOwner;
        QConsoleStation = new QConsoleStation(lGate);
        QConsoleScene = new QConsoleScene(lGate.CConsoleScene, lEnvoy);
        qConsoleGate.CConsoleChanged += QConsoleUpdate;
        qConsoleGate.CConsoleDeferred += QConsoleDefer;
        qConsoleGate.CConsoleProgress.CConsoleProgressChanged += QConsoleProgressRaise;
        lStripOwner.QStripSelectChanged += QConsoleTabHandle;
        QConsoleScene.QConsoleWindowAttach(lSceneOwner, lStripOwner);
    }

    public QConsoleStation QConsoleStation { get; }

    public QConsoleScene QConsoleScene { get; }

    public Duration QConsoleProgressGlide => new(qConsoleGate.CConsolePeriod);

    public event Action<QConsoleStatus>? QConsoleStatusApplied;

    public event Action<double, bool>? QConsoleProgressApplied;

    public event Action? QConsoleUpdateDeferred;

    public event Action<string, string>? QConsoleWarningShown;

    private void QConsoleTabHandle(QStripTab? lTab)
    {
        if (lTab?.QStripTabWorkspace.QWorkspaceStation is { } lStation)
        {
            qConsoleGate.CConsoleStationSet(lStation);
        }
    }

    public void QConsoleClose()
    {
        qConsoleGate.CConsoleClose();
        qConsoleGate.CConsoleChanged -= QConsoleUpdate;
        qConsoleGate.CConsoleDeferred -= QConsoleDefer;
        qConsoleGate.CConsoleProgress.CConsoleProgressChanged -= QConsoleProgressRaise;
        QConsoleStation.QConsoleWatchStop();
        qConsoleStrip.QStripSelectChanged -= QConsoleTabHandle;
    }

    public void QConsoleScheduleLoad() => qConsoleGate.CConsoleLoad();

    private void QConsoleDefer() => QConsoleUpdateDeferred?.Invoke();

    private void QConsoleProgressRaise() =>
        QConsoleProgressApplied?.Invoke(
            qConsoleGate.CConsoleProgress.CConsoleProgressValue, qConsoleGate.CConsoleProgress.CConsoleProgressGlide);

    public void QConsoleUpdate()
    {
        CConsoleStatus lStatus = qConsoleGate.CConsoleUpdate();
        QConsoleStatusApplied?.Invoke(QConsoleLine.QConsoleStatusCreate(lStatus));
    }

    public void QConsoleStart() => qConsoleGate.CConsoleStart();

    public void QConsolePause() => qConsoleGate.CConsolePause();

    public void QConsoleStop() => qConsoleGate.CConsoleStop();

    public void QConsoleAutoSet(bool? lChecked) => qConsoleGate.CConsoleAutoSet(lChecked);

    public void QConsoleCancel() => qConsoleGate.CConsoleCancel(qConsoleEnvoy);

    public void QConsoleRemove()
    {
        if (qConsoleGate.CConsoleRemove() is { } lMessage)
        {
            QConsoleWarningShown?.Invoke(
                QLocalizationCatalog.QLocalizationTextRead("Console.Remove.Title"),
                QConsoleLine.QConsoleTextRead(lMessage));
        }
    }

    // The roster's removal warning, until the roster's own gate words it.
    public static string? QConsoleRemovalFormat(IReadOnlyDictionary<Guid, LScheduleRemoval> lOutcomes)
    {
        int lHeld = lOutcomes.Values.Count(lOutcome => lOutcome == LScheduleRemoval.LScheduleRemovalHeld);
        int lBlocked = lOutcomes.Values.Count(lOutcome => lOutcome == LScheduleRemoval.LScheduleRemovalBlocked);
        if (lHeld == 0 && lBlocked == 0)
        {
            return null;
        }

        string lDetail = lHeld > 0 && lBlocked > 0
            ? QLocalizationCatalog.QLocalizationFormat("Console.Remove.HeldAndBlocked", lHeld, lBlocked)
            : lHeld > 0
                ? QLocalizationCatalog.QLocalizationFormat("Console.Remove.Held", lHeld)
                : QLocalizationCatalog.QLocalizationFormat("Console.Remove.Blocked", lBlocked);
        return QLocalizationCatalog.QLocalizationFormat(
            "Console.Remove.Partial", lOutcomes.Count - lHeld - lBlocked, lOutcomes.Count, lDetail);
    }

    public void QConsoleDoneClear() => qConsoleGate.CConsoleDoneClear(qConsoleEnvoy);

    public void QConsoleAllClear() => _ = qConsoleGate.CConsoleAllClear(qConsoleEnvoy);

    public void QConsoleTabsClear()
    {
        if (qConsoleGate.CConsoleTabsClear(qConsoleEnvoy))
        {
            qConsoleStrip.QStripContentClear();
        }
    }
}
