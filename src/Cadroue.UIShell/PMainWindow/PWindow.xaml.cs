using Cadroue.Infrastructure;
using System;
using System.Windows;
using System.Windows.Input;
using Cadroue.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIShell.PMainWindow;
public partial class PWindow : Window
{
    private const int PResizeBorderPixels = 8;
    private const int PResizeLeft = 1;
    private const int PResizeRight = 2;
    private const int PResizeTop = 4;
    private const int PResizeBottom = 8;
    private const int PWindowMessageErase = 0x0014;
    private const double PWindowFontSize = 13;
    private const int PWindowCornerPreference = 33;
    private const int PWindowCornerRound = 2;
    private const int PWindowCaptionColor = 35;
    private const int PWindowColorBackground = 0x00F7E8DC;
    private const double PWindowWidthFloor = 900;
    private readonly PStrip pStrip;
    private readonly PRail pRail;
    private bool pResizeActive;
    private int pResizeDirection;
    private Point pResizeStartPointer;
    private Rect pResizeStartBounds;
    private PFlowControl? pFlowActive; private PViewer? pViewerActive; private PList? pListActive;
    private PMainArea.PTabSurface? pWindowSurfaceActive;
    private bool pWindowAudioAllowed;
    public PWindow()
    {
        InitializeComponent();
        Title = LLocalization.LLocalizationTextRead("Program.Window.Title");
        pStrip = new PStrip();
        pRail = new PRail();
        pRail.PRailAttach(pStrip);
        LRelay? lRelayStartup = LRelayChannel.LRelayPayloadRead();
        if (lRelayStartup is null)
        {
            PWindowTabsRestore(pStrip, LPreference.LPreferenceStateCurrent, LScene.LSceneCurrent);
        }

        pControlBar.PToolbarTabSet(pRail);
        pControlBar.PToolbarOptionsApply += PWindowOptionsHandle;
        PWindowOptionsHandle(LPreference.LPreferenceStateCurrent);
        PWindowPositionRestore(LFrameStore.LFrameStateCurrent);
        pDeck.PDeckTabsetSet(pStrip);
        pStrip.PStripSelectChange += PWindowTabHandle;
        PWindowTabHandle(pStrip.PStripSelected);
        if (lRelayStartup is { } lRelayPayload)
        {
            PWindowRelayPlace(lRelayPayload);
            PWindowRelayAccept(lRelayPayload);
        }
        else
        {
            PWindowMediaRestore(LPreference.LPreferenceStateCurrent);
        }

        LRelayChannel.LRelayTabReceive += PWindowRelayHandle;
        PDropHandlersAdd();
        PResizeHandlersAdd();
        PreviewKeyDown += PShortcutKeyHandle;
        Closed += PWindowCloseHandle;
    }
    private static void PWindowTabsRestore(PStrip pTabset, LPreferenceState lPreferenceState, LSceneRecord lScene)
    {
        LTrace.LTraceLoadingSet(true);
        try
        {
            if (lPreferenceState.LPreferenceStartupMode == "DefaultTab")
            {
                foreach (string pStartupKey in lPreferenceState.LPreferenceStartupTabs)
                {
                    pTabset.PStripAdd(pStartupKey);
                }

                pTabset.PStripSelect(pTabset.PStripRecords[0]);
                return;
            }

            PWindowSceneRestore(pTabset, lScene);
        }
        finally
        {
            LTrace.LTraceLoadingSet(false);
        }
    }

