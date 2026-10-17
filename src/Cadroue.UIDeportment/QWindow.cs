using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QWindowTab(
    string QWindowTabKey,
    LPreset? QWindowTabPreset,
    LSceneTabEntry? QWindowTabLayout,
    string? QWindowTabName);

public sealed class QWindow
{
    private const double QWindowWidthFloor = 900;

    private readonly QStrip qStrip;
    private readonly Func<CWorkspace> qWindowWorkspaceSource;
    private QStripTab? qWindowTab;
    private QViewer? qWindowViewer;

    internal CUsher QWindowUsher { get; }

    private CInfo QWindowInfo { get; }

    public QWindow(CAtelier lAtelier)
    {
        ArgumentNullException.ThrowIfNull(lAtelier);

        QWindowUsher = lAtelier.CAtelierUsher;
        QWindowInfo = lAtelier.CAtelierInfo;
        qWindowWorkspaceSource = lAtelier.CAtelierWorkspaceCreate;
        qStrip = new QStrip(
            QWindowUsher,
            QWindowInfo,
            lAtelier.CAtelierLineage,
            qWindowWorkspaceSource,
            lAtelier.CAtelierInspectorCreate,
            lAtelier.CAtelierEdit);
        QWindowScene = new QWindowScene(qStrip);
        QWindowSceneAttach();
        QWindowLogo = new QLogoMenu(QWindowUsher);
        QWindowPart = new QWindowPart(this);
    }

    private void QWindowSceneAttach()
    {
        QWindowScene.QWindowSceneAdded += QWindowTabRaise;
        QWindowScene.QWindowSceneRestored += QWindowFunnelRaise;
    }

    public QStrip QWindowStrip => qStrip;

    public QWindowScene QWindowScene { get; }

    public QWorkspacePair QWindowPairCreate() =>
        new(new QWorkspaceFacts(string.Empty), QWindowUsher, QWindowInfo, qWindowWorkspaceSource());

    public QLogoMenu QWindowLogo { get; }

    public void QWindowDispatcherAttach(Func<Func<bool>, bool> lDispatch) =>
        LRelayChannel.LRelayAcceptSeam = lRelay => QWindowRelayHandle(lRelay, lDispatch);

    public QStripTab QWindowTabCreate(QWindowTab lTab)
    {
        QStripTab lStripTab = qStrip.QStripTabCreate(lTab.QWindowTabKey);
        if (lTab.QWindowTabPreset is { } lPreset)
        {
            lStripTab.QStripTabWorkspace.QWorkspacePresetAccept(lPreset);
        }

        return lStripTab;
    }

    public event Action<QWindowTab>? QWindowTabAdded;
    public event Action? QWindowFunnelUpdated;
    public event Action<double, double>? QWindowPlaced;
    public event Action? QWindowShown;
    public event Action<string>? QWindowMediaDeferred;
    public event Action<string>? QWindowMediaOpened;
    public event Action<QStripTab, double>? QWindowTabAttached;
    public event Action<QStripTab>? QWindowTabDetached;
    public event Action<bool>? QWindowLayoutChanged;
    public event Action? QWindowVerticalApplied;
    public event Action? QWindowHorizontalApplied;
    public event Action<double>? QWindowFlowApplied;
    public event Action<double>? QWindowVolumeSynced;

    public QWindowPart QWindowPart { get; }

    public QStripTab? QWindowTab => qWindowTab;

    public QViewer? QWindowViewer => qWindowViewer;

    public bool QWindowListPresent => qWindowTab?.QStripTabDocket is not null;

    public bool QWindowViewerPresent => qWindowViewer is not null;

    public bool QWindowAudioAllowed => qWindowViewer?.QViewerAudioAllowed == true;

    public void QWindowTabSet(QStripTab? lTab, QViewer? lViewer)
    {
        if (qWindowTab is { } lPrevious)
        {
            QWindowTabDetached?.Invoke(lPrevious);
        }

        QWindowCurrentSet(lTab, lViewer);
        if (lTab is null)
        {
            return;
        }

        QWindowTabAttached?.Invoke(lTab, LFrameStore.LFrameStateCurrent.LFrameFlowHeight);
        QWindowVolumeSync(LPreference.LPreferenceStateCurrent);
    }

    private void QWindowCurrentSet(QStripTab? lTab, QViewer? lViewer)
    {
        qWindowTab = lTab;
        qWindowViewer = lViewer;
    }

    public void QWindowTabsStart()
    {
        if (LRelayChannel.LRelayPayloadRead() is null)
        {
            QWindowStartupRestore(LPreference.LPreferenceStateCurrent, LScene.LSceneCurrent);
        }
    }

    public void QWindowRelayStart(double lScreenLeft, double lScreenTop, double lScreenWidth, double lScreenHeight)
    {
        if (LRelayChannel.LRelayPayloadRead() is { } lRelay)
        {
            (double lLeft, double lTop) = QSashRelay.QSashRelayResolve(
                lRelay.LRelayDropLeft, lRelay.LRelayDropTop, lScreenLeft, lScreenTop, lScreenWidth, lScreenHeight);
            QWindowPlaced?.Invoke(lLeft, lTop);
            QWindowRelayAccept(lRelay);
            LRelayChannel.LRelayStartupCommit();
            return;
        }

        QWindowMediaRestore(LPreference.LPreferenceStateCurrent);
    }

