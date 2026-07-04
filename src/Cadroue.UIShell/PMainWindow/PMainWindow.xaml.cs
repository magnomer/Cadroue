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
public partial class PMainWindow : Window
{
    private const int PMainWindowResizeBorderPixels = 8;
    private const int PMainWindowResizeLeft = 1;
    private const int PMainWindowResizeRight = 2;
    private const int PMainWindowResizeTop = 4;
    private const int PMainWindowResizeBottom = 8;
    private const int PMainWindowDwmWindowCornerPreference = 33;
    private const int PMainWindowDwmWindowCornerRound = 2;
    private const int PMainWindowDwmCaptionColorAttribute = 35;
    private const int PMainWindowColorRefBackground = 0x00F7E8DC;
    private readonly LTabSelect lTabSelect;
    private bool pMainWindowResizeActive;
    private int pMainWindowResizeDirection;
    private Point pMainWindowResizeStartPointer;
    private Rect pMainWindowResizeStartBounds;
    private PFlowControl? pFlowActive;
    private PViewerPanel? pViewerPanelActive;
    private bool pMainWindowAudioOnlyAllowed;
    public PMainWindow()
    {
        InitializeComponent();
        lTabSelect = new LTabSelect();
        PMainWindowTabsRestore(lTabSelect, App.LPreferenceStateCurrent);
        pControlBar.PControlBarTabSelectSet(lTabSelect);
        pControlBar.PPreferenceApplyRequest += PMainWindowPreferenceApplyHandle;
        PMainWindowPreferenceApplyHandle(App.LPreferenceStateCurrent);
        PMainWindowPositionRestore(App.LPreferenceStateCurrent);
        pMainArea.PMainAreaTabSelectSet(lTabSelect);
        lTabSelect.LTabSelectChange += PMainWindowTabSelectChangeHandle;
        PMainWindowTabSelectChangeHandle(lTabSelect.PTabSelectRecord);
        PMainWindowDropHandlersAdd();
        PMainWindowResizeHandlersAdd();
        PreviewKeyDown += PMainWindowShortcutKeyDownHandle;
        Closed += PMainWindowClosedHandle;
    }
    private static void PMainWindowTabsRestore(LTabSelect pTabSelect, LPreferenceState lPreferenceState)
    {
        IReadOnlyList<string> pTabKeys = lPreferenceState.LPreferenceTabLayoutKeys.Count > 0
            ? lPreferenceState.LPreferenceTabLayoutKeys
            : new[] { "Split", "Edit", "Audio", "Convert", "Merge", "Worklist" };
        foreach (string pTabKey in pTabKeys)
            pTabSelect.LTabAddRequest(pTabKey);
        int pSelectIndex = Math.Clamp(lPreferenceState.LPreferenceTabSelectIndex, 0, pTabSelect.PTabRecords.Count - 1);
        pTabSelect.LTabSelectRequest(pTabSelect.PTabRecords[pSelectIndex]);
    }
    private void PMainWindowPositionRestore(LPreferenceState lPrefs)
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
    private void PMainWindowTabSelectChangeHandle(PTabRecord? pTabRecord)
    {
        PMainWindowWorkspaceDetach();
        if (pTabRecord is null)
        {
            return;
        }
        pFlowActive = pTabRecord.PTabWorkspace.PTabWorkspaceFlow;
        pViewerPanelActive = pTabRecord.PTabWorkspace.PTabWorkspaceViewer;
        pMainWindowAudioOnlyAllowed = pTabRecord.PTabLayoutKey == "Audio";
        PMainWindowWorkspaceAttach(pTabRecord);
    }
    private void PMainWindowWorkspaceAttach(PTabRecord pTabRecord)
    {
        if (pFlowActive is null || pViewerPanelActive is null)
        {
            return;
        }
        pFlowActive.PFlowCommandActiveSet(true);
        pFlowActive.PFlowSectionUiActiveSet(pTabRecord.PTabLayoutKey == "Split");
        pFlowActive.Height = App.LPreferenceStateCurrent.LPreferenceFlowHeight;
        pViewerPanelActive.PViewerPanelCommandActiveSet(true);
        pViewerPanelActive.PViewerPanelMediaStatusChange += PMainWindowViewerMediaStatusChangeHandle;
        pViewerPanelActive.PViewerPanelClockTick += PMainWindowViewerClockTickHandle;
        pFlowActive.PFlowCursorChangeRequest += pViewerPanelActive.PViewerPanelSeekRequest;
        pFlowActive.PFlowPlayRequest += pViewerPanelActive.PViewerPanelPlayRequest;
        pFlowActive.PFlowPauseRequest += pViewerPanelActive.PViewerPanelPauseRequest;
        pFlowActive.PFlowVolumeChangeRequest += pViewerPanelActive.PViewerPanelVolumeRequest;
        PMainWindowVolumeSyncForActive(App.LPreferenceStateCurrent);
    }
    private void PMainWindowWorkspaceDetach()
    {
        if (pFlowActive is not null && pViewerPanelActive is not null)
        {
            pViewerPanelActive.PViewerPanelMediaStatusChange -= PMainWindowViewerMediaStatusChangeHandle;
            pViewerPanelActive.PViewerPanelClockTick -= PMainWindowViewerClockTickHandle;
            pFlowActive.PFlowCursorChangeRequest -= pViewerPanelActive.PViewerPanelSeekRequest;
            pFlowActive.PFlowPlayRequest -= pViewerPanelActive.PViewerPanelPlayRequest;
            pFlowActive.PFlowPauseRequest -= pViewerPanelActive.PViewerPanelPauseRequest;
            pFlowActive.PFlowVolumeChangeRequest -= pViewerPanelActive.PViewerPanelVolumeRequest;
            pFlowActive.PFlowSectionUiActiveSet(false);
            pFlowActive.PFlowCommandActiveSet(false);
            pViewerPanelActive.PViewerPanelCommandActiveSet(false);
        }
        pFlowActive = null;
        pViewerPanelActive = null;
    }
    private void PMainWindowViewerMediaStatusChangeHandle(LMediaOpenStatus mediaStatus)
    {
        if (mediaStatus.LMediaOpenMediaInfo is LMediaInfo mediaInfo)
        {
            pFlowActive?.PFlowAttach(mediaInfo, mediaStatus.LMediaOpenSourcePath, TimeSpan.Zero);
            return;
        }
        pFlowActive?.PFlowClear();
    }
    private void PMainWindowViewerClockTickHandle(TimeSpan playbackPosition)
    {
        pFlowActive?.PFlowCursorUpdate(playbackPosition);
    }
    private void PMainWindowShortcutKeyDownHandle(object sender, KeyEventArgs e)
    {
        if (PMainWindowTextInputFind(e.OriginalSource as DependencyObject))
        {
            return;
        }
        bool pHandled = PMainWindowShortcutHandle(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers);
        if (pHandled)
        {
            e.Handled = true;
        }
    }
    private bool PMainWindowShortcutHandle(Key pKey, ModifierKeys pModifiers)
    {
        if (pModifiers == ModifierKeys.Control && (pKey == Key.OemQuestion || pKey == Key.Divide))
        {
            pControlBar.PControlBarShortcutDialogShow();
            return true;
        }
        if (pModifiers != ModifierKeys.None)
        {
            return false;
        }
        return pKey switch
        {
            Key.Space => PMainWindowShortcutPlayPause(),
            Key.C => pFlowActive?.PFlowShortcutRequest("zoomIn") == true,
            Key.V => pFlowActive?.PFlowShortcutRequest("zoomOut") == true,
            Key.Q => pFlowActive?.PFlowShortcutRequest("addSection") == true,
            Key.D => pFlowActive?.PFlowShortcutRequest("setStart") == true,
            Key.S => pFlowActive?.PFlowShortcutRequest("splitSection") == true,
            Key.F => pFlowActive?.PFlowShortcutRequest("setEnd") == true,
            Key.Delete => pFlowActive?.PFlowShortcutRequest("deleteSection") == true,
            Key.E => pFlowActive?.PFlowShortcutRequest("previousKey") == true,
            Key.W => pFlowActive?.PFlowShortcutRequest("nearestKey") == true,
            Key.R => pFlowActive?.PFlowShortcutRequest("nextKey") == true,
            _ => false
        };
    }
    private bool PMainWindowShortcutPlayPause()
    {
        if (pViewerPanelActive is null)
        {
            return false;
        }
        if (pViewerPanelActive.LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            pViewerPanelActive.PViewerPanelPauseRequest();
        }
        else
        {
            pViewerPanelActive.PViewerPanelPlayRequest();
        }
        return true;
    }
    private static bool PMainWindowTextInputFind(DependencyObject? pSource)
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
    private void PMainWindowPreferenceApplyHandle(LPreferenceState lPreferenceState)
    {
        Width = lPreferenceState.LPreferenceProgramWidth;
        Height = lPreferenceState.LPreferenceProgramHeight;
        FontSize = lPreferenceState.LPreferenceFontSize;
        if (pFlowActive is not null)
            pFlowActive.Height = lPreferenceState.LPreferenceFlowHeight;
        PMainWindowVolumeSyncForActive(lPreferenceState);
    }
    private void PMainWindowVolumeSyncForActive(LPreferenceState lPreferenceState)
    {
        if (pFlowActive is null || pViewerPanelActive is null) return;
        double pVolume = lPreferenceState.LPreferenceVolumeSingleGlobal ? lPreferenceState.LPreferenceVolume : pViewerPanelActive.PViewerPanelVolumeCurrent;
        if (lPreferenceState.LPreferenceVolumeSingleGlobal) pViewerPanelActive.PViewerPanelVolumeRequest(pVolume);
        pFlowActive.PFlowVolumeValueSet(pVolume);
    }
    private void PMainWindowDropHandlersAdd()
    {
        AddHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PMainWindowDragAccept), true);
        AddHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PMainWindowDragAccept), true);
        AddHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PMainWindowDrop), true);
    }
    private void PMainWindowDropHandlersRemove()
    {
        RemoveHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PMainWindowDragAccept));
        RemoveHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PMainWindowDragAccept));
        RemoveHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PMainWindowDrop));
    }
    private void PMainWindowResizeHandlersAdd()
    {
        PreviewMouseMove += PMainWindowResizeMouseMove;
        PreviewMouseLeftButtonDown += PMainWindowResizeMouseDown;
        PreviewMouseLeftButtonUp += PMainWindowResizeMouseUp;
        LostMouseCapture += PMainWindowResizeLostCaptureHandle;
    }
    private void PMainWindowResizeHandlersRemove()
    {
        PreviewMouseMove -= PMainWindowResizeMouseMove;
        PreviewMouseLeftButtonDown -= PMainWindowResizeMouseDown;
        PreviewMouseLeftButtonUp -= PMainWindowResizeMouseUp;
        LostMouseCapture -= PMainWindowResizeLostCaptureHandle;
    }
    private void PMainWindowResizeMouseDown(object sender, MouseButtonEventArgs e)
    {
        int pDirection = PMainWindowResizeDirectionRead(e.GetPosition(this));
        if (WindowState != WindowState.Normal || pDirection == 0)
            return;
        pMainWindowResizeActive = true;
        pMainWindowResizeDirection = pDirection;
        pMainWindowResizeStartPointer = PMainWindowPointerScreenDipRead(e);
        pMainWindowResizeStartBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        Mouse.Capture(this);
        e.Handled = true;
    }
    private void PMainWindowResizeMouseMove(object sender, MouseEventArgs e)
    {
        if (pMainWindowResizeActive)
        {
            PMainWindowResizeApply(PMainWindowPointerScreenDipRead(e));
            e.Handled = true;
            return;
        }
        int pDirection = WindowState == WindowState.Normal ? PMainWindowResizeDirectionRead(e.GetPosition(this)) : 0;
        Cursor = pDirection == 0 ? null : PMainWindowResizeCursorRead(pDirection);
    }
    private void PMainWindowResizeMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!pMainWindowResizeActive)
            return;
        pMainWindowResizeActive = false;
        Mouse.Capture(null);
        e.Handled = true;
    }
    private void PMainWindowResizeLostCaptureHandle(object sender, MouseEventArgs e)
    {
        pMainWindowResizeActive = false;
    }
    private int PMainWindowResizeDirectionRead(Point pPoint)
    {
        bool pLeft = pPoint.X >= 0 && pPoint.X < PMainWindowResizeBorderPixels;
        bool pRight = pPoint.X <= ActualWidth && pPoint.X > ActualWidth - PMainWindowResizeBorderPixels;
        bool pTop = pPoint.Y >= 0 && pPoint.Y < PMainWindowResizeBorderPixels;
        bool pBottom = pPoint.Y <= ActualHeight && pPoint.Y > ActualHeight - PMainWindowResizeBorderPixels;
        int pDirection = 0;
        if (pLeft) pDirection |= PMainWindowResizeLeft;
        if (pRight) pDirection |= PMainWindowResizeRight;
        if (pTop) pDirection |= PMainWindowResizeTop;
        if (pBottom) pDirection |= PMainWindowResizeBottom;
        return pDirection;
    }
    private static Cursor PMainWindowResizeCursorRead(int pDirection)
    {
        bool pHorizontal = (pDirection & (PMainWindowResizeLeft | PMainWindowResizeRight)) != 0;
        bool pVertical = (pDirection & (PMainWindowResizeTop | PMainWindowResizeBottom)) != 0;
        if (!pHorizontal || !pVertical)
            return pHorizontal ? Cursors.SizeWE : Cursors.SizeNS;
        bool pLeft = (pDirection & PMainWindowResizeLeft) != 0;
        bool pTop = (pDirection & PMainWindowResizeTop) != 0;
        return pLeft == pTop ? Cursors.SizeNWSE : Cursors.SizeNESW;
    }
    private Point PMainWindowPointerScreenDipRead(MouseEventArgs e)
    {
        Point pScreenPoint = PointToScreen(e.GetPosition(this));
        return PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice.Transform(pScreenPoint) ?? pScreenPoint;
    }
    private void PMainWindowResizeApply(Point pPointer)
    {
        double pDx = pPointer.X - pMainWindowResizeStartPointer.X;
        double pDy = pPointer.Y - pMainWindowResizeStartPointer.Y;
        double pLeft = pMainWindowResizeStartBounds.Left;
        double pTop = pMainWindowResizeStartBounds.Top;
        double pWidth = pMainWindowResizeStartBounds.Width;
        double pHeight = pMainWindowResizeStartBounds.Height;
        if ((pMainWindowResizeDirection & PMainWindowResizeLeft) != 0)
        {
            pWidth = Math.Max(MinWidth, pMainWindowResizeStartBounds.Width - pDx);
            pLeft = pMainWindowResizeStartBounds.Right - pWidth;
        }
        if ((pMainWindowResizeDirection & PMainWindowResizeRight) != 0)
            pWidth = Math.Max(MinWidth, pMainWindowResizeStartBounds.Width + pDx);
        if ((pMainWindowResizeDirection & PMainWindowResizeTop) != 0)
        {
            pHeight = Math.Max(MinHeight, pMainWindowResizeStartBounds.Height - pDy);
            pTop = pMainWindowResizeStartBounds.Bottom - pHeight;
        }
        if ((pMainWindowResizeDirection & PMainWindowResizeBottom) != 0)
            pHeight = Math.Max(MinHeight, pMainWindowResizeStartBounds.Height + pDy);
        Left = pLeft;
        Top = pTop;
        Width = pWidth;
        Height = pHeight;
    }
    private void PMainWindowDragAccept(object sender, DragEventArgs dragEvent)
    {
        dragEvent.Effects = PMainWindowDropEffectRead(dragEvent);
        dragEvent.Handled = true;
    }
    private void PMainWindowDrop(object sender, DragEventArgs dragEvent)
    {
        DragDropEffects dropEffect = PMainWindowDropEffectRead(dragEvent);
        dragEvent.Effects = dropEffect;
        dragEvent.Handled = true;
        if (dropEffect == DragDropEffects.None || pViewerPanelActive is null)
        {
            return;
        }
        string? sourcePath = PMainWindowDropSourcePathRead(dragEvent);
        if (sourcePath is null)
        {
            dragEvent.Effects = DragDropEffects.None;
            return;
        }
        pViewerPanelActive.PViewerPanelSourceOpenRequest(sourcePath);
    }
    private DragDropEffects PMainWindowDropEffectRead(DragEventArgs dragEvent)
    {
        if (pViewerPanelActive is null)
        {
            return DragDropEffects.None;
        }
        string? pSourcePath = PMainWindowDropSourcePathRead(dragEvent);
        if (pSourcePath is null || PMainWindowAudioExtensionCheck(pSourcePath) && !pMainWindowAudioOnlyAllowed)
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
    private static bool PMainWindowAudioExtensionCheck(string pSourcePath)
    {
        string pExtension = Path.GetExtension(pSourcePath);
        return pExtension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".aac", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".flac", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
    }
    private static string? PMainWindowDropSourcePathRead(DragEventArgs dragEvent)
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
    private void PMainWindowClosedHandle(object? sender, EventArgs eventArgs)
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
        lPrefs.LPreferenceTabLayoutKeys = lTabSelect.PTabRecords.Select(r => r.PTabLayoutKey).ToList();
        lPrefs.LPreferenceTabSelectIndex = lTabSelect.PTabSelectRecord is null ? 0
            : Math.Max(0, lTabSelect.PTabRecords.IndexOf(lTabSelect.PTabSelectRecord));
        App.LPreferenceStateSet(lPrefs);
        lTabSelect.LTabSelectChange -= PMainWindowTabSelectChangeHandle;
        pControlBar.PPreferenceApplyRequest -= PMainWindowPreferenceApplyHandle;
        PreviewKeyDown -= PMainWindowShortcutKeyDownHandle;
        PMainWindowDropHandlersRemove();
        PMainWindowResizeHandlersRemove();
        PMainWindowWorkspaceDetach();
        foreach (PTabRecord pTabRecord in lTabSelect.PTabRecords)
        {
            pTabRecord.PTabWorkspace.PTabWorkspaceCloseRequest();
        }
        Closed -= PMainWindowClosedHandle;
    }
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PMainWindowDwmCornerApply();
    }
    private void PMainWindowDwmCornerApply()
    {
        IntPtr pMainWindowHandle = new WindowInteropHelper(this).Handle;
        if (pMainWindowHandle == IntPtr.Zero)
        {
            return;
        }
        int pMainWindowCornerPreference = PMainWindowDwmWindowCornerRound;
        _ = DwmSetWindowAttribute(
            pMainWindowHandle,
            PMainWindowDwmWindowCornerPreference,
            ref pMainWindowCornerPreference,
            Marshal.SizeOf<int>());
        int pMainWindowCaptionColor = PMainWindowColorRefBackground;
        _ = DwmSetWindowAttribute(
            pMainWindowHandle,
            PMainWindowDwmCaptionColorAttribute,
            ref pMainWindowCaptionColor,
            Marshal.SizeOf<int>());
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);
}
