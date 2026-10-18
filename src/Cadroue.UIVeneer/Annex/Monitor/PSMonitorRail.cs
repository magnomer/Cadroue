using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

// One waveform rail of the monitor: its canvas, status, playhead and radio, drawn and sought through the driver.
internal sealed class PSMonitorRail
{
    private const double PSMonitorGutter = QSMonitorPlan.QSMonitorGutter;
    private const double PSMonitorRadioLeft = PSMonitorGutter + 8;

    internal static readonly Brush PSMonitorGridFill = new SolidColorBrush(Color.FromRgb(0xE4, 0xE9, 0xF0));
    internal static readonly Brush PSMonitorAxisFill = new SolidColorBrush(Color.FromRgb(0x8A, 0x95, 0xA6));

    private readonly QSMonitor psMonitorSource;
    private readonly QViewer psMonitorViewer;
    private readonly Brush psMonitorRailFill;
    private readonly bool psMonitorRailAfter;
    private readonly RadioButton psMonitorRailRadio;
    private readonly Canvas psMonitorRailCanvas;
    private readonly TextBlock psMonitorRailStatus;
    private readonly Border psMonitorRailHead;

    internal PSMonitorRail(
        QSMonitor pSource,
        QViewer pViewer,
        Brush pFill,
        bool pAfter,
        string pLabelKey,
        string pTooltipKey,
        Action pResize)
    {
        psMonitorSource = pSource;
        psMonitorViewer = pViewer;
        psMonitorRailFill = pFill;
        psMonitorRailAfter = pAfter;
        psMonitorRailRadio = PSMonitorRadioBuild(pLabelKey, pTooltipKey, !pAfter);
        PSMonitorRailGrid = PSMonitorLaneBuild(
            psMonitorRailRadio,
            out psMonitorRailCanvas,
            out psMonitorRailStatus,
            out psMonitorRailHead);
        PSMonitorCanvasAttach(pResize);
    }

    internal Grid PSMonitorRailGrid { get; }

    private void PSMonitorCanvasAttach(Action pResize)
    {
        psMonitorRailCanvas.SizeChanged += (_, _) => pResize();
        psMonitorRailCanvas.MouseLeftButtonDown += PSMonitorSeekStart;
        psMonitorRailCanvas.MouseMove += PSMonitorSeekHandle;
        psMonitorRailCanvas.MouseLeftButtonUp += (_, _) => psMonitorRailCanvas.ReleaseMouseCapture();
    }

    private static Grid PSMonitorLaneBuild(
        RadioButton pRadio,
        out Canvas pCanvas,
        out TextBlock pStatus,
        out Border pHead)
    {
        var psRail = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)),
            ClipToBounds = true
        };

        var psCanvas = new Canvas { Background = Brushes.Transparent };
        psRail.Children.Add(psCanvas);
        pCanvas = psCanvas;

        var psHead = new Border
        {
            Width = 1.5,
            Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x27, 0x33)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        psRail.Children.Add(psHead);
        pHead = psHead;

        var psStatus = new TextBlock
        {
            FontSize = 12,
            Foreground = PSField.PSFieldMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        psRail.Children.Add(psStatus);
        pStatus = psStatus;

        psRail.Children.Add(pRadio);
        return psRail;
    }

    private RadioButton PSMonitorRadioBuild(string pLabelKey, string pTooltipKey, bool pBypass)
    {
        RadioButton psRadio = PSMonitorOptionBuild(pLabelKey, pTooltipKey);
        PSMonitorRadioAttach(psRadio, pBypass);
        return psRadio;
    }

    private void PSMonitorRadioAttach(RadioButton psRadio, bool pBypass) =>
        psRadio.Checked += (_, _) => psMonitorSource.QSMonitorRadioHandle(pBypass);

    private static RadioButton PSMonitorOptionBuild(string pLabelKey, string pTooltipKey)
    {
        return new RadioButton
        {
            Content = QLocalizationCatalog.QLocalizationTextRead(pLabelKey),
            GroupName = "PSMonitorAudio",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PSField.PSFieldMuted,
            Cursor = Cursors.Hand,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTooltipKey),
            Margin = new Thickness(PSMonitorRadioLeft, 8, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
    }

    // The rail's radio follows the bypass: the before rail is checked while bypassed, the after rail otherwise.
    internal void PSMonitorRadioApply() =>
        psMonitorRailRadio.IsChecked = PLook.PLookChecked[psMonitorSource.QSMonitorBypass != psMonitorRailAfter];

    internal void PSMonitorRailDraw()
    {
        PSMonitorStatusSet();
        psMonitorRailCanvas.Children.Clear();
        QSMonitorFrame lFrame = psMonitorSource.QSMonitorFrameResolve(
            psMonitorRailAfter, psMonitorRailCanvas.ActualWidth, psMonitorRailCanvas.ActualHeight);
        PSMonitorFrameDraw(psMonitorRailCanvas, psMonitorRailFill, lFrame);
    }

    private void PSMonitorStatusSet() =>
        psMonitorRailStatus.Text = psMonitorSource.QSMonitorWaveform.QSMonitorStatusRead(psMonitorRailAfter);

    private static void PSMonitorFrameDraw(Canvas pCanvas, Brush pFill, QSMonitorFrame lFrame)
    {
        lFrame.QSMonitorFrameLines.ToList().ForEach(pY => PSMonitorLineDraw(pCanvas, pY));
        lFrame.QSMonitorFrameLabels.ToList().ForEach(lLabel => PSMonitorLabelDraw(pCanvas, lLabel));
        pCanvas.Children.Add(new System.Windows.Shapes.Path
        {
            Data = PFlowWaveform.PFlowWaveformBuild(lFrame.QSMonitorFrameOutline),
            Fill = pFill
        });
    }

    private static void PSMonitorLineDraw(Canvas pCanvas, double pY) => pCanvas.Children.Add(
        new System.Windows.Shapes.Line
        {
            X1 = PSMonitorGutter,
            X2 = pCanvas.ActualWidth,
            Y1 = pY,
            Y2 = pY,
            Stroke = PSMonitorGridFill,
            StrokeThickness = 1,
            IsHitTestVisible = false
        });

    private static void PSMonitorLabelDraw(Canvas pCanvas, QSMonitorLabel lLabel)
    {
        var pText = new TextBlock
        {
            Text = lLabel.QSMonitorLabelText,
            FontSize = 10,
            Foreground = PSMonitorAxisFill,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(pText, 4);
        Canvas.SetTop(pText, lLabel.QSMonitorLabelTop);
        pCanvas.Children.Add(pText);
    }

    internal void PSMonitorHeadApply()
    {
        QSMonitorHead lHead = psMonitorSource.QSMonitorHeadResolve(
            psMonitorRailCanvas.ActualWidth, psMonitorViewer.QViewerDuration);
        psMonitorRailHead.Visibility = PLook.PLookVisible[lHead.QSMonitorHeadShown];
        psMonitorRailHead.Margin = new Thickness(lHead.QSMonitorHeadLeft, 0, 0, 0);
    }

    private void PSMonitorSeekStart(object pSender, MouseButtonEventArgs pEvent)
    {
        ((Canvas)pSender).CaptureMouse();
        PSMonitorSeekHandle(pSender, pEvent);
    }

    private void PSMonitorSeekHandle(object pSender, MouseEventArgs pEvent)
    {
        var pCanvas = (Canvas)pSender;
        psMonitorSource.QSMonitorSeekHandle(
            pCanvas.IsMouseCaptured,
            pEvent.GetPosition(pCanvas).X,
            pCanvas.ActualWidth,
            psMonitorViewer.QViewerDuration);
    }
}