    public void QWindowMediaRun(string lPath)
    {
        if (qWindowViewer is null)
        {
            QStripTab? lMediaTab = QWindowMediaFind();
            if (lMediaTab is not null)
            {
                qStrip.QStripSelect(lMediaTab);
            }
        }

        if (qWindowViewer is not null)
        {
            QWindowMediaOpened?.Invoke(lPath);
        }
    }

    private QStripTab? QWindowMediaFind() => qStrip.QStripTabs.FirstOrDefault(lTab => !lTab.QStripTabWorklist);

    public void QWindowOptionsApply()
    {
        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent;
        QWindowLayoutChanged?.Invoke(lPreference.LPreferenceVerticalTabs);
        if (lPreference.LPreferenceVerticalTabs)
        {
            QWindowVerticalApplied?.Invoke();
        }
        else
        {
            QWindowHorizontalApplied?.Invoke();
        }

        QWindowFlowApplied?.Invoke(LFrameStore.LFrameStateCurrent.LFrameFlowHeight);
        QWindowVolumeSync(lPreference);
    }

    public (double QWindowMinimum, double QWindowWidth) QWindowWidthResolve(
        double? lRequired,
        double lReserved,
        double lWidth)
    {
        double lMinimum = Math.Max(QWindowWidthFloor, lRequired ?? 0) + lReserved;
        return (lMinimum, Math.Max(lWidth, lMinimum));
    }

    public void QWindowFrameSave(bool lNormal, QSashBounds lBounds, QSashBounds lRestore)
    {
        if (QWindowFrameResolve(lNormal, lBounds, lRestore) is { } lState)
        {
            LFrameStore.LFrameSave(lState);
        }
    }

    private static LFrameState? QWindowFrameResolve(bool lNormal, QSashBounds lBounds, QSashBounds lRestore)
    {
        QSashBounds lFrame = lNormal ? lBounds : lRestore;
        if (!(lFrame.QSashWidth > 0 && lFrame.QSashHeight > 0))
        {
            return null;
        }

        return new LFrameState
        {
            LFrameLeft = lFrame.QSashLeft,
            LFrameTop = lFrame.QSashTop,
            LFrameWidth = lFrame.QSashWidth,
            LFrameHeight = lFrame.QSashHeight,
            LFrameFlowHeight = LFrameStore.LFrameStateCurrent.LFrameFlowHeight
        };
    }

    public void QWindowClose()
    {
        LScene.LSceneStateSave(QWindowScene.QWindowSceneRead(LScene.LSceneActiveName));
        QWindowDispatcherDetach();
    }

    private static void QWindowDispatcherDetach() => LRelayChannel.LRelayAcceptSeam = null;

    public void QWindowRelayAccept(LRelay lRelay)
    {
        QWindowTabAdded?.Invoke(new QWindowTab(
            lRelay.LRelayLayoutKey,
            LPreset.LPresetStateCreate(lRelay.LRelayExport),
            lRelay.LRelayLayout,
            lRelay.LRelayCustomName));
        QStripTab? lAdded = qStrip.QStripTabs.LastOrDefault();
        qStrip.QStripSelect(lAdded);
        lAdded?.QStripTabWorkspace.QWorkspaceRelay.QWorkspaceRelayApply(lRelay);
        QWindowFunnelUpdated?.Invoke();
    }

    private bool QWindowRelayHandle(LRelay lRelay, Func<Func<bool>, bool> lDispatch)
    {
        try
        {
            return lDispatch(() =>
            {
                QWindowRelayAccept(lRelay);
                QWindowShown?.Invoke();
                return true;
            });
        }
        catch (Exception lRelayException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Relayed '{lRelay.LRelayLayoutKey}' tab could not be taken; refused",
                lRelayException);
            return false;
        }
    }

    private void QWindowStartupRestore(LPreferenceState lPreference, LSceneDocument lScene)
    {
        LTrace.LTraceLoadingSet(true);
        try
        {
            if (lPreference.LPreferenceStartupMode == "DefaultTab")
            {
                QWindowDefaultAdd(lPreference.LPreferenceStartupTabs);
                qStrip.QStripSelect(qStrip.QStripTabs.FirstOrDefault());
                QWindowScene.QWindowLoadingRecord(null);
                return;
            }

            QWindowScene.QWindowSceneRestore(lScene);
            QWindowScene.QWindowLoadingRecord(lScene.LSceneName);
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
            QWindowTabAdded?.Invoke(new QWindowTab(lKey, null, null, null));
        }
    }

    private void QWindowMediaRestore(LPreferenceState lPreference)
    {
        if (!lPreference.LPreferenceMediaAutomatic
            || string.IsNullOrWhiteSpace(lPreference.LPreferenceMediaPath)
            || !QWindowUsher.CUsherFileExist(lPreference.LPreferenceMediaPath))
        {
            return;
        }

        QWindowMediaDeferred?.Invoke(lPreference.LPreferenceMediaPath);
    }

    private void QWindowVolumeSync(LPreferenceState lPreference)
    {
        if (qWindowViewer is null || !lPreference.LPreferenceVolumeUnified)
        {
            return;
        }

        QWindowVolumeSynced?.Invoke(lPreference.LPreferenceVolume);
    }

    private void QWindowTabRaise(QWindowTab lTab) => QWindowTabAdded?.Invoke(lTab);

    private void QWindowFunnelRaise() => QWindowFunnelUpdated?.Invoke();
}