    private static void PWindowSceneRestore(PStrip pTabset, LSceneRecord lScene)
    {
        IReadOnlyList<string> pTabKeys = lScene.LSceneLayoutKeys.Count > 0
            ? lScene.LSceneLayoutKeys
            : new[] { "Split", "Edit", "Audio", "Convert", "Merge", "Worklist" };
        IReadOnlyList<LPresetRecord> pTabExports = lScene.LSceneTabExports;
        IReadOnlyList<LSceneTabRecord> pTabLayouts = lScene.LSceneTabLayouts;
        pTabset.PStripUpdateSuspend();
        for (int pTabIndex = 0; pTabIndex < pTabKeys.Count; pTabIndex++)
        {
            LPreset? pTabExportState = pTabIndex < pTabExports.Count
                ? LPreset.LPresetStateCreate(pTabExports[pTabIndex])
                : null;
            LSceneTabRecord? pTabLayout = pTabIndex < pTabLayouts.Count ? pTabLayouts[pTabIndex] : null;
            PTabRecord pTabRestored = pTabset.PStripAdd(pTabKeys[pTabIndex], pTabExportState, pTabLayout);
            if (pTabIndex < lScene.LSceneTabNames.Count)
            {
                pTabset.PStripNameSet(pTabRestored, lScene.LSceneTabNames[pTabIndex]);
            }
        }
        pTabset.PStripUpdateResume();
        PWindowRelayApply(pTabset.PStripRecords, lScene.LSceneTabRelays);
        foreach (PTabRecord pTabRecord in pTabset.PStripRecords)
        {
            if (pTabRecord.PTabWorkspace.PWorkspaceSurface is PMainArea.PFunnelTab pFunnelSurface)
            {
                pFunnelSurface.PFunnelTargetsResolve(pTabset.PStripRecords);
            }
        }
        int pSelectIndex = Math.Clamp(lScene.LSceneTabIndex, 0, pTabset.PStripRecords.Count - 1);
        pTabset.PStripTitleUpdate();
        pTabset.PStripSelect(pTabset.PStripRecords[pSelectIndex]);
    }
    private void PWindowMediaRestore(LPreferenceState lPreferenceState)
    {
        if (!lPreferenceState.LPreferenceMediaAutomatic
            || string.IsNullOrWhiteSpace(lPreferenceState.LPreferenceMediaPath)
            || !System.IO.File.Exists(lPreferenceState.LPreferenceMediaPath))
        {
            return;
        }

        string pMediaPath = lPreferenceState.LPreferenceMediaPath;
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            new Action(() => pViewerActive?.PViewerSourceOpen(pMediaPath)));
    }

    private void PWindowRelayPlace(LRelay lRelay)
    {
        double pRelayLeft = lRelay.LRelayDropLeft - 120;
        double pRelayTop = lRelay.LRelayDropTop - 16;
        double pRelayRightLimit = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 200;
        double pRelayBottomLimit = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 120;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = Math.Clamp(pRelayLeft, SystemParameters.VirtualScreenLeft, pRelayRightLimit);
        Top = Math.Clamp(pRelayTop, SystemParameters.VirtualScreenTop, pRelayBottomLimit);
    }

    private void PWindowRelayAccept(LRelay lRelay)
    {
        PTabRecord pRelayTabRecord = pStrip.PStripAdd(
            lRelay.LRelayLayoutKey,
            LPreset.LPresetStateCreate(lRelay.LRelayExport),
            lRelay.LRelayLayout);
        if (!string.IsNullOrWhiteSpace(lRelay.LRelayCustomName))
        {
            pStrip.PStripNameSet(pRelayTabRecord, lRelay.LRelayCustomName);
        }

        pStrip.PStripSelect(pRelayTabRecord);
        pRelayTabRecord.PTabWorkspace.PWorkspaceRelayApply(lRelay);
    }

    private void PWindowRelayHandle(LRelay lRelay)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            PWindowRelayAccept(lRelay);
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }));
    }

    private void PWindowPositionRestore(LFrameState lFrame)
    {
        if (lFrame.LFrameLeft is not double pLeft || lFrame.LFrameTop is not double pTop)
            return;
        double pVRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        double pVBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
        if (pLeft >= SystemParameters.VirtualScreenLeft && pTop >= SystemParameters.VirtualScreenTop
            && pLeft + 100 <= pVRight && pTop + 40 <= pVBottom)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = pLeft;
            Top = pTop;
        }
    }
    private void PWindowTabHandle(PTabRecord? pTabRecord)
    {
        PWindowWidthDetach();
        PWindowWorkspaceDetach();
        if (pTabRecord is null)
        {
            return;
        }
        pFlowActive = pTabRecord.PTabWorkspace.PWorkspaceFlow;
        pViewerActive = pTabRecord.PTabWorkspace.PWorkspaceViewer;
        pListActive = pTabRecord.PTabWorkspace.PWorkspaceList;
        pWindowAudioAllowed = pTabRecord.PTabLayoutKey == "Audio";
        PWindowWorkspaceAttach(pTabRecord);
        PWindowWidthAttach(pTabRecord.PTabWorkspace.PWorkspaceSurface);
    }

    private void PWindowWidthAttach(PMainArea.PTabSurface pWindowSurface)
    {
        pWindowSurfaceActive = pWindowSurface;
        pWindowSurfaceActive.PTabWidthChange += PWindowWidthHandle;
        if (pListActive is not null)
        {
            pListActive.PListMinimizeChange += PWindowListHandle;
        }

        PWindowWidthApply();
    }

    private void PWindowWidthDetach()
    {
        if (pWindowSurfaceActive is not null)
        {
            pWindowSurfaceActive.PTabWidthChange -= PWindowWidthHandle;
            pWindowSurfaceActive = null;
        }

        if (pListActive is not null)
        {
            pListActive.PListMinimizeChange -= PWindowListHandle;
        }
    }

    private void PWindowListHandle(bool pWindowListMinimized) => PWindowWidthApply();

    private void PWindowWidthHandle() => PWindowWidthApply();

    private void PWindowWidthApply()
    {
        double pWindowRequired = pWindowSurfaceActive?.PTabWidthRead() ?? 0;
        double pWindowContentMinimum = Math.Max(PWindowWidthFloor, pWindowRequired);
        double pWindowReservedWidth = pTabRailColumn.Width.IsAbsolute
            ? pTabRailColumn.Width.Value
            : 0;
        MinWidth = pWindowContentMinimum + pWindowReservedWidth;
        if (Width < MinWidth)
        {
            Width = MinWidth;
        }
    }
    private void PWindowWorkspaceAttach(PTabRecord pTabRecord)
    {
        if (pFlowActive is null || pViewerActive is null)
        {
            return;
        }
        pFlowActive.PFlowCommandSet(true);
        pFlowActive.PFlowSectionShow(pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabSectionShow);
        pFlowActive.Height = LFrameStore.LFrameStateCurrent.LFrameFlowHeight;
        pFlowActive.PFlowOrderApply();
        pFlowActive.PFlowPlayingSource = pViewerActive.PViewerPlayingRead;
        pViewerActive.PViewerCommandSet(true);
        pViewerActive.PViewerMediaChange += PWindowMediaHandle;
        pViewerActive.PViewerClockTick += PWindowClockHandle;
        pViewerActive.PViewerKeyDispatch += PShortcutViewerHandle;
        pFlowActive.PFlowCursorChange += pViewerActive.PViewerSeek;
        pFlowActive.PFlowPlay += pViewerActive.PViewerPlay;
        pFlowActive.PFlowPause += pViewerActive.PViewerPause;
        pFlowActive.PFlowVolumeChange += pViewerActive.PViewerVolumeSet;
        PWindowVolumeSync(LPreference.LPreferenceStateCurrent);
    }
    private void PWindowWorkspaceDetach()
    {
        if (pFlowActive is not null && pViewerActive is not null)
        {
            pViewerActive.PViewerMediaChange -= PWindowMediaHandle;
            pViewerActive.PViewerClockTick -= PWindowClockHandle;
            pViewerActive.PViewerKeyDispatch -= PShortcutViewerHandle;
            pFlowActive.PFlowCursorChange -= pViewerActive.PViewerSeek;
            pFlowActive.PFlowPlay -= pViewerActive.PViewerPlay;
            pFlowActive.PFlowPause -= pViewerActive.PViewerPause;
            pFlowActive.PFlowVolumeChange -= pViewerActive.PViewerVolumeSet;
            pFlowActive.PFlowPlayingSource = null;
            pFlowActive.PFlowSectionShow(false);
            pFlowActive.PFlowCommandSet(false);
            pViewerActive.PViewerCommandSet(false);
        }
        pFlowActive = null;
        pViewerActive = null;
    }
    private void PWindowMediaHandle(LCargo mediaStatus)
    {
        if (mediaStatus.LCargoMediaInfo is LMediaInfo mediaInfo)
        {
            pFlowActive?.PFlowAttach(mediaInfo, mediaStatus.LCargoSourcePath, TimeSpan.Zero);
            return;
        }
        pFlowActive?.PFlowClear();
    }
    private void PWindowClockHandle(TimeSpan playbackPosition)
    {
        pFlowActive?.PFlowCursorUpdate(playbackPosition);
    }
    private void PWindowOptionsHandle(LPreferenceState lPreferenceState)
    {
        Width = LFrameStore.LFrameStateCurrent.LFrameWidth;
        Height = LFrameStore.LFrameStateCurrent.LFrameHeight;
        FontSize = PWindowFontSize;
        PWindowLayoutApply(lPreferenceState.LPreferenceVerticalTabs);
        if (pFlowActive is not null)
        {
            pFlowActive.Height = LFrameStore.LFrameStateCurrent.LFrameFlowHeight;
            pFlowActive.PFlowOrderApply();
            pFlowActive.PFlowPaletteApply();
        }
        PWindowVolumeSync(lPreferenceState);
    }

    private void PWindowLayoutApply(bool pVertical)
    {
        UIElement pSceneControls = pConsole.PConsoleSceneRead();
        pControlBar.PToolbarTabSet(null);
        pTabRailHost.Content = null;
        pConsole.PConsoleSceneSet(null);
        pControlBar.PToolbarSceneSet(null);

        pRail.PRailApply(pVertical);
        pControlBar.PToolbarVerticalSet(pVertical);
        pTabRailColumn.Width = new GridLength(pVertical ? PRail.PRailWidth : 0);
        if (pVertical)
        {
            pTabRailHost.Content = pRail;
            pControlBar.PToolbarSceneSet(pSceneControls);
        }
        else
        {
            pControlBar.PToolbarTabSet(pRail);
            pConsole.PConsoleSceneSet(pSceneControls);
        }

        PWindowWidthApply();
    }
    private void PWindowVolumeSync(LPreferenceState lPreferenceState)
    {
        if (pFlowActive is null || pViewerActive is null) return;
        double pVolume = lPreferenceState.LPreferenceVolumeUnified ? lPreferenceState.LPreferenceVolume : pViewerActive.PViewerVolumeCurrent;
        if (lPreferenceState.LPreferenceVolumeUnified) pViewerActive.PViewerVolumeSet(pVolume);
        pFlowActive.PFlowVolumeSet(pVolume);
    }
    private void PWindowCloseHandle(object? sender, EventArgs eventArgs)
    {
        Rect lBounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (lBounds.Width > 0 && lBounds.Height > 0)
        {
            LFrameStore.LFrameSave(new LFrameState
            {
                LFrameLeft = lBounds.Left,
                LFrameTop = lBounds.Top,
                LFrameWidth = lBounds.Width,
                LFrameHeight = lBounds.Height,
                LFrameFlowHeight = LFrameStore.LFrameStateCurrent.LFrameFlowHeight
            });
        }

        LScene.LSceneStateSave(PWindowSceneRead(LScene.LSceneActiveName));
        LRelayChannel.LRelayTabReceive -= PWindowRelayHandle;
        pStrip.PStripSelectChange -= PWindowTabHandle;
        pControlBar.PToolbarOptionsApply -= PWindowOptionsHandle;
        PreviewKeyDown -= PShortcutKeyHandle;
        PDropHandlersRemove();
        PResizeHandlersRemove();
        PWindowWidthDetach();
        PWindowWorkspaceDetach();
        foreach (PTabRecord pTabRecord in pStrip.PStripRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceClose();
        }
        Closed -= PWindowCloseHandle;
    }
}
