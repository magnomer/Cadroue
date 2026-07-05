using Cadroue.UIShell.PMainWindow;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSExportSpecific : Window
{
    private const int PSExportSpecificResizeBorderPixels = 8;
    private const int PSExportSpecificResizeLeft = 1;
    private const int PSExportSpecificResizeRight = 2;
    private const int PSExportSpecificResizeTop = 4;
    private const int PSExportSpecificResizeBottom = 8;
    private const int PSExportSpecificDwmWindowCornerPreference = 33;
    private const int PSExportSpecificDwmWindowCornerRound = 2;
    private const int PSExportSpecificDwmCaptionColorAttribute = 35;
    private const int PSExportSpecificColorRefBackground = 0x00F7E8DC;

    private readonly LExportSpecificState lsExportSpecificSource;
    private readonly LExportSpecificState lsExportSpecificEdit;
    private readonly System.Action pSummaryRefresh;
    private readonly PMainTokenTextBox psNameBox;
    private readonly ComboBox psContainerCombo;
    private readonly ComboBox psModeCombo;
    private readonly ComboBox psVideoStreamCombo;
    private readonly ComboBox psAudioStreamCombo;
    private readonly ComboBox psVideoModeCombo;
    private readonly ComboBox psAudioModeCombo;
    private string psVideoEncoderVerificationLog = "Verification has not run yet.";
    private string? psExportSpecificCustomFolderPath;
    private bool psExportSpecificResizeActive;
    private int psExportSpecificResizeDirection;
    private Point psExportSpecificResizeStartPointer;
    private Rect psExportSpecificResizeStartBounds;

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PMutedBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x6F, 0x83));

    public PSExportSpecific(LExportSpecificState lsExportSpecificState, System.Action pRefresh)
    {
        lsExportSpecificSource = lsExportSpecificState;
        lsExportSpecificEdit = lsExportSpecificState.LClone();
        pSummaryRefresh = pRefresh;
        psNameBox = new PMainTokenTextBox { Text = lsExportSpecificEdit.Name, MinWidth = 320 };
        psContainerCombo = PSComboBuild(lsExportSpecificEdit.Container, "MP4", "Matroska", "MOV", "WebM", "M4A", "MP3", "WAV", "FLAC", "OGG", "All FFmpeg formats...");
        psModeCombo = PSComboBuild(lsExportSpecificEdit.ExportMode, "Smart export", "Remux only", "Re-encode");
        psVideoStreamCombo = PSComboBuild(lsExportSpecificEdit.VideoStream, "Include", "Exclude");
        psAudioStreamCombo = PSComboBuild(lsExportSpecificEdit.AudioStream, "Include first audio track", "Include all audio tracks", "Exclude");
        psVideoModeCombo = PSComboBuild(lsExportSpecificEdit.VideoMode, "Auto", "Copy", "Encode", "Exclude");
        psAudioModeCombo = PSComboBuild(lsExportSpecificEdit.AudioMode, "Auto", "Copy", "Encode", "Exclude");

        Title = "Specific Export Settings";
        Width = 820;
        Height = 700;
        MinWidth = 680;
        MinHeight = 520;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        Content = PSExportSpecificWindowBuild();
        PSExportSpecificResizeHandlersAdd();
    }

    private UIElement PSExportSpecificWindowBuild()
    {
        var pRoot = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)) };
        pRoot.Children.Add(PSTabControlBuild());
        pRoot.Children.Add(PSExportSpecificChromeOverlayBuild());
        return pRoot;
    }

    private void PSExportSpecificApply()
    {
        lsExportSpecificEdit.Name = string.IsNullOrWhiteSpace(psNameBox.Text) ? "{OriginalName}_export" : psNameBox.Text.Trim();
        lsExportSpecificEdit.Container = PSComboTextRead(psContainerCombo);
        lsExportSpecificEdit.ExportMode = PSComboTextRead(psModeCombo);
        lsExportSpecificEdit.VideoStream = PSComboTextRead(psVideoStreamCombo);
        lsExportSpecificEdit.AudioStream = PSComboTextRead(psAudioStreamCombo);
        lsExportSpecificEdit.VideoMode = PSComboTextRead(psVideoModeCombo);
        lsExportSpecificEdit.AudioMode = PSComboTextRead(psAudioModeCombo);
        lsExportSpecificSource.LCopyFrom(lsExportSpecificEdit);
        pSummaryRefresh();
    }

    private void PSExportSpecificResizeHandlersAdd()
    {
        PreviewMouseMove += PSExportSpecificResizeMouseMove;
        PreviewMouseLeftButtonDown += PSExportSpecificResizeMouseDown;
        PreviewMouseLeftButtonUp += PSExportSpecificResizeMouseUp;
        LostMouseCapture += PSExportSpecificResizeLostCaptureHandle;
        Closed += PSExportSpecificClosedHandle;
    }

    private void PSExportSpecificResizeHandlersRemove()
    {
        PreviewMouseMove -= PSExportSpecificResizeMouseMove;
        PreviewMouseLeftButtonDown -= PSExportSpecificResizeMouseDown;
        PreviewMouseLeftButtonUp -= PSExportSpecificResizeMouseUp;
        LostMouseCapture -= PSExportSpecificResizeLostCaptureHandle;
        Closed -= PSExportSpecificClosedHandle;
    }

    private void PSExportSpecificResizeMouseDown(object sender, MouseButtonEventArgs e)
    {
        int pDirection = PSExportSpecificResizeDirectionRead(e.GetPosition(this));
        if (WindowState != WindowState.Normal || pDirection == 0)
        {
            return;
        }

        psExportSpecificResizeActive = true;
        psExportSpecificResizeDirection = pDirection;
        psExportSpecificResizeStartPointer = PSExportSpecificPointerScreenDipRead(e);
        psExportSpecificResizeStartBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        Mouse.Capture(this);
        e.Handled = true;
    }

    private void PSExportSpecificResizeMouseMove(object sender, MouseEventArgs e)
    {
        if (psExportSpecificResizeActive)
        {
            PSExportSpecificResizeApply(PSExportSpecificPointerScreenDipRead(e));
            e.Handled = true;
            return;
        }

        int pDirection = WindowState == WindowState.Normal ? PSExportSpecificResizeDirectionRead(e.GetPosition(this)) : 0;
        Cursor = pDirection == 0 ? null : PSExportSpecificResizeCursorRead(pDirection);
    }

    private void PSExportSpecificResizeMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!psExportSpecificResizeActive)
        {
            return;
        }

        psExportSpecificResizeActive = false;
        Mouse.Capture(null);
        e.Handled = true;
    }

    private void PSExportSpecificResizeLostCaptureHandle(object sender, MouseEventArgs e)
    {
        psExportSpecificResizeActive = false;
    }

    private void PSExportSpecificClosedHandle(object? sender, System.EventArgs e)
    {
        PSExportSpecificResizeHandlersRemove();
    }

    private int PSExportSpecificResizeDirectionRead(Point pPoint)
    {
        bool pLeft = pPoint.X >= 0 && pPoint.X < PSExportSpecificResizeBorderPixels;
        bool pRight = pPoint.X <= ActualWidth && pPoint.X > ActualWidth - PSExportSpecificResizeBorderPixels;
        bool pTop = pPoint.Y >= 0 && pPoint.Y < PSExportSpecificResizeBorderPixels;
        bool pBottom = pPoint.Y <= ActualHeight && pPoint.Y > ActualHeight - PSExportSpecificResizeBorderPixels;
        int pDirection = 0;
        if (pLeft) pDirection |= PSExportSpecificResizeLeft;
        if (pRight) pDirection |= PSExportSpecificResizeRight;
        if (pTop) pDirection |= PSExportSpecificResizeTop;
        if (pBottom) pDirection |= PSExportSpecificResizeBottom;
        return pDirection;
    }

    private static Cursor PSExportSpecificResizeCursorRead(int pDirection)
    {
        bool pHorizontal = (pDirection & (PSExportSpecificResizeLeft | PSExportSpecificResizeRight)) != 0;
        bool pVertical = (pDirection & (PSExportSpecificResizeTop | PSExportSpecificResizeBottom)) != 0;
        if (!pHorizontal || !pVertical)
        {
            return pHorizontal ? Cursors.SizeWE : Cursors.SizeNS;
        }

        bool pLeft = (pDirection & PSExportSpecificResizeLeft) != 0;
        bool pTop = (pDirection & PSExportSpecificResizeTop) != 0;
        return pLeft == pTop ? Cursors.SizeNWSE : Cursors.SizeNESW;
    }

    private Point PSExportSpecificPointerScreenDipRead(MouseEventArgs e)
    {
        Point pScreenPoint = PointToScreen(e.GetPosition(this));
        return PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice.Transform(pScreenPoint) ?? pScreenPoint;
    }

    private void PSExportSpecificResizeApply(Point pPointer)
    {
        double pDx = pPointer.X - psExportSpecificResizeStartPointer.X;
        double pDy = pPointer.Y - psExportSpecificResizeStartPointer.Y;
        double pLeft = psExportSpecificResizeStartBounds.Left;
        double pTop = psExportSpecificResizeStartBounds.Top;
        double pWidth = psExportSpecificResizeStartBounds.Width;
        double pHeight = psExportSpecificResizeStartBounds.Height;

        if ((psExportSpecificResizeDirection & PSExportSpecificResizeLeft) != 0)
        {
            pWidth = Math.Max(MinWidth, psExportSpecificResizeStartBounds.Width - pDx);
            pLeft = psExportSpecificResizeStartBounds.Right - pWidth;
        }

        if ((psExportSpecificResizeDirection & PSExportSpecificResizeRight) != 0)
        {
            pWidth = Math.Max(MinWidth, psExportSpecificResizeStartBounds.Width + pDx);
        }

        if ((psExportSpecificResizeDirection & PSExportSpecificResizeTop) != 0)
        {
            pHeight = Math.Max(MinHeight, psExportSpecificResizeStartBounds.Height - pDy);
            pTop = psExportSpecificResizeStartBounds.Bottom - pHeight;
        }

        if ((psExportSpecificResizeDirection & PSExportSpecificResizeBottom) != 0)
        {
            pHeight = Math.Max(MinHeight, psExportSpecificResizeStartBounds.Height + pDy);
        }

        Left = pLeft;
        Top = pTop;
        Width = pWidth;
        Height = pHeight;
    }


    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PSExportSpecificDwmCornerApply();
    }

    private void PSExportSpecificDwmCornerApply()
    {
        IntPtr psExportSpecificHandle = new WindowInteropHelper(this).Handle;
        if (psExportSpecificHandle == IntPtr.Zero)
        {
            return;
        }

        int psExportSpecificCornerPreference = PSExportSpecificDwmWindowCornerRound;
        _ = DwmSetWindowAttribute(
            psExportSpecificHandle,
            PSExportSpecificDwmWindowCornerPreference,
            ref psExportSpecificCornerPreference,
            Marshal.SizeOf<int>());

        int psExportSpecificCaptionColor = PSExportSpecificColorRefBackground;
        _ = DwmSetWindowAttribute(
            psExportSpecificHandle,
            PSExportSpecificDwmCaptionColorAttribute,
            ref psExportSpecificCaptionColor,
            Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);

}
