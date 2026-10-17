using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private const string PPickerIcon = "/PAsset/PPanel/PProcessingPicker.svg";

    private ToggleButton pInspectorNeutralTool = null!;
    private ToggleButton pInspectorWhiteTool = null!;
    private Image pInspectorNeutralIcon = null!;
    private Image pInspectorWhiteIcon = null!;
    private StackPanel pInspectorNeutralGroup = null!;
    private Rectangle pInspectorNeutralSwatch = null!;
    private TextBlock pInspectorNeutralReadout = null!;
    private TextBlock pInspectorNeutralStatus = null!;

    private UIElement PToneNeutralBuild()
    {
        pInspectorNeutralIcon = PWhitebalanceIconBuild();
        pInspectorWhiteIcon = PWhitebalanceIconBuild();
        pInspectorNeutralTool = PWhitebalancePickerBuild(
            LNeutralTarget.LNeutralTargetGrey,
            pInspectorNeutralIcon,
            "Inspector.Video.WhitebalancePickTooltip");
        pInspectorWhiteTool = PWhitebalancePickerBuild(
            LNeutralTarget.LNeutralTargetWhite,
            pInspectorWhiteIcon,
            "Inspector.Video.WhitebalancePickWhiteTooltip");

        var pInspectorNeutralTools = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pInspectorNeutralTools.Children.Add(pInspectorNeutralTool);
        pInspectorWhiteTool.Margin = new Thickness(6, 0, 0, 0);
        pInspectorNeutralTools.Children.Add(pInspectorWhiteTool);

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
            FontFamily = pInspectorFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x70, 0x82)),
            VerticalAlignment = VerticalAlignment.Center
        };

        pInspectorNeutralStatus = new TextBlock
        {
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x70, 0x82)),
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 0, 4)
        };

        UIElement pInspectorNeutralField = PInspectorFieldBuild(
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

        var pInspectorNeutralColumn = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        pInspectorNeutralColumn.Children.Add(pInspectorNeutralField);
        pInspectorNeutralColumn.Children.Add(pInspectorNeutralRow);
        pInspectorNeutralColumn.Children.Add(pInspectorNeutralStatus);
        pInspectorNeutralGroup = pInspectorNeutralRow;
        return pInspectorNeutralColumn;
    }

    private static Image PWhitebalanceIconBuild() => new()
    {
        Width = 18,
        Height = 18,
        Source = QIcon.QIconRead(PPickerIcon, pInspectorIconBrush),
        Stretch = Stretch.Uniform
    };

    private ToggleButton PWhitebalancePickerBuild(LNeutralTarget pTarget, Image pPickerIcon, string pTooltipKey)
    {
        ToggleButton pPickerTool = PWhitebalanceToggleBuild(pPickerIcon, pTooltipKey);
        PWhitebalancePickerAttach(pPickerTool, pTarget);
        return pPickerTool;
    }

    private static ToggleButton PWhitebalanceToggleBuild(Image pPickerIcon, string pTooltipKey) => new()
    {
        Content = pPickerIcon,
        ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTooltipKey),
        Width = 32,
        Height = 32,
        VerticalAlignment = VerticalAlignment.Center,
        Style = PInspectorToolCreate(typeof(ToggleButton))
    };

    private void PWhitebalancePickerAttach(ToggleButton pPickerTool, LNeutralTarget pTarget)
    {
        pPickerTool.Checked += (_, _) => QWhitebalance.QWhitebalanceToolToggle(pTarget, true);
        pPickerTool.Unchecked += (_, _) => QWhitebalance.QWhitebalanceToolToggle(pTarget, false);
    }

    private void PWhitebalanceToolUpdate()
    {
        PWhitebalanceToolApply();
        PWhitebalanceIconApply();
        PWhitebalanceStatusApply();
    }

    private void PWhitebalanceToolApply()
    {
        pInspectorNeutralTool.IsChecked = PLook.PLookChecked[QWhitebalance.QWhitebalanceGreyArmed];
        pInspectorWhiteTool.IsChecked = PLook.PLookChecked[QWhitebalance.QWhitebalanceWhiteArmed];
    }

    private void PWhitebalanceIconApply()
    {
        pInspectorNeutralIcon.Source = QIcon.QIconRead(
            PPickerIcon, PInspectorPickBrush[QWhitebalance.QWhitebalanceGreyArmed]);
        pInspectorWhiteIcon.Source = QIcon.QIconRead(
            PPickerIcon, PInspectorPickBrush[QWhitebalance.QWhitebalanceWhiteArmed]);
    }

    private void PWhitebalanceStatusApply()
    {
        pInspectorNeutralStatus.Text = QWhitebalance.QWhitebalanceGuideRead();
        pInspectorNeutralStatus.Visibility = PLook.PLookVisible[QWhitebalance.QWhitebalanceGuideShown];
    }

    private void PWhitebalanceReadoutUpdate()
    {
        QWhitebalanceReadout pReadout = QWhitebalance.QWhitebalanceReadoutRead();
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
