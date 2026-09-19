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
    private const int PResizeBorderPixels = 8;
    private const double PWindowFontSize = 13;
    private readonly PStrip pStrip;
    private readonly PRail pRail;
    private readonly LWindow lWindow;
    private readonly LSash lSash = new(PResizeBorderPixels);
    public PWindow()
    {
        InitializeComponent();
        Title = LLocalization.LLocalizationTextRead("Program.Window.Title");
        FontSize = PWindowFontSize;
        pStrip = new PStrip();
        pRail = new PRail(pStrip);
        lWindow = new LWindow(pStrip.LStrip, PWindowDispatch);
        PWindowNoticesAttach();
        pDeck.PDeckAttach(pStrip);
        Width = LFrameStore.LFrameStateCurrent.LFrameWidth;
        Height = LFrameStore.LFrameStateCurrent.LFrameHeight;
        lWindow.LWindowTabsStart();
        pToolbar.PToolbarTabSet(pRail);
        pToolbar.PToolbarOptionsApply += lWindow.LWindowOptionsHandle;
        lWindow.LWindowOptionsHandle(LPreference.LPreferenceStateCurrent);
        PWindowPositionRestore(LFrameStore.LFrameStateCurrent);
        pStrip.LStrip.LStripSelectChange += PWindowTabHandle;
        PWindowTabHandle(pStrip.LStrip.LStripSelected);
        lWindow.LWindowRelayStart(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        PDropHandlersAdd();
        PResizeHandlersAdd();
        Closing += PWindowExitHandle;
        Closed += PWindowCloseHandle;
    }

    public LWindow LWindow => lWindow;

    public PStrip PStrip => pStrip;

    public static PStrip? PWindowStripRead() =>
        System.Windows.Application.Current.Windows.OfType<PWindow>().FirstOrDefault()?.PStrip;

    private void PWindowNoticesAttach()
    {
        lWindow.LWindowTabAdd += PWindowAddHandle;
        lWindow.LWindowTargetApply += PWindowTargetHandle;
        lWindow.LWindowFunnelUpdate += PWindowFunnelUpdate;
        lWindow.LWindowRelayApply += PWindowRelayHandle;
        lWindow.LWindowPlace += PWindowPlace;
        lWindow.LWindowShow += PWindowShowHandle;
        lWindow.LWindowMediaDefer += PWindowMediaDefer;
        lWindow.LWindowMediaOpen += PWindowMediaOpen;
        lWindow.LWindowTabAttach += PWindowTabAttach;
        lWindow.LWindowTabDetach += PWindowTabDetach;
        lWindow.LWindowLayoutChange += PWindowLayoutApply;
        lWindow.LWindowVerticalApply += PWindowVerticalApply;
        lWindow.LWindowHorizontalApply += PWindowHorizontalApply;
        lWindow.LWindowFlowApply += PWindowFlowApply;
        lWindow.LWindowVolumeSet += PWindowVolumeSet;
        lWindow.LWindowDrop.LWindowListAdd += PWindowListAdd;
        lWindow.LWindowDrop.LWindowViewerOpen += PWindowViewerOpen;
        lWindow.LWindowShortcut.LWindowShortcutPlay += PWindowPlay;
        lWindow.LWindowShortcut.LWindowShortcutPause += PWindowPause;
        lWindow.LWindowShortcut.LWindowShortcutAttach(
            pToolbar.PToolbarShortcutShow,
            PWindowUndo,
            PWindowRedo,
            pStrip.LStrip.LStripContentClear,
            PWindowMediaClear,
            PWindowFlowDispatch);
        LAskNotice.LAskRaise += PWindowAskHandle;
        lSash.LSashCaptureStart += PResizeCaptureStart;
        lSash.LSashCaptureStop += PResizeCaptureStop;
        lSash.LSashActiveChange += PResizeRenderApply;
        lSash.LSashBoundsChange += PResizeBoundsApply;
    }

    private bool PWindowDispatch(Func<bool> pBody) => Dispatcher.Invoke(pBody);

    private void PWindowAskHandle(LAsk lAsk, Action<bool> lAnswer) => PSAlert.PSAlertConfirm(this, lAsk, lAnswer);

    private void PWindowTabHandle(LStripTab? lStripTab) =>
        lWindow.LWindowTabSet(lStripTab, lStripTab?.LStripTabWorkspace?.LWorkspaceViewer);

    private void PWindowAddHandle(LWindowTab lTab) =>
        pStrip.LStrip.LStripNameSet(
            pStrip.PStripAdd(lTab.LWindowTabKey, lTab.LWindowTabPreset, lTab.LWindowTabLayout),
            lTab.LWindowTabName);

    private void PWindowTargetHandle(Guid lSource, Guid lTarget) =>
        pStrip.PStripWorkspaceRead(pStrip.LStrip.LStripTabFind(lSource))?
            .PWorkspaceSurface.PTabAction?.PActionRelayApply(lTarget);

    private void PWindowFunnelUpdate() => pStrip.LStrip.LStripTabs.ToList().ForEach(PWindowFunnelResolve);

    private void PWindowFunnelResolve(LStripTab lStripTab) =>
        (pStrip.PStripWorkspaceRead(lStripTab)?.PWorkspaceSurface as PFunnelTab)?
            .PFunnelTargetsResolve(pStrip.LStrip.LStripTabs);

    private void PWindowRelayHandle(LRelay lRelay) =>
        pStrip.PStripSelected?.LWorkspace.LWorkspaceRelayApply(lRelay);

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
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => lWindow.LWindowMediaRun(pPath)));

    private void PWindowMediaOpen(string pPath) =>
        pStrip.PStripSelected?.PWorkspaceViewer?.PViewerSourceOpen(pPath);

    private void PWindowTabAttach(LStripTab lTab, double pFlowHeight)
    {
        PWorkspace? pWorkspace = pStrip.PStripWorkspaceRead(lTab);
        pWorkspace?.PWorkspaceCommandApply(pFlowHeight);
        pWorkspace?.PWorkspaceWidthAttach(PWindowWidthApply);
        PWindowWidthApply();
    }

    private void PWindowTabDetach(LStripTab lTab)
    {
        PWorkspace? pWorkspace = pStrip.PStripWorkspaceRead(lTab);
        pWorkspace?.PWorkspaceWidthDetach(PWindowWidthApply);
        pWorkspace?.PWorkspaceCommandReset();
    }

    private void PWindowLayoutApply(bool pVertical)
    {
        pToolbar.PToolbarTabSet(null);
        pTabRailHost.Content = null;
        pConsole.PConsoleSceneSet(null);
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
        pConsole.PConsoleSceneSet(pConsole.PConsoleSceneRead());
        PWindowWidthApply();
    }

    private void PWindowFlowApply(double pFlowHeight) =>
        pStrip.PStripSelected?.PWorkspaceFlowApply(pFlowHeight);

    private void PWindowVolumeSet(double pVolume) =>
        pStrip.PStripSelected?.PWorkspaceViewer?.PViewerVolumeSet(pVolume);

    private void PWindowWidthApply()
    {
        (MinWidth, Width) = lWindow.LWindowWidthResolve(
            pStrip.PStripSelected?.PWorkspaceSurface.PTabWidthRead(),
            pTabRailColumn.Width.Value,
            Width);
    }

    private void PWindowListAdd(IReadOnlyList<string> pPaths) =>
        _ = pStrip.PStripSelected?.PWorkspaceList?.PListPathsAdd(pPaths);

    private void PWindowViewerOpen(string pPath) => PWindowMediaOpen(pPath);

    private void PWindowPlay() => pStrip.PStripSelected?.PWorkspaceViewer?.PViewerPlay();

    private void PWindowPause() => pStrip.PStripSelected?.PWorkspaceViewer?.PViewerPause();

    private bool? PWindowUndo() => pStrip.PStripSelected?.LWorkspace.LWorkspaceUndo();

    private bool? PWindowRedo() => pStrip.PStripSelected?.LWorkspace.LWorkspaceRedo();

    private bool? PWindowMediaClear(IReadOnlySet<Guid> lCohorts) =>
        pStrip.PStripSelected?.LWorkspace.LWorkspaceMediaClear(lCohorts);

    private bool? PWindowFlowDispatch(string pCode) =>
        pStrip.PStripSelected?.PWorkspaceFlow?.PFlowShortcutDispatch(pCode);

    public LSceneRecord PWindowSceneRead(string lSceneName) => lWindow.LWindowSceneRead(lSceneName);

    public bool PWindowSceneApply(LSceneRecord lScene) =>
        lWindow.LWindowSceneApply(lScene, pStrip.LStrip.LStripCloseConfirm(), pStrip.LStrip.LStripAllClose);

    private void PWindowExitHandle(object? sender, System.ComponentModel.CancelEventArgs eventArgs)
    {
        eventArgs.Cancel = !pStrip.LStrip.LStripCloseConfirm();
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
        lWindow.LWindowFrameSave(
            PLook.PLookNormal[WindowState],
            new LSashBounds(Left, Top, Width, Height),
            new LSashBounds(RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height));
        lWindow.LWindowClose();
        LAskNotice.LAskRaise -= PWindowAskHandle;
        pStrip.LStrip.LStripSelectChange -= PWindowTabHandle;
        pToolbar.PToolbarOptionsApply -= lWindow.LWindowOptionsHandle;
        ComponentDispatcher.ThreadPreprocessMessage -= PShortcutMessageHandle;
        PDropHandlersRemove();
        PResizeHandlersRemove();
        lWindow.LWindowTabSet(null, null);
        pStrip.LStrip.LStripTabs.ToList().ForEach(PWindowWorkspaceClose);
        Closing -= PWindowExitHandle;
        Closed -= PWindowCloseHandle;
    }

    private void PWindowWorkspaceClose(LStripTab lStripTab) => pStrip.PStripWorkspaceRead(lStripTab)?.PWorkspaceClose();
}
