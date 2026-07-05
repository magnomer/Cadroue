using Cadroue.UIShell.PMainWindow;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder : Window
{
    private const int PSGrabberBorderPixels = 8;
    private const int PSGrabberLeft = 1;
    private const int PSGrabberRight = 2;
    private const int PSGrabberTop = 4;
    private const int PSGrabberBottom = 8;
    private const int PSEncoderDwmPreference = 33;
    private const int PSEncoderDwmRound = 2;
    private const int PSEncoderDwmCaption = 35;
    private const int PSEncoderColor = 0x00F7E8DC;

    private readonly LExportSpecificState lsExportSpecificSource;
    private readonly LExportSpecificState lsExportSpecificEdit;
    private readonly System.Action pSummaryRefresh;
    private readonly PToken psNameBox;
    private readonly ComboBox psContainerCombo;
    private readonly ComboBox psModeCombo;
    private readonly ComboBox psVideoStreamCombo;
    private readonly ComboBox psAudioStreamCombo;
    private readonly ComboBox psVideoModeCombo;
    private readonly ComboBox psAudioModeCombo;
    private string psCodecLog = "Verification has not run yet.";
    private string? psEncoderFolderPath;
    private bool psGrabberActive;
    private int psGrabberDirection;
    private Point psGrabberStartPointer;
    private Rect psGrabberStartBounds;

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PMutedBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x6F, 0x83));

    public PSEncoder(LExportSpecificState lsExportSpecificState, System.Action pRefresh)
    {
        lsExportSpecificSource = lsExportSpecificState;
        lsExportSpecificEdit = lsExportSpecificState.LPresetClone();
        pSummaryRefresh = pRefresh;
        psNameBox = new PToken { Text = lsExportSpecificEdit.Name, MinWidth = 320 };
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
        Content = PSEncoderBuild();
        PSGrabberHandlersAdd();
    }

    private UIElement PSEncoderBuild()
    {
        var pRoot = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)) };
        pRoot.Children.Add(PSSheetControlBuild());
        pRoot.Children.Add(PSCasementOverlayBuild());
        return pRoot;
    }

    private void PSEncoderApply()
    {
        lsExportSpecificEdit.Name = string.IsNullOrWhiteSpace(psNameBox.Text) ? "{OriginalName}_export" : psNameBox.Text.Trim();
        lsExportSpecificEdit.Container = PSComboTextRead(psContainerCombo);
        lsExportSpecificEdit.ExportMode = PSComboTextRead(psModeCombo);
        lsExportSpecificEdit.VideoStream = PSComboTextRead(psVideoStreamCombo);
        lsExportSpecificEdit.AudioStream = PSComboTextRead(psAudioStreamCombo);
        lsExportSpecificEdit.VideoMode = PSComboTextRead(psVideoModeCombo);
        lsExportSpecificEdit.AudioMode = PSComboTextRead(psAudioModeCombo);
        lsExportSpecificSource.LPresetCopy(lsExportSpecificEdit);
        pSummaryRefresh();
    }

    private void PSGrabberHandlersAdd()
    {
        PreviewMouseMove += PSGrabberMoveHandle;
        PreviewMouseLeftButtonDown += PSGrabberPressHandle;
        PreviewMouseLeftButtonUp += PSGrabberReleaseHandle;
        LostMouseCapture += PSGrabberCaptureHandle;
        Closed += PSEncoderCloseHandle;
    }

    private void PSGrabberHandlersRemove()
    {
        PreviewMouseMove -= PSGrabberMoveHandle;
        PreviewMouseLeftButtonDown -= PSGrabberPressHandle;
        PreviewMouseLeftButtonUp -= PSGrabberReleaseHandle;
        LostMouseCapture -= PSGrabberCaptureHandle;
        Closed -= PSEncoderCloseHandle;
    }

    private void PSGrabberPressHandle(object sender, MouseButtonEventArgs e)
    {
        int pDirection = PSGrabberDirectionRead(e.GetPosition(this));
        if (WindowState != WindowState.Normal || pDirection == 0)
        {
            return;
        }

        psGrabberActive = true;
        psGrabberDirection = pDirection;
        psGrabberStartPointer = PSGrabberPointerRead(e);
        psGrabberStartBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        Mouse.Capture(this);
        e.Handled = true;
    }

    private void PSGrabberMoveHandle(object sender, MouseEventArgs e)
    {
        if (psGrabberActive)
        {
            PSGrabberApply(PSGrabberPointerRead(e));
            e.Handled = true;
            return;
        }

        int pDirection = WindowState == WindowState.Normal ? PSGrabberDirectionRead(e.GetPosition(this)) : 0;
        Cursor = pDirection == 0 ? null : PSGrabberCursorRead(pDirection);
    }

    private void PSGrabberReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        if (!psGrabberActive)
        {
            return;
        }

        psGrabberActive = false;
        Mouse.Capture(null);
        e.Handled = true;
    }

    private void PSGrabberCaptureHandle(object sender, MouseEventArgs e)
    {
        psGrabberActive = false;
    }

    private void PSEncoderCloseHandle(object? sender, System.EventArgs e)
    {
        PSGrabberHandlersRemove();
    }

    private int PSGrabberDirectionRead(Point pPoint)
    {
        bool pLeft = pPoint.X >= 0 && pPoint.X < PSGrabberBorderPixels;
        bool pRight = pPoint.X <= ActualWidth && pPoint.X > ActualWidth - PSGrabberBorderPixels;
        bool pTop = pPoint.Y >= 0 && pPoint.Y < PSGrabberBorderPixels;
        bool pBottom = pPoint.Y <= ActualHeight && pPoint.Y > ActualHeight - PSGrabberBorderPixels;
        int pDirection = 0;
        if (pLeft) pDirection |= PSGrabberLeft;
        if (pRight) pDirection |= PSGrabberRight;
        if (pTop) pDirection |= PSGrabberTop;
        if (pBottom) pDirection |= PSGrabberBottom;
        return pDirection;
    }

    private static Cursor PSGrabberCursorRead(int pDirection)
    {
        bool pHorizontal = (pDirection & (PSGrabberLeft | PSGrabberRight)) != 0;
        bool pVertical = (pDirection & (PSGrabberTop | PSGrabberBottom)) != 0;
        if (!pHorizontal || !pVertical)
        {
            return pHorizontal ? Cursors.SizeWE : Cursors.SizeNS;
        }

        bool pLeft = (pDirection & PSGrabberLeft) != 0;
        bool pTop = (pDirection & PSGrabberTop) != 0;
        return pLeft == pTop ? Cursors.SizeNWSE : Cursors.SizeNESW;
    }

    private Point PSGrabberPointerRead(MouseEventArgs e)
    {
        Point pScreenPoint = PointToScreen(e.GetPosition(this));
        return PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice.Transform(pScreenPoint) ?? pScreenPoint;
    }

    private void PSGrabberApply(Point pPointer)
    {
        double pDx = pPointer.X - psGrabberStartPointer.X;
        double pDy = pPointer.Y - psGrabberStartPointer.Y;
        double pLeft = psGrabberStartBounds.Left;
        double pTop = psGrabberStartBounds.Top;
        double pWidth = psGrabberStartBounds.Width;
        double pHeight = psGrabberStartBounds.Height;

        if ((psGrabberDirection & PSGrabberLeft) != 0)
        {
            pWidth = Math.Max(MinWidth, psGrabberStartBounds.Width - pDx);
            pLeft = psGrabberStartBounds.Right - pWidth;
        }

        if ((psGrabberDirection & PSGrabberRight) != 0)
        {
            pWidth = Math.Max(MinWidth, psGrabberStartBounds.Width + pDx);
        }

        if ((psGrabberDirection & PSGrabberTop) != 0)
        {
            pHeight = Math.Max(MinHeight, psGrabberStartBounds.Height - pDy);
            pTop = psGrabberStartBounds.Bottom - pHeight;
        }

        if ((psGrabberDirection & PSGrabberBottom) != 0)
        {
            pHeight = Math.Max(MinHeight, psGrabberStartBounds.Height + pDy);
        }

        Left = pLeft;
        Top = pTop;
        Width = pWidth;
        Height = pHeight;
    }


    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PSEncoderDwmApply();
    }

    private void PSEncoderDwmApply()
    {
        IntPtr psEncoderHandle = new WindowInteropHelper(this).Handle;
        if (psEncoderHandle == IntPtr.Zero)
        {
            return;
        }

        int psEncoderCornerPreference = PSEncoderDwmRound;
        _ = DwmSetWindowAttribute(
            psEncoderHandle,
            PSEncoderDwmPreference,
            ref psEncoderCornerPreference,
            Marshal.SizeOf<int>());

        int psEncoderCaptionColor = PSEncoderColor;
        _ = DwmSetWindowAttribute(
            psEncoderHandle,
            PSEncoderDwmCaption,
            ref psEncoderCaptionColor,
            Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);

}
