using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PCabin;
using Cadroue.UIVeneer.PPorch;

namespace Cadroue.UIVeneer.PHouse;
public partial class PWindow : Window
{
    private const double PWindowFontSize = 13;
    private readonly PStrip pStrip;
    private readonly PRail pRail;
    private readonly QWindow lWindow;
    private readonly QSash lSash;
    public PWindow(QWindow lWindowRoot)
    {
        InitializeComponent();
        Title = LLocalization.LLocalizationTextRead("Program.Window.Title");
        FontSize = PWindowFontSize;
        lWindow = lWindowRoot;
        lSash = lWindow.QWindowPart.QWindowPartSash;
        pStrip = new PStrip(lWindow.QWindowStrip);
        pRail = new PRail(pStrip);
        lWindow.QWindowDispatcherAttach(PWindowDispatch);
        pConsole.PConsoleAttach(lWindow.QWindowPart.QWindowPartConsole);
        PreviewMouseDown += pConsole.PConsoleSceneRead().PConsolePressHandle;
        Deactivated += pConsole.PConsoleSceneRead().PConsoleDeactivateHandle;
        PWindowNoticesAttach();
        pDeck.PDeckAttach(pStrip);
        Width = LFrameStore.LFrameStateCurrent.LFrameWidth;
        Height = LFrameStore.LFrameStateCurrent.LFrameHeight;
        lWindow.QWindowTabsStart();
        pToolbar.PToolbarTabSet(pRail);
        pToolbar.PToolbarLogoSet(lWindow.QWindowLogo);
        pToolbar.PToolbarChromeSet(lWindow.QWindowPart.QWindowPartChrome);
        pToolbar.PToolbarOptionsApplied += lWindow.QWindowOptionsApply;
        lWindow.QWindowOptionsApply();
        PWindowPositionRestore(LFrameStore.LFrameStateCurrent);
        pStrip.QStrip.QStripSelectChanged += PWindowTabHandle;
        PWindowTabHandle(pStrip.QStrip.QStripSelected);
        lWindow.QWindowRelayStart(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        PDropHandlersAdd();
        PResizeHandlersAdd();
        Closing += PWindowExitHandle;
        Closed += PWindowCloseHandle;
    }

    public QWindow QWindow => lWindow;

    public PStrip PStrip => pStrip;

    public static PStrip? PWindowStripRead() =>
        System.Windows.Application.Current.Windows.OfType<PWindow>().FirstOrDefault()?.PStrip;

    public static PConsole? PWindowConsoleRead() =>
        System.Windows.Application.Current.Windows.OfType<PWindow>().FirstOrDefault()?.pConsole;

    private void PWindowNoticesAttach()
    {
        lWindow.QWindowTabAdded += PWindowAddHandle;
        lWindow.QWindowFunnelUpdated += PWindowFunnelUpdate;
        lWindow.QWindowPlaced += PWindowPlace;
        lWindow.QWindowShown += PWindowShowHandle;
        lWindow.QWindowMediaDeferred += PWindowMediaDefer;
        lWindow.QWindowMediaOpened += PWindowMediaOpen;
        lWindow.QWindowTabAttached += PWindowTabAttach;
        lWindow.QWindowTabDetached += PWindowTabDetach;
        lWindow.QWindowLayoutChanged += PWindowLayoutApply;
        lWindow.QWindowVerticalApplied += PWindowVerticalApply;
        lWindow.QWindowHorizontalApplied += PWindowHorizontalApply;
        lWindow.QWindowFlowApplied += PWindowFlowApply;
        lWindow.QWindowVolumeSynced += PWindowVolumeSet;
        lWindow.QWindowPart.QWindowPartDrop.QWindowListAdded += PWindowListAdd;
        lWindow.QWindowPart.QWindowPartDrop.QWindowViewerOpened += PWindowViewerOpen;
        lWindow.QWindowPart.QWindowPartShortcut.QWindowShortcutPlayed += PWindowPlay;
        lWindow.QWindowPart.QWindowPartShortcut.QWindowShortcutPaused += PWindowPause;
        lWindow.QWindowPart.QWindowPartShortcut.QWindowShortcutAttach(
            pToolbar.PToolbarShortcutShow,
            PWindowUndo,
            PWindowRedo,
            pStrip.QStrip.QStripContentClear,
            PWindowMediaClear);
        QAskNotice.QAskRaised += PWindowAskHandle;
        lSash.QSashCaptureStarted += PResizeCaptureStart;
        lSash.QSashCaptureStopped += PResizeCaptureStop;
        lSash.QSashActiveChanged += PResizeRenderApply;
        lSash.QSashBoundsChanged += PResizeBoundsApply;
    }

    private bool PWindowDispatch(Func<bool> pBody) => Dispatcher.Invoke(pBody);

    private void PWindowAskHandle(QAsk lAsk, Action<bool> lAnswer) => PSAlert.PSAlertConfirm(this, lAsk, lAnswer);

    private void PWindowTabHandle(QStripTab? lStripTab) =>
        lWindow.QWindowTabSet(lStripTab, lStripTab?.QStripTabWorkspace.QWorkspaceViewer);

    private void PWindowAddHandle(QWindowTab lTab) =>
        pStrip.QStrip.QStripNameSet(
            pStrip.PStripAdd(lWindow.QWindowTabCreate(lTab), lTab.QWindowTabLayout),
            lTab.QWindowTabName);

    private void PWindowFunnelUpdate() => pStrip.QStrip.QStripTabs.ToList().ForEach(PWindowFunnelResolve);

    private void PWindowFunnelResolve(QStripTab lStripTab) =>
        pStrip.PStripWorkspaceRead(lStripTab)?.PWorkspaceSurface.PTabTargetsResolve();

    private void PWindowPlace(double pLeft, double pTop)
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = pLeft;
        Top = pTop;
    }

