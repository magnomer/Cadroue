using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
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
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePicker"),
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
        Source = PIcon.PIconRead(PPickerIcon, pInspectorIconBrush),
        Stretch = Stretch.Uniform
    };

    private ToggleButton PWhitebalancePickerBuild(LNeutralTarget pTarget, Image pPickerIcon, string pTooltipKey)
    {
        var pPickerTool = new ToggleButton
        {
            Content = pPickerIcon,
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorToolCreate(typeof(ToggleButton))
        };
        pPickerTool.Checked += (_, _) => LWhitebalance.LWhitebalanceToolToggle(pTarget, true);
        pPickerTool.Unchecked += (_, _) => LWhitebalance.LWhitebalanceToolToggle(pTarget, false);
        return pPickerTool;
    }

    private void PWhitebalanceToolUpdate()
    {
        pInspectorNeutralTool.IsChecked = PLook.PLookChecked[LWhitebalance.LWhitebalanceGreyArmed];
        pInspectorWhiteTool.IsChecked = PLook.PLookChecked[LWhitebalance.LWhitebalanceWhiteArmed];
        pInspectorNeutralIcon.Source = PIcon.PIconRead(
            PPickerIcon, PInspectorPickBrush[LWhitebalance.LWhitebalanceGreyArmed]);
        pInspectorWhiteIcon.Source = PIcon.PIconRead(
            PPickerIcon, PInspectorPickBrush[LWhitebalance.LWhitebalanceWhiteArmed]);
        pInspectorNeutralStatus.Text = LWhitebalance.LWhitebalanceGuideRead();
        pInspectorNeutralStatus.Visibility = PLook.PLookVisible[LWhitebalance.LWhitebalanceGuideShown];
    }

    private void PWhitebalanceReadoutUpdate()
    {
        LWhitebalanceReadout pReadout = LWhitebalance.LWhitebalanceReadoutRead();
        pInspectorNeutralGroup.Visibility = PLook.PLookVisible[pReadout.LWhitebalanceReadoutSampled];
        pInspectorNeutralSwatch.Fill = new SolidColorBrush(Color.FromRgb(
            pReadout.LWhitebalanceReadoutRed,
            pReadout.LWhitebalanceReadoutGreen,
            pReadout.LWhitebalanceReadoutBlue));
        pInspectorNeutralReadout.Text = pReadout.LWhitebalanceReadoutText;
    }
}
