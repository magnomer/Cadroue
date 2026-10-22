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

// The main window's driver: builds its strip, scene and parts, keeps the current tab and viewer, and broadcasts the
// tab, layout and volume changes the surface applies.
public sealed class QWindow
{
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
        qStrip = new QStrip(lAtelier);
        QWindowScene = new QWindowScene(qStrip);
        QWindowSceneAttach();
        QWindowRelay = new QWindowRelay(this);
        QWindowStartup = new QWindowStartup(this);
        QWindowLogo = new QLogoMenu(QWindowUsher, lAtelier.CAtelierAboutCreate);
        QWindowPart = new QWindowPart(this, lAtelier.CAtelierConsoleCreate());
    }

    private void QWindowSceneAttach()
    {
        QWindowScene.QWindowSceneAdded += QWindowTabRaise;
        QWindowScene.QWindowSceneRestored += QWindowFunnelRaise;
    }

    public QStrip QWindowStrip => qStrip;

    public QWindowScene QWindowScene { get; }

    public QWindowRelay QWindowRelay { get; }

    public QWindowStartup QWindowStartup { get; }

    public QWorkspacePair QWindowPairCreate() =>
        new(new QWorkspaceFacts(string.Empty), QWindowUsher, QWindowInfo, qWindowWorkspaceSource());

    public QLogoMenu QWindowLogo { get; }

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

    public void QWindowClose()
    {
        LScene.LSceneStateSave(QWindowScene.QWindowSceneRead(LScene.LSceneActiveName));
        QWindowRelay.QWindowDispatcherDetach();
    }

    private void QWindowVolumeSync(LPreferenceState lPreference)
    {
        if (qWindowViewer is null || !lPreference.LPreferenceVolumeUnified)
        {
            return;
        }

        QWindowVolumeSynced?.Invoke(lPreference.LPreferenceVolume);
    }

    internal void QWindowTabRaise(QWindowTab lTab) => QWindowTabAdded?.Invoke(lTab);

    internal void QWindowFunnelRaise() => QWindowFunnelUpdated?.Invoke();
}
