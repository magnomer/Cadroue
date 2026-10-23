using System;
using System.Windows;
using System.Windows.Threading;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;
public partial class PWindow : Window
{
    private const double PWindowFontSize = 13;
    private readonly PStrip pStrip;
    private readonly PRail pRail;
    private readonly QWindow lWindow;
    private readonly PWindowDrop pWindowDrop;
    private readonly PWindowResize pWindowResize;
    private readonly PWindowShortcut pWindowShortcut;
    public PWindow(QWindow lWindowRoot)
    {
        InitializeComponent();
        PWindowLookApply();
        lWindow = lWindowRoot;
        pWindowResize = new PWindowResize(this, lWindow.QWindowPart.QWindowPartSash);
        pStrip = new PStrip(
            lWindow.QWindowStrip,
            qExport => QExportSurface.QExportSurfaceBuild(
                qExport,
                new PExport(),
                () => new PSEncoder(),
                PSVerdict.PSVerdictShow,
                PSWarning.PSWarningShow,
                () => new PExportPresetGroup(),
                () => new PExportRow(),
                () => new PExportPreset(),
                () => new PExportCommands()));
        pRail = new PRail(pStrip);
        pWindowDrop = new PWindowDrop(lWindow.QWindowPart.QWindowPartDrop);
        pWindowShortcut = new PWindowShortcut(this, pStrip, lWindow.QWindowPart.QWindowPartShortcut);
        QLogoMenu qLogoMenu = lWindow.QWindowLogo;
        QLogoAnswer qLogoAnswer = new(
            owner => qLogoMenu
                .QLogoAboutCreate(
                    () => new PSAbout(),
                    PSDialog.PSDialogBuild,
                    PSGrabber.PSGrabberPairBuild,
                    PSWarning.PSWarningShow,
                    PSGrabberPlacement.PSGrabberPlacementRestore,
                    PSGrabberPlacement.PSGrabberPlacementSave)
                .QSAboutShow(owner),
            (owner, applied) => QSOptionsWindow.QSOptionsWindowShow(
                qLogoMenu.QLogoOptionsCreate(),
                new QSOptionsAnswer(
                    () => new PSOptions(),
                    PSSubwindow.PSSubwindowBuild,
                    PSSheet.PSSheetApply,
                    PSSheet.PSSheetHeaderBuild,
                    PSGrabber.PSGrabberPairBuild,
                    (PSGrabberPlacement.PSGrabberPlacementRestore, PSGrabberPlacement.PSGrabberPlacementSave),
                    (PSWarning.PSWarningShow, PSAnnouncement.PSAnnouncementShow),
                    (window, title, question, action) => PSAlert.PSAlertConfirm(
                        window, title, question, action),
                    PSDiagnosis.PSDiagnosisShow,
                    PSMode.PSModeBuild,
                    PSMode.PSModeTextRead),
                owner,
                applied),
            (owner, applied) => PSKeymap.PSKeymapShow(
                owner, qLogoMenu.QLogoKeymapCreate(), applied),
            owner => PLogWindow.PLogWindowShow(owner, qLogoMenu.QLogoLogCreate),
            (owner, answer) => PSDebug.PSDebugShow(owner, qLogoMenu, answer));
        pToolbar.PToolbarChromeSet(lWindow.QWindowPart.QWindowPartChrome);
        qLogoMenu.QLogoMenuAttach(
            pToolbar,
            lWindow.QWindowPart.QWindowPartChrome,
            qLogoAnswer,
            lWindow.QWindowOptionsApply);
        lWindow.QWindowRelay.QWindowDispatcherAttach(PWindowDispatch);
        pConsole.PConsoleAttach(lWindow.QWindowPart.QWindowPartConsole);
        PWindowConsoleAttach();
        PWindowNoticesAttach(() => qLogoAnswer.QLogoAnswerShortcut(this, lWindow.QWindowOptionsApply));
        pStrip.QStrip.QStripDeck.QDeckAttach(pDeck);
        PWindowSizeSet(LFrameStore.LFrameStateCurrent);
        lWindow.QWindowStartup.QWindowTabsStart();
        pToolbar.PToolbarTabSet(pRail);
        lWindow.QWindowOptionsApply();
        PWindowPositionRestore(LFrameStore.LFrameStateCurrent);
        pStrip.QStrip.QStripSelectChanged += PWindowTabHandle;
        PWindowTabHandle(pStrip.QStrip.QStripSelected);
        lWindow.QWindowStartup.QWindowRelayStart(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        pWindowDrop.PDropHandlersAdd(this);
        pWindowResize.PResizeHandlersAdd();
        PWindowCloseAttach();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PWindowDwm.PWindowMessageAttach(this);
        pWindowShortcut.PShortcutAttach();
        PWindowDwm.PWindowDwmApply(this);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        PWindowDwm.PWindowIconApply(this);
        Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() => PWindowDwm.PWindowIconApply(this)));
    }

    private void PWindowLookApply()
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Program.Window.Title");
        FontSize = PWindowFontSize;
    }

    private void PWindowConsoleAttach()
    {
        PreviewMouseDown += pConsole.PConsoleSceneRead().PConsolePressHandle;
        Deactivated += pConsole.PConsoleSceneRead().PConsoleDeactivateHandle;
    }

    private void PWindowSizeSet(LFrameState lFrame)
    {
        Width = lFrame.LFrameWidth;
        Height = lFrame.LFrameHeight;
    }

    private void PWindowCloseAttach()
    {
        Closing += PWindowExitHandle;
        Closed += PWindowCloseHandle;
    }

    private void PWindowCloseDetach()
    {
        Closing -= PWindowExitHandle;
        Closed -= PWindowCloseHandle;
    }

    public QWindow QWindow => lWindow;

    public PStrip PStrip => pStrip;

    public static PStrip? PWindowStripRead() =>
        System.Windows.Application.Current.Windows.OfType<PWindow>().FirstOrDefault()?.PStrip;

    private void PWindowNoticesAttach(Action pShortcutShow)
    {
        lWindow.QWindowTabAdded += PWindowAddHandle;
        lWindow.QWindowFunnelUpdated += PWindowFunnelUpdate;
        lWindow.QWindowStartup.QWindowPlaced += PWindowPlace;
        lWindow.QWindowRelay.QWindowShown += PWindowShowHandle;
        lWindow.QWindowStartup.QWindowMediaDeferred += PWindowMediaDefer;
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
            pShortcutShow,
            PWindowUndo,
            PWindowRedo,
            pStrip.QStrip.QStripContentClear,
            PWindowMediaClear);
        QAskNotice.QAskRaised += PWindowAskHandle;
        QWarningNotice.QWarningRaised += PWindowWarningHandle;
        lWindow.QWindowPart.QWindowPartEnvoy.QEnvoyAttach(
            PWindowAskHandle,
            PWindowWarningHandle,
            (lTitle, lMessage) => PSWarning.PSWarningShow(this, lTitle, lMessage));
        pWindowResize.PResizeSashAttach();
    }

    private bool PWindowDispatch(Func<bool> pBody) => Dispatcher.Invoke(pBody);

    private void PWindowAskHandle(QAsk lAsk, Action<bool> lAnswer) => PSAlert.PSAlertConfirm(this, lAsk, lAnswer);

    private void PWindowWarningHandle(string lTitle, string lMessage) =>
        PSAnnouncement.PSAnnouncementShow(this, lTitle, lMessage);

    private void PWindowTabHandle(QStripTab? lStripTab) =>
        lWindow.QWindowTabSet(lStripTab, lStripTab?.QStripTabWorkspace.QWorkspaceViewer);

    private void PWindowAddHandle(QWindowTab lTab) =>
        pStrip.QStrip.QStripTitle.QStripNameSet(
            pStrip.PStripAdd(lWindow.QWindowTabCreate(lTab), lTab.QWindowTabLayout),
            lTab.QWindowTabName);

    private void PWindowFunnelUpdate() => pStrip.QStrip.QStripTabs.ToList().ForEach(PWindowFunnelResolve);

    private void PWindowFunnelResolve(QStripTab lStripTab) =>
        pStrip.QStrip.QStripDeck.QDeckIndex.QDeckSurfaceRead<PTabSurface>(lStripTab)?.PTabTargetsResolve();

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
        pStrip.PStripSelected?.PTabViewer?.QViewer.QViewerSource.QViewerSourceOpen(pPath);

    private void PWindowTabAttach(QStripTab lTab, double pFlowHeight)
    {
        PTabSurface? pSurface = pStrip.QStrip.QStripDeck.QDeckIndex.QDeckSurfaceRead<PTabSurface>(lTab);
        lTab.QStripTabWorkspace.QWorkspaceFlowApply();
        pSurface?.PTabFlow?.PFlowLayoutApply(pFlowHeight);
        lTab.QStripTabWorkspace.QWorkspaceViewerApply();
        pSurface?.PTabWidthChanged += PWindowWidthApply;
        PWindowWidthApply();
    }

    private void PWindowTabDetach(QStripTab lTab)
    {
        pStrip.QStrip.QStripDeck.QDeckIndex.QDeckSurfaceRead<PTabSurface>(lTab)?.PTabWidthChanged -=
            PWindowWidthApply;
        lTab.QStripTabWorkspace.QWorkspaceCommandReset();
    }

    private void PWindowLayoutApply(bool pVertical)
    {
        pToolbar.PToolbarTabSet(null);
        PWindowRailSet(null);
        pConsole.PConsoleSceneSet(false);
        pToolbar.PToolbarSceneSet(null);
        pRail.PRailApply(pVertical);
        pToolbar.PToolbarVerticalSet(pVertical);
        PWindowColumnApply(pVertical);
    }

    private void PWindowRailSet(PRail? pContent) => pTabRailHost.Content = pContent;

    private void PWindowColumnApply(bool pVertical) => pTabRailColumn.Width = PLook.PLookRailWidth[pVertical];

    private void PWindowVerticalApply()
    {
        PWindowRailSet(pRail);
        pToolbar.PToolbarSceneSet(pConsole.PConsoleSceneRead());
        PWindowWidthApply();
    }

    private void PWindowHorizontalApply()
    {
        pToolbar.PToolbarTabSet(pRail);
        pConsole.PConsoleSceneSet(true);
        PWindowWidthApply();
    }

    private void PWindowFlowApply(double pFlowHeight)
    {
        pStrip.PStripSelected?.PTabFlow?.PFlowSelectedApply(pFlowHeight);
    }

    private void PWindowVolumeSet(double pVolume) =>
        pStrip.PStripSelected?.PTabViewer?.QViewer.QViewerPlayback.QViewerVolumeSet(pVolume);

    private void PWindowWidthApply()
    {
        (MinWidth, Width) = QWindowFrame.QWindowWidthResolve(
            pStrip.PStripSelected?.PTabWidthRead(),
            pTabRailColumn.Width.Value,
            Width);
    }

    private void PWindowListAdd(IReadOnlyList<string> pPaths) =>
        _ = pStrip.PStripSelected?.PTabList?.QList.QListPathsAdd(pPaths);

    private void PWindowViewerOpen(string pPath) => PWindowMediaOpen(pPath);

    private void PWindowPlay() => pStrip.PStripSelected?.PTabViewer?.QViewer.QViewerPlayback.QViewerPlay();

    private void PWindowPause() => pStrip.PStripSelected?.PTabViewer?.QViewer.QViewerPlayback.QViewerPause();

    private bool? PWindowUndo() =>
        pStrip.QStrip.QStripSelected?.QStripTabWorkspace.QWorkspaceHistory.QWorkspaceUndo();

    private bool? PWindowRedo() =>
        pStrip.QStrip.QStripSelected?.QStripTabWorkspace.QWorkspaceHistory.QWorkspaceRedo();

    private bool? PWindowMediaClear(IReadOnlySet<Guid> lCohorts) =>
        pStrip.QStrip.QStripSelected?.QStripTabWorkspace.QWorkspaceMedia.QWorkspaceMediaClear(lCohorts);

    private void PWindowExitHandle(object? sender, System.ComponentModel.CancelEventArgs eventArgs)
    {
        eventArgs.Cancel = !pStrip.QStrip.QStripClosure.QStripCloseConfirm();
    }

    private void PWindowPositionRestore(LFrameState lFrame)
    {
        QSashMaximize.QSashMaximizeAttach(this);
        QSashPlacement.QSashPlacementRestore(
            this,
            lFrame.LFrameLeft,
            lFrame.LFrameTop,
            lFrame.LFrameWidth,
            lFrame.LFrameHeight);
    }

    private void PWindowCloseHandle(object? sender, EventArgs eventArgs)
    {
        QWindowFrame.QWindowFrameSave(
            PLook.PLookNormal[WindowState],
            new QSashBounds(Left, Top, Width, Height),
            new QSashBounds(RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height));
        lWindow.QWindowClose();
        PWindowNoticesDetach();
        pWindowDrop.PDropHandlersRemove(this);
        pWindowResize.PResizeHandlersRemove();
        lWindow.QWindowTabSet(null, null);
        pStrip.QStrip.QStripDeck.QDeckAllClose();
        PWindowCloseDetach();
    }

    private void PWindowNoticesDetach()
    {
        QAskNotice.QAskRaised -= PWindowAskHandle;
        QWarningNotice.QWarningRaised -= PWindowWarningHandle;
        pStrip.QStrip.QStripSelectChanged -= PWindowTabHandle;
        pWindowShortcut.PShortcutDetach();
    }
}
