using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorWhitebalanceNeutral
{
    internal const string PPickerIcon = "/PAsset/PPanel/PProcessingPicker.svg";

    private readonly Image pInspectorNeutralIcon;
    private readonly Image pInspectorWhiteIcon;
    private readonly StackPanel pInspectorNeutralGroup;
    private readonly Rectangle pInspectorNeutralSwatch;
    private readonly TextBlock pInspectorNeutralReadout;
    private readonly TextBlock pInspectorNeutralStatus;

    internal PInspectorWhitebalanceNeutral()
    {
        pInspectorNeutralIcon = PWhitebalanceIconBuild();
        pInspectorWhiteIcon = PWhitebalanceIconBuild();
        PInspectorNeutralTool = PWhitebalanceToggleBuild(
            pInspectorNeutralIcon,
            "Inspector.Video.WhitebalancePickTooltip");
        PInspectorWhiteTool = PWhitebalanceToggleBuild(
            pInspectorWhiteIcon,
            "Inspector.Video.WhitebalancePickWhiteTooltip");

        var pInspectorNeutralTools = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pInspectorNeutralTools.Children.Add(PInspectorNeutralTool);
        PInspectorWhiteTool.Margin = new Thickness(6, 0, 0, 0);
        pInspectorNeutralTools.Children.Add(PInspectorWhiteTool);

        pInspectorNeutralSwatch = new Rectangle
        {
            Width = 20,
            Height = 20,
            RadiusX = 3,
            RadiusY = 3,
            Stroke = new SolidColorBrush(Color.FromRgb(0x9A, 0xA6, 0xB8)),
            StrokeThickness = 1,
            Fill = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 6, 0)
        };
        pInspectorNeutralReadout = new TextBlock
        {
            FontSize = 11,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x70, 0x82)),
            VerticalAlignment = VerticalAlignment.Center
        };

        pInspectorNeutralStatus = new TextBlock
        {
            FontSize = 11,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x70, 0x82)),
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 0, 4)
        };

        UIElement pInspectorNeutralField = PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalancePicker"),
            pInspectorNeutralTools,
            true);

        var pInspectorNeutralRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 0)
        };
        pInspectorNeutralRow.Children.Add(pInspectorNeutralSwatch);
        pInspectorNeutralRow.Children.Add(pInspectorNeutralReadout);

        PInspectorNeutralColumn = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        PInspectorNeutralColumn.Children.Add(pInspectorNeutralField);
        PInspectorNeutralColumn.Children.Add(pInspectorNeutralRow);
        PInspectorNeutralColumn.Children.Add(pInspectorNeutralStatus);
        pInspectorNeutralGroup = pInspectorNeutralRow;
    }

    internal ToggleButton PInspectorNeutralTool { get; }

    internal ToggleButton PInspectorWhiteTool { get; }

    internal StackPanel PInspectorNeutralColumn { get; }

    private static Image PWhitebalanceIconBuild() => new()
    {
        Width = 18,
        Height = 18,
        Source = QIcon.QIconRead(PPickerIcon, PInspectorLook.PInspectorIconBrush),
        Stretch = Stretch.Uniform
    };

    private static ToggleButton PWhitebalanceToggleBuild(Image pPickerIcon, string pTooltipKey) => new()
    {
        Content = pPickerIcon,
        ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTooltipKey),
        Width = 32,
        Height = 32,
        VerticalAlignment = VerticalAlignment.Center,
        Style = PInspectorTool.PInspectorToolCreate(typeof(ToggleButton))
    };

    internal void PWhitebalancePickerApply(QWhitebalance lWhitebalance, QInspectorTip pTip)
    {
        PInspectorNeutralTool.IsEnabled = lWhitebalance.QWhitebalanceCapable;
        PInspectorNeutralTool.ToolTip = pTip.QInspectorNoticeRead("Inspector.Video.WhitebalancePickTooltip");
        PInspectorWhiteTool.IsEnabled = lWhitebalance.QWhitebalanceCapable;
        PInspectorWhiteTool.ToolTip = pTip.QInspectorNoticeRead("Inspector.Video.WhitebalancePickWhiteTooltip");
        ToolTipService.SetShowOnDisabled(PInspectorNeutralTool, true);
        ToolTipService.SetShowOnDisabled(PInspectorWhiteTool, true);
    }

    internal void PWhitebalanceToolUpdate(QWhitebalance lWhitebalance)
    {
        PWhitebalanceToolApply(lWhitebalance);
        PWhitebalanceIconApply(lWhitebalance);
        PWhitebalanceStatusApply(lWhitebalance);
    }

    private void PWhitebalanceToolApply(QWhitebalance lWhitebalance)
    {
        PInspectorNeutralTool.IsChecked = PLook.PLookChecked[lWhitebalance.QWhitebalanceGreyArmed];
        PInspectorWhiteTool.IsChecked = PLook.PLookChecked[lWhitebalance.QWhitebalanceWhiteArmed];
    }

    private void PWhitebalanceIconApply(QWhitebalance lWhitebalance)
    {
        pInspectorNeutralIcon.Source = QIcon.QIconRead(
            PPickerIcon, PInspectorLook.PInspectorPickBrush[lWhitebalance.QWhitebalanceGreyArmed]);
        pInspectorWhiteIcon.Source = QIcon.QIconRead(
            PPickerIcon, PInspectorLook.PInspectorPickBrush[lWhitebalance.QWhitebalanceWhiteArmed]);
    }

    private void PWhitebalanceStatusApply(QWhitebalance lWhitebalance)
    {
        pInspectorNeutralStatus.Text = lWhitebalance.QWhitebalanceGuideRead();
        pInspectorNeutralStatus.Visibility = PLook.PLookVisible[lWhitebalance.QWhitebalanceGuideShown];
    }

    internal void PWhitebalanceReadoutUpdate(QWhitebalance lWhitebalance)
    {
        QWhitebalanceReadout pReadout = lWhitebalance.QWhitebalanceReadoutRead();
        PWhitebalanceGroupApply(pReadout.QWhitebalanceReadoutSampled);
        PWhitebalanceSwatchApply(pReadout);
        PWhitebalanceReadoutApply(pReadout.QWhitebalanceReadoutText);
    }

    private void PWhitebalanceGroupApply(bool pSampled) =>
        pInspectorNeutralGroup.Visibility = PLook.PLookVisible[pSampled];

    private void PWhitebalanceSwatchApply(QWhitebalanceReadout pReadout) =>
        pInspectorNeutralSwatch.Fill = new SolidColorBrush(Color.FromRgb(
            pReadout.QWhitebalanceReadoutRed,
            pReadout.QWhitebalanceReadoutGreen,
            pReadout.QWhitebalanceReadoutBlue));

    private void PWhitebalanceReadoutApply(string pReadoutText) => pInspectorNeutralReadout.Text = pReadoutText;
}
