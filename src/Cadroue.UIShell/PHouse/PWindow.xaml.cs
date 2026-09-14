using Cadroue.Infrastructure;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.Media;
using Cadroue.UIShell.PToolbar;
using Cadroue.UIShell.PPanel;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIShell.PHouse;
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
    private string? pWindowRestorePath;
    private bool pResizeActive;
    private int pResizeDirection;
    private Point pResizeStartPointer;
    private Rect pResizeStartBounds;
    private PFlowControl? pFlowActive; private PViewer? pViewerActive; private PList? pListActive;
    private PDeck.PTabSurface? pWindowSurfaceActive;
    private bool pWindowAudioAllowed;
    public PWindow()
    {
        InitializeComponent();
        Title = LLocalization.LLocalizationTextRead("Program.Window.Title");
        pStrip = new PStrip();
        pRail = new PRail();
        pRail.PRailAttach(pStrip);
        Width = LFrameStore.LFrameStateCurrent.LFrameWidth;
        Height = LFrameStore.LFrameStateCurrent.LFrameHeight;
        LRelay? lRelayStartup = LRelayChannel.LRelayPayloadRead();
        if (lRelayStartup is null)
        {
            PWindowTabsRestore(pStrip, LPreference.LPreferenceStateCurrent, LScene.LSceneCurrent);
        }

        pToolbar.PToolbarTabSet(pRail);
        pToolbar.PToolbarOptionsApply += PWindowOptionsHandle;
        PWindowOptionsHandle(LPreference.LPreferenceStateCurrent);
        PWindowPositionRestore(LFrameStore.LFrameStateCurrent);
        pDeck.PDeckTabsetSet(pStrip);
        pStrip.PStripSelectChange += PWindowTabHandle;
        PWindowTabHandle(pStrip.PStripSelected);
        if (lRelayStartup is { } lRelayPayload)
        {
            PWindowRelayPlace(lRelayPayload);
            PWindowRelayAccept(lRelayPayload);
            LRelayChannel.LRelayStartupCommit();
        }
        else
        {
            PWindowMediaRestore(LPreference.LPreferenceStateCurrent);
        }

        LRelayChannel.LRelayAcceptSeam = PWindowRelayHandle;
        PDropHandlersAdd();
        PResizeHandlersAdd();
        Closing += PWindowExitHandle;
        Closed += PWindowCloseHandle;
    }

    private void PWindowExitHandle(object? sender, System.ComponentModel.CancelEventArgs eventArgs)
    {
        eventArgs.Cancel = !pStrip.PStripCloseConfirm(this);
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
        LRelayChannel.LRelayAcceptSeam = null;
        pStrip.PStripSelectChange -= PWindowTabHandle;
        pToolbar.PToolbarOptionsApply -= PWindowOptionsHandle;
        ComponentDispatcher.ThreadPreprocessMessage -= PShortcutMessageHandle;
        PDropHandlersRemove();
        PResizeHandlersRemove();
        PWindowWidthDetach();
        PWindowWorkspaceDetach();
        foreach (PTabRecord pTabRecord in pStrip.PStripRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceClose();
        }
        Closing -= PWindowExitHandle;
        Closed -= PWindowCloseHandle;
    }
}
