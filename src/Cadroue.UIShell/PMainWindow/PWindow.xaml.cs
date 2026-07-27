using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
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
    private const int PWindowDwmCornerPreference = 33;
    private const int PWindowDwmCornerRound = 2;
    private const int PWindowDwmCaptionColor = 35;
    private const int PWindowColorBackground = 0x00F7E8DC;
    private readonly LTabset lTabset;
    private bool pResizeActive;
    private int pResizeDirection;
    private Point pResizeStartPointer;
    private Rect pResizeStartBounds;
    private PFlowControl? pFlowActive;
    private PViewer? pViewerActive;
    private bool pWindowAudioAllowed;
    public PWindow()
    {
        InitializeComponent();
        lTabset = new LTabset();
        PWindowTabsRestore(lTabset, App.LPreferenceStateCurrent);
        pControlBar.PToolbarTabsetSet(lTabset);
        pControlBar.PToolbarPreferenceApply += PWindowPreferenceHandle;
        PWindowPreferenceHandle(App.LPreferenceStateCurrent);
        PWindowPositionRestore(App.LPreferenceStateCurrent);
        pDeck.PDeckTabsetSet(lTabset);
        lTabset.LTabsetSelectChange += PWindowTabHandle;
        PWindowTabHandle(lTabset.PTabsetSelectRecord);
        PDropHandlersAdd();
        PResizeHandlersAdd();
        PreviewKeyDown += PShortcutKeyHandle;
        Closed += PWindowCloseHandle;
    }
    private static void PWindowTabsRestore(LTabset pTabset, LPreferenceState lPreferenceState)
    {
        IReadOnlyList<string> pTabKeys = lPreferenceState.LPreferenceTabLayoutKeys.Count > 0
            ? lPreferenceState.LPreferenceTabLayoutKeys
            : new[] { "Split", "Edit", "Audio", "Convert", "Merge", "Worklist" };
        IReadOnlyList<LExportSpecificPresetRecord> pTabExports = lPreferenceState.LPreferenceTabExports;
        for (int pTabIndex = 0; pTabIndex < pTabKeys.Count; pTabIndex++)
        {
            // Index-aligned with the layout keys. A shorter list (preferences written by
            // an older build) leaves the remaining tabs on default export settings.
            LExportSpecificState? pTabExportState = pTabIndex < pTabExports.Count
                ? pTabExports[pTabIndex].LPresetStateCreate()
                : null;
            pTabset.LTabsetAdd(pTabKeys[pTabIndex], pTabExportState);
        }
        int pSelectIndex = Math.Clamp(lPreferenceState.LPreferenceTabSelectIndex, 0, pTabset.PTabsetRecords.Count - 1);
        pTabset.LTabsetSelect(pTabset.PTabsetRecords[pSelectIndex]);
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
        PWindowWorkspaceDetach();
        if (pTabRecord is null)
        {
            return;
        }
        pFlowActive = pTabRecord.PTabWorkspace.PWorkspaceFlow;
        pViewerActive = pTabRecord.PTabWorkspace.PWorkspaceViewer;
        pWindowAudioAllowed = pTabRecord.PTabLayoutKey == "Audio";
        PWindowWorkspaceAttach(pTabRecord);
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
    private void PShortcutKeyHandle(object sender, KeyEventArgs e)
    {
        if (PWindowInputFind(e.OriginalSource as DependencyObject))
        {
            return;
        }
        bool pHandled = PShortcutDispatch(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers);
        if (pHandled)
        {
            e.Handled = true;
        }
    }
    private bool PShortcutDispatch(Key pKey, ModifierKeys pModifiers)
    {
        if (pModifiers == ModifierKeys.Control && (pKey == Key.OemQuestion || pKey == Key.Divide))
        {
            pControlBar.PToolbarShortcutShow();
            return true;
        }
        if (pModifiers != ModifierKeys.None)
        {
            return false;
        }
        return pKey switch
        {
            Key.Space => PShortcutPlayToggle(),
            Key.C => pFlowActive?.PFlowShortcutDispatch("zoomIn") == true,
            Key.V => pFlowActive?.PFlowShortcutDispatch("zoomOut") == true,
            Key.Q => pFlowActive?.PFlowShortcutDispatch("addSection") == true,
            Key.D => pFlowActive?.PFlowShortcutDispatch("setStart") == true,
            Key.S => pFlowActive?.PFlowShortcutDispatch("splitSection") == true,
            Key.F => pFlowActive?.PFlowShortcutDispatch("setEnd") == true,
            Key.Delete => pFlowActive?.PFlowShortcutDispatch("deleteSection") == true,
            Key.E => pFlowActive?.PFlowShortcutDispatch("previousKey") == true,
            Key.W => pFlowActive?.PFlowShortcutDispatch("nearestKey") == true,
            Key.R => pFlowActive?.PFlowShortcutDispatch("nextKey") == true,
            _ => false
        };
    }
    private bool PShortcutPlayToggle()
    {
        if (pViewerActive is null)
        {
            return false;
        }
        if (pViewerActive.LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            pViewerActive.PViewerPause();
        }
        else
        {
            pViewerActive.PViewerPlay();
        }
        return true;
    }
    private static bool PWindowInputFind(DependencyObject? pSource)
    {
        while (pSource is not null)
        {
            if (pSource is TextBoxBase || pSource is PasswordBox)
            {
                return true;
            }
            pSource = VisualTreeHelper.GetParent(pSource);
        }
        return false;
    }
    private void PWindowPreferenceHandle(LPreferenceState lPreferenceState)
    {
        Width = lPreferenceState.LPreferenceProgramWidth;
        Height = lPreferenceState.LPreferenceProgramHeight;
        FontSize = lPreferenceState.LPreferenceFontSize;
        if (pFlowActive is not null)
            pFlowActive.Height = lPreferenceState.LPreferenceFlowHeight;
        PWindowVolumeSync(lPreferenceState);
    }
    private void PWindowVolumeSync(LPreferenceState lPreferenceState)
    {
        if (pFlowActive is null || pViewerActive is null) return;
        double pVolume = lPreferenceState.LPreferenceVolumeSingleGlobal ? lPreferenceState.LPreferenceVolume : pViewerActive.PViewerVolumeCurrent;
        if (lPreferenceState.LPreferenceVolumeSingleGlobal) pViewerActive.PViewerVolumeSet(pVolume);
        pFlowActive.PFlowVolumeSet(pVolume);
    }
    private void PDropHandlersAdd()
    {
        AddHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropAccept), true);
        AddHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropAccept), true);
        AddHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle), true);
    }
    private void PDropHandlersRemove()
    {
        RemoveHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropAccept));
        RemoveHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropAccept));
        RemoveHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle));
    }
    private void PResizeHandlersAdd()
    {
        PreviewMouseMove += PResizeMoveHandle;
        PreviewMouseLeftButtonDown += PResizePressHandle;
        PreviewMouseLeftButtonUp += PResizeReleaseHandle;
        LostMouseCapture += PResizeCaptureHandle;
    }
    private void PResizeHandlersRemove()
    {
        PreviewMouseMove -= PResizeMoveHandle;
        PreviewMouseLeftButtonDown -= PResizePressHandle;
        PreviewMouseLeftButtonUp -= PResizeReleaseHandle;
        LostMouseCapture -= PResizeCaptureHandle;
    }
    private void PResizePressHandle(object sender, MouseButtonEventArgs e)
    {
        int pDirection = PResizeDirectionRead(e.GetPosition(this));
        if (WindowState != WindowState.Normal || pDirection == 0)
            return;
        pResizeActive = true;
        pResizeDirection = pDirection;
        pResizeStartPointer = PResizePointerRead(e);
        pResizeStartBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        // Drop to software rendering for the drag so WPF and the Flyleaf DirectX surface no longer
        // fight over the window each frame (airspace flicker); hardware rendering restores on release.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        Mouse.Capture(this);
        e.Handled = true;
    }
    private void PResizeMoveHandle(object sender, MouseEventArgs e)
    {
        if (pResizeActive)
        {
            PResizeApply(PResizePointerRead(e));
            e.Handled = true;
            return;
        }
        int pDirection = WindowState == WindowState.Normal ? PResizeDirectionRead(e.GetPosition(this)) : 0;
        Cursor = pDirection == 0 ? null : PResizeCursorRead(pDirection);
    }
    private void PResizeReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        if (!pResizeActive)
            return;
        pResizeActive = false;
        RenderOptions.ProcessRenderMode = RenderMode.Default;
        Mouse.Capture(null);
        e.Handled = true;
    }
    private void PResizeCaptureHandle(object sender, MouseEventArgs e)
    {
        pResizeActive = false;
        RenderOptions.ProcessRenderMode = RenderMode.Default;
    }
    private int PResizeDirectionRead(Point pPoint)
    {
        bool pLeft = pPoint.X >= 0 && pPoint.X < PResizeBorderPixels;
        bool pRight = pPoint.X <= ActualWidth && pPoint.X > ActualWidth - PResizeBorderPixels;
        bool pTop = pPoint.Y >= 0 && pPoint.Y < PResizeBorderPixels;
        bool pBottom = pPoint.Y <= ActualHeight && pPoint.Y > ActualHeight - PResizeBorderPixels;
        int pDirection = 0;
        if (pLeft) pDirection |= PResizeLeft;
        if (pRight) pDirection |= PResizeRight;
        if (pTop) pDirection |= PResizeTop;
        if (pBottom) pDirection |= PResizeBottom;
        return pDirection;
    }
    private static Cursor PResizeCursorRead(int pDirection)
    {
        bool pHorizontal = (pDirection & (PResizeLeft | PResizeRight)) != 0;
        bool pVertical = (pDirection & (PResizeTop | PResizeBottom)) != 0;
        if (!pHorizontal || !pVertical)
            return pHorizontal ? Cursors.SizeWE : Cursors.SizeNS;
        bool pLeft = (pDirection & PResizeLeft) != 0;
        bool pTop = (pDirection & PResizeTop) != 0;
        return pLeft == pTop ? Cursors.SizeNWSE : Cursors.SizeNESW;
    }
    private Point PResizePointerRead(MouseEventArgs e)
    {
        Point pScreenPoint = PointToScreen(e.GetPosition(this));
        return PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice.Transform(pScreenPoint) ?? pScreenPoint;
    }
    private void PResizeApply(Point pPointer)
    {
        double pDx = pPointer.X - pResizeStartPointer.X;
        double pDy = pPointer.Y - pResizeStartPointer.Y;
        double pLeft = pResizeStartBounds.Left;
        double pTop = pResizeStartBounds.Top;
        double pWidth = pResizeStartBounds.Width;
        double pHeight = pResizeStartBounds.Height;
        if ((pResizeDirection & PResizeLeft) != 0)
        {
            pWidth = Math.Max(MinWidth, pResizeStartBounds.Width - pDx);
            pLeft = pResizeStartBounds.Right - pWidth;
        }
        if ((pResizeDirection & PResizeRight) != 0)
            pWidth = Math.Max(MinWidth, pResizeStartBounds.Width + pDx);
        if ((pResizeDirection & PResizeTop) != 0)
        {
            pHeight = Math.Max(MinHeight, pResizeStartBounds.Height - pDy);
            pTop = pResizeStartBounds.Bottom - pHeight;
        }
        if ((pResizeDirection & PResizeBottom) != 0)
            pHeight = Math.Max(MinHeight, pResizeStartBounds.Height + pDy);
        Left = pLeft;
        Top = pTop;
        Width = pWidth;
        Height = pHeight;
    }
    private void PDropAccept(object sender, DragEventArgs dragEvent)
    {
        dragEvent.Effects = PDropEffectRead(dragEvent);
        dragEvent.Handled = true;
    }
    private void PDropHandle(object sender, DragEventArgs dragEvent)
    {
        DragDropEffects dropEffect = PDropEffectRead(dragEvent);
        dragEvent.Effects = dropEffect;
        dragEvent.Handled = true;
        if (dropEffect == DragDropEffects.None || pViewerActive is null)
        {
            return;
        }
        string? sourcePath = PDropPathRead(dragEvent);
        if (sourcePath is null)
        {
            dragEvent.Effects = DragDropEffects.None;
            return;
        }
        pViewerActive.PViewerSourceOpen(sourcePath);
    }
    private DragDropEffects PDropEffectRead(DragEventArgs dragEvent)
    {
        if (pViewerActive is null)
        {
            return DragDropEffects.None;
        }
        string? pSourcePath = PDropPathRead(dragEvent);
        if (pSourcePath is null || PDropAudioCheck(pSourcePath) && !pWindowAudioAllowed)
        {
            return DragDropEffects.None;
        }
        if ((dragEvent.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
        {
            return DragDropEffects.Copy;
        }
        if ((dragEvent.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move)
        {
            return DragDropEffects.Move;
        }
        if ((dragEvent.AllowedEffects & DragDropEffects.Link) == DragDropEffects.Link)
        {
            return DragDropEffects.Link;
        }
        return DragDropEffects.None;
    }
    private static bool PDropAudioCheck(string pSourcePath)
    {
        string pExtension = Path.GetExtension(pSourcePath);
        return pExtension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".aac", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".flac", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
    }
    private static string? PDropPathRead(DragEventArgs dragEvent)
    {
        if (!dragEvent.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }
        if (dragEvent.Data.GetData(DataFormats.FileDrop) is not string[] sourcePaths)
        {
            return null;
        }
        foreach (string sourcePath in sourcePaths)
        {
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }
        return null;
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
        lPrefs.LPreferenceTabSelectIndex = lTabset.PTabsetSelectRecord is null ? 0
            : Math.Max(0, lTabset.PTabsetRecords.IndexOf(lTabset.PTabsetSelectRecord));
        App.LPreferenceStateSet(lPrefs);
        lTabset.LTabsetSelectChange -= PWindowTabHandle;
        pControlBar.PToolbarPreferenceApply -= PWindowPreferenceHandle;
        PreviewKeyDown -= PShortcutKeyHandle;
        PDropHandlersRemove();
        PResizeHandlersRemove();
        PWindowWorkspaceDetach();
        foreach (PTabRecord pTabRecord in lTabset.PTabsetRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceClose();
        }
        Closed -= PWindowCloseHandle;
    }
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        HwndSource? pWindowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        pWindowSource?.AddHook(PWindowMessageHook);
        PWindowDwmApply();
    }
    private IntPtr PWindowMessageHook(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Swallow WM_ERASEBKGND so Windows does not repaint the background brush over the client
        // area on every resize step; WPF still renders the full tree, so nothing is lost. This
        // removes the whole-window flicker while the borderless window is resized.
        if (message == PWindowMessageErase)
        {
            handled = true;
            return new IntPtr(1);
        }
        return IntPtr.Zero;
    }
    private void PWindowDwmApply()
    {
        IntPtr pWindowHandle = new WindowInteropHelper(this).Handle;
        if (pWindowHandle == IntPtr.Zero)
        {
            return;
        }
        int pWindowCornerPreference = PWindowDwmCornerRound;
        _ = DwmSetWindowAttribute(
            pWindowHandle,
            PWindowDwmCornerPreference,
            ref pWindowCornerPreference,
            Marshal.SizeOf<int>());
        int pWindowCaptionColor = PWindowColorBackground;
        _ = DwmSetWindowAttribute(
            pWindowHandle,
            PWindowDwmCaptionColor,
            ref pWindowCaptionColor,
            Marshal.SizeOf<int>());
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);
}