    private void PWindowShowHandle()
    {
        WindowState = PLook.PLookUnminimized[WindowState];
        Activate();
    }

    private void PWindowMediaDefer(string pPath) =>
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => lWindow.QWindowMediaRun(pPath)));

    private void PWindowMediaOpen(string pPath) =>
        pStrip.PStripSelected?.PWorkspaceViewer?.QViewer.QViewerSource.QViewerSourceOpen(pPath);

    private void PWindowTabAttach(QStripTab lTab, double pFlowHeight)
    {
        PWorkspace? pWorkspace = pStrip.PStripWorkspaceRead(lTab);
        pWorkspace?.PWorkspaceCommandApply(pFlowHeight);
        pWorkspace?.PWorkspaceWidthAttach(PWindowWidthApply);
        PWindowWidthApply();
    }

    private void PWindowTabDetach(QStripTab lTab)
    {
        PWorkspace? pWorkspace = pStrip.PStripWorkspaceRead(lTab);
        pWorkspace?.PWorkspaceWidthDetach(PWindowWidthApply);
        pWorkspace?.PWorkspaceCommandReset();
    }

    private void PWindowLayoutApply(bool pVertical)
    {
        pToolbar.PToolbarTabSet(null);
        pTabRailHost.Content = null;
        pConsole.PConsoleSceneSet(false);
        pToolbar.PToolbarSceneSet(null);
        pRail.PRailApply(pVertical);
        pToolbar.PToolbarVerticalSet(pVertical);
        pTabRailColumn.Width = PLook.PLookRailWidth[pVertical];
    }

    private void PWindowVerticalApply()
    {
        pTabRailHost.Content = pRail;
        pToolbar.PToolbarSceneSet(pConsole.PConsoleSceneRead());
        PWindowWidthApply();
    }

    private void PWindowHorizontalApply()
    {
        pToolbar.PToolbarTabSet(pRail);
        pConsole.PConsoleSceneSet(true);
        PWindowWidthApply();
    }

    private void PWindowFlowApply(double pFlowHeight) =>
        pStrip.PStripSelected?.PWorkspaceFlowApply(pFlowHeight);

    private void PWindowVolumeSet(double pVolume) =>
        pStrip.PStripSelected?.PWorkspaceViewer?.QViewer.QViewerPlayback.QViewerVolumeSet(pVolume);

    private void PWindowWidthApply()
    {
        (MinWidth, Width) = lWindow.QWindowWidthResolve(
            pStrip.PStripSelected?.PWorkspaceSurface.PTabWidthRead(),
            pTabRailColumn.Width.Value,
            Width);
    }

    private void PWindowListAdd(IReadOnlyList<string> pPaths) =>
        _ = pStrip.PStripSelected?.PWorkspaceList?.QList.QListPathsAdd(pPaths);

    private void PWindowViewerOpen(string pPath) => PWindowMediaOpen(pPath);

    private void PWindowPlay() => pStrip.PStripSelected?.PWorkspaceViewer?.QViewer.QViewerPlayback.QViewerPlay();

    private void PWindowPause() => pStrip.PStripSelected?.PWorkspaceViewer?.QViewer.QViewerPlayback.QViewerPause();

    private bool? PWindowUndo() => pStrip.PStripSelected?.QWorkspace.QWorkspaceUndo();

    private bool? PWindowRedo() => pStrip.PStripSelected?.QWorkspace.QWorkspaceRedo();

    private bool? PWindowMediaClear(IReadOnlySet<Guid> lCohorts) =>
        pStrip.PStripSelected?.QWorkspace.QWorkspaceMediaClear(lCohorts);

    private void PWindowExitHandle(object? sender, System.ComponentModel.CancelEventArgs eventArgs)
    {
        eventArgs.Cancel = !pStrip.QStrip.QStripClosure.QStripCloseConfirm();
    }

    private void PWindowPositionRestore(LFrameState lFrame)
    {
        PSash.PSashPlacementRestore(
            this,
            lFrame.LFrameLeft,
            lFrame.LFrameTop,
            lFrame.LFrameWidth,
            lFrame.LFrameHeight);
    }

    private void PWindowCloseHandle(object? sender, EventArgs eventArgs)
    {
        lWindow.QWindowFrameSave(
            PLook.PLookNormal[WindowState],
            new QSashBounds(Left, Top, Width, Height),
            new QSashBounds(RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height));
        lWindow.QWindowClose();
        QAskNotice.QAskRaised -= PWindowAskHandle;
        pStrip.QStrip.QStripSelectChanged -= PWindowTabHandle;
        pToolbar.PToolbarOptionsApplied -= lWindow.QWindowOptionsApply;
        ComponentDispatcher.ThreadPreprocessMessage -= PShortcutMessageHandle;
        PDropHandlersRemove();
        PResizeHandlersRemove();
        lWindow.QWindowTabSet(null, null);
        pStrip.QStrip.QStripTabs.ToList().ForEach(PWindowWorkspaceClose);
        Closing -= PWindowExitHandle;
        Closed -= PWindowCloseHandle;
    }

    private void PWindowWorkspaceClose(QStripTab lStripTab) => pStrip.PStripWorkspaceRead(lStripTab)?.PWorkspaceClose();
}
