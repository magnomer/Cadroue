using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The window's start: without a relay it restores the startup tabs or the last scene and defers the remembered media;
// a window started by a relay is placed at the drop and takes the relayed tab.
public sealed class QWindowStartup
{
    private readonly QWindow qWindow;

    public QWindowStartup(QWindow lOwner)
    {
        qWindow = lOwner;
    }

    public event Action<double, double>? QWindowPlaced;
    public event Action<string>? QWindowMediaDeferred;

    public void QWindowTabsStart()
    {
        if (LRelayStartup.LRelayPayloadRead() is null)
        {
            QWindowStartupRestore(LPreference.LPreferenceStateCurrent, LScene.LSceneCurrent);
        }
    }

    public void QWindowRelayStart(double lScreenLeft, double lScreenTop, double lScreenWidth, double lScreenHeight)
    {
        if (LRelayStartup.LRelayPayloadRead() is { } lRelay)
        {
            (double lLeft, double lTop) = QSashRelay.QSashRelayResolve(
                lRelay.LRelayDropLeft, lRelay.LRelayDropTop, lScreenLeft, lScreenTop, lScreenWidth, lScreenHeight);
            QWindowPlaced?.Invoke(lLeft, lTop);
            qWindow.QWindowRelay.QWindowRelayAccept(lRelay);
            LRelayStartup.LRelayStartupCommit();
            return;
        }

        QWindowMediaRestore(LPreference.LPreferenceStateCurrent);
    }

    private void QWindowStartupRestore(LPreferenceState lPreference, LSceneDocument lScene)
    {
        LTrace.LTraceLoadingSet(true);
        try
        {
            if (lPreference.LPreferenceStartupMode == "DefaultTab")
            {
                QWindowDefaultAdd(lPreference.LPreferenceStartupTabs);
                qWindow.QWindowStrip.QStripSelect(qWindow.QWindowStrip.QStripTabs.FirstOrDefault());
                qWindow.QWindowScene.QWindowLoadingRecord(null);
                return;
            }

            qWindow.QWindowScene.QWindowSceneRestore(lScene);
            qWindow.QWindowScene.QWindowLoadingRecord(lScene.LSceneName);
        }
        finally
        {
            LTrace.LTraceLoadingSet(false);
        }
    }

    private void QWindowDefaultAdd(IReadOnlyList<string> lKeys)
    {
        foreach (string lKey in lKeys)
        {
            qWindow.QWindowTabRaise(new QWindowTab(lKey, null, null, null));
        }
    }

    private void QWindowMediaRestore(LPreferenceState lPreference)
    {
        if (!lPreference.LPreferenceMediaAutomatic
            || string.IsNullOrWhiteSpace(lPreference.LPreferenceMediaPath)
            || !qWindow.QWindowUsher.CUsherFileExist(lPreference.LPreferenceMediaPath))
        {
            return;
        }

        QWindowMediaDeferred?.Invoke(lPreference.LPreferenceMediaPath);
    }
}
