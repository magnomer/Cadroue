using System;
using System.Windows;
using System.Windows.Input;
using Cadroue.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
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
    private const int PWindowDwmCornerPreference = 33;
    private const int PWindowDwmCornerRound = 2;
    private const int PWindowDwmCaptionColor = 35;
    private const int PWindowColorBackground = 0x00F7E8DC;
    private const double PWindowWidthFloor = 900;
    private readonly LTabset lTabset;
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
        lTabset = new LTabset();
        LRelay? lRelayStartup = App.LRelayStartupTake();
        if (lRelayStartup is null)
        {
            PWindowTabsRestore(lTabset, App.LPreferenceStateCurrent);
        }

        pControlBar.PToolbarTabsetSet(lTabset);
        pControlBar.PToolbarOptionsApply += PWindowOptionsHandle;
        PWindowOptionsHandle(App.LPreferenceStateCurrent);
        PWindowPositionRestore(App.LPreferenceStateCurrent);
        pDeck.PDeckTabsetSet(lTabset);
        lTabset.LTabsetSelectChange += PWindowTabHandle;
        PWindowTabHandle(lTabset.PTabsetSelectRecord);
        if (lRelayStartup is { } lRelayPayload)
        {
            PWindowRelayPlace(lRelayPayload);
            PWindowRelayAdopt(lRelayPayload);
        }
        else
        {
            PWindowMediaRestore(App.LPreferenceStateCurrent);
        }

        LRelayChannel.LRelayTabReceive += PWindowRelayHandle;
        PDropHandlersAdd();
        PResizeHandlersAdd();
        PreviewKeyDown += PShortcutKeyHandle;
        Closed += PWindowCloseHandle;
    }
    private static void PWindowTabsRestore(LTabset pTabset, LPreferenceState lPreferenceState)
    {
        if (lPreferenceState.LPreferenceStartupMode == "DefaultTab")
        {
            foreach (string pStartupKey in lPreferenceState.LPreferenceStartupTabs)
            {
                pTabset.LTabsetAdd(pStartupKey);
            }

            pTabset.LTabsetSelect(pTabset.PTabsetRecords[0]);
            return;
        }

        IReadOnlyList<string> pTabKeys = lPreferenceState.LPreferenceTabLayoutKeys.Count > 0
            ? lPreferenceState.LPreferenceTabLayoutKeys
            : new[] { "Split", "Edit", "Audio", "Convert", "Merge", "Worklist" };
        IReadOnlyList<LExportSpecificPresetRecord> pTabExports = lPreferenceState.LPreferenceTabExports;
        IReadOnlyList<LPreferenceTabLayoutRecord> pTabLayouts = lPreferenceState.LPreferenceTabLayouts;
        for (int pTabIndex = 0; pTabIndex < pTabKeys.Count; pTabIndex++)
        {
            LExportSpecificState? pTabExportState = pTabIndex < pTabExports.Count
                ? pTabExports[pTabIndex].LPresetStateCreate()
                : null;
            LPreferenceTabLayoutRecord? pTabLayout = pTabIndex < pTabLayouts.Count ? pTabLayouts[pTabIndex] : null;
            pTabset.LTabsetAdd(pTabKeys[pTabIndex], pTabExportState, pTabLayout);
        }
        int pSelectIndex = Math.Clamp(lPreferenceState.LPreferenceTabSelectIndex, 0, pTabset.PTabsetRecords.Count - 1);
        pTabset.LTabsetSelect(pTabset.PTabsetRecords[pSelectIndex]);
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
        double pRelayLeft = lRelay.DropLeft - 120;
        double pRelayTop = lRelay.DropTop - 16;
        double pRelayRightLimit = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 200;
        double pRelayBottomLimit = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 120;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = Math.Clamp(pRelayLeft, SystemParameters.VirtualScreenLeft, pRelayRightLimit);
        Top = Math.Clamp(pRelayTop, SystemParameters.VirtualScreenTop, pRelayBottomLimit);
    }

    private void PWindowRelayAdopt(LRelay lRelay)
    {
        PTabRecord pRelayTabRecord = lTabset.LTabsetAdd(
            lRelay.LayoutKey,
            lRelay.LRelayExportCreate(),
            lRelay.Layout);
        lTabset.LTabsetSelect(pRelayTabRecord);
        pRelayTabRecord.PTabWorkspace.PWorkspaceRelayApply(lRelay);
    }

    private void PWindowRelayHandle(LRelay lRelay)
    {
        PWindowRelayAdopt(lRelay);
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void PWindowPositionRestore(LPreferenceState lPrefs)
    {
        if (lPrefs.LPreferenceProgramLeft is not double pLeft || lPrefs.LPreferenceProgramTop is not double pTop)
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
            pListActive.PListMinimizeChange += PWindowListWidthHandle;
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
            pListActive.PListMinimizeChange -= PWindowListWidthHandle;
        }
    }

    private void PWindowListWidthHandle(bool pWindowListMinimized) => PWindowWidthApply();

    private void PWindowWidthHandle() => PWindowWidthApply();

    private void PWindowWidthApply()
    {
        double pWindowRequired = pWindowSurfaceActive?.PTabWidthRead() ?? 0;
        if (pWindowRequired <= 0)
        {
            MinWidth = PWindowWidthFloor;
            return;
        }

        MinWidth = Math.Max(PWindowWidthFloor, pWindowRequired);
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
        pFlowActive.PFlowSectionShow(pTabRecord.PTabLayoutKey == "Split");
        pFlowActive.Height = App.LPreferenceStateCurrent.LPreferenceFlowHeight;
        pFlowActive.PFlowOrderApply();
        pFlowActive.PFlowPlayingSource = pViewerActive.PViewerPlayingRead;
        pViewerActive.PViewerCommandSet(true);
        pViewerActive.PViewerMediaChange += PWindowMediaHandle;
        pViewerActive.PViewerClockTick += PWindowClockHandle;
        pFlowActive.PFlowCursorChange += pViewerActive.PViewerSeek;
        pFlowActive.PFlowPlay += pViewerActive.PViewerPlay;
        pFlowActive.PFlowPause += pViewerActive.PViewerPause;
        pFlowActive.PFlowVolumeChange += pViewerActive.PViewerVolumeSet;
        PWindowVolumeSync(App.LPreferenceStateCurrent);
    }
    private void PWindowWorkspaceDetach()
    {
        if (pFlowActive is not null && pViewerActive is not null)
        {
            pViewerActive.PViewerMediaChange -= PWindowMediaHandle;
            pViewerActive.PViewerClockTick -= PWindowClockHandle;
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
    private void PWindowMediaHandle(LMediaOpenStatus mediaStatus)
    {
        if (mediaStatus.LMediaOpenMediaInfo is LMediaInfo mediaInfo)
        {
            pFlowActive?.PFlowAttach(mediaInfo, mediaStatus.LMediaOpenSourcePath, TimeSpan.Zero);
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
        Width = lPreferenceState.LPreferenceProgramWidth;
        Height = lPreferenceState.LPreferenceProgramHeight;
        FontSize = PWindowFontSize;
        if (pFlowActive is not null)
        {
            pFlowActive.Height = lPreferenceState.LPreferenceFlowHeight;
            pFlowActive.PFlowOrderApply();
            pFlowActive.PFlowPaletteApply();
        }
        PWindowVolumeSync(lPreferenceState);
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
        LPreferenceState lPrefs = App.LPreferenceStateCurrent.LPreferenceClone();
        Rect lBounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (lBounds.Width > 0 && lBounds.Height > 0)
        {
            lPrefs.LPreferenceProgramLeft = lBounds.Left;
            lPrefs.LPreferenceProgramTop = lBounds.Top;
            lPrefs.LPreferenceProgramWidth = lBounds.Width;
            lPrefs.LPreferenceProgramHeight = lBounds.Height;
        }
        lPrefs.LPreferenceTabLayoutKeys = lTabset.PTabsetRecords.Select(r => r.PTabLayoutKey).ToList();
        lPrefs.LPreferenceTabExports = lTabset.PTabsetRecords
            .Select(r => LExportSpecificPresetRecord.LPresetRecordCreate(r.PTabWorkspace.PWorkspaceExportState))
            .ToList();
        lPrefs.LPreferenceTabLayouts = lTabset.PTabsetRecords.Select(r => r.PTabWorkspace.PWorkspaceLayoutRead()).ToList();
        lPrefs.LPreferenceTabSelectIndex = lTabset.PTabsetSelectRecord is null ? 0
            : Math.Max(0, lTabset.PTabsetRecords.IndexOf(lTabset.PTabsetSelectRecord));
        App.LPreferenceStateSet(lPrefs);
        LRelayChannel.LRelayTabReceive -= PWindowRelayHandle;
        lTabset.LTabsetSelectChange -= PWindowTabHandle;
        pControlBar.PToolbarOptionsApply -= PWindowOptionsHandle;
        PreviewKeyDown -= PShortcutKeyHandle;
        PDropHandlersRemove();
        PResizeHandlersRemove();
        PWindowWidthDetach();
        PWindowWorkspaceDetach();
        foreach (PTabRecord pTabRecord in lTabset.PTabsetRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceClose();
        }
        Closed -= PWindowCloseHandle;
    }
}
