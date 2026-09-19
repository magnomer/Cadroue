using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;

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

    public event Action<bool, LNeutralTarget>? PWhitebalanceToolChange;

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
        pPickerTool.Checked += (_, _) => LWhitebalance.LWhitebalanceToolSet(true, pTarget);
        pPickerTool.Unchecked += (_, _) =>
        {
            if (LWhitebalance.LWhitebalanceToolArmed && LWhitebalance.LWhitebalanceTarget == pTarget)
            {
                LWhitebalance.LWhitebalanceToolSet(false, pTarget);
            }
        };
        return pPickerTool;
    }

    public void PInspectorNeutralShow(string pNeutralStatus) => LWhitebalance.LWhitebalanceStatusSet(pNeutralStatus);

    public void PWhitebalanceToolSet(bool pNeutralArmed, LNeutralTarget pTarget) =>
        LWhitebalance.LWhitebalanceToolSet(pNeutralArmed, pTarget);

    public void PToneNeutralApply(LNeutralSample pNeutralSample) =>
        LWhitebalance.LWhitebalanceSampleSet(pNeutralSample);

    private void PWhitebalanceToolUpdate()
    {
        bool pArmed = LWhitebalance.LWhitebalanceToolArmed;
        LNeutralTarget pTarget = LWhitebalance.LWhitebalanceTarget;
        bool pGreyWas = pInspectorNeutralTool.IsChecked == true;
        bool pWhiteWas = pInspectorWhiteTool.IsChecked == true;
        bool pGrey = pArmed && pTarget == LNeutralTarget.LNeutralTargetGrey;
        bool pWhite = pArmed && pTarget == LNeutralTarget.LNeutralTargetWhite;
        pInspectorNeutralTool.IsChecked = pGrey;
        pInspectorWhiteTool.IsChecked = pWhite;
        pInspectorNeutralIcon.Source = PIcon.PIconRead(
            PPickerIcon, pGrey ? pInspectorAccentBrush : pInspectorIconBrush);
        pInspectorWhiteIcon.Source = PIcon.PIconRead(PPickerIcon, pWhite ? pInspectorAccentBrush : pInspectorIconBrush);
        if (pArmed)
        {
            LInspector.LInspectorToolSet(false);
        }

        string pStatus = pArmed
            ? LLocalization.LLocalizationTextRead(pTarget == LNeutralTarget.LNeutralTargetWhite
                ? "Inspector.Video.WhitebalanceGuideWhite"
                : "Inspector.Video.WhitebalanceGuide")
            : LWhitebalance.LWhitebalanceStatus;
        pInspectorNeutralStatus.Text = pStatus;
        pInspectorNeutralStatus.Visibility = string.IsNullOrEmpty(pStatus)
            ? Visibility.Collapsed
            : Visibility.Visible;

        bool pWasArmed = pGreyWas || pWhiteWas;
        if (pArmed && (!pWasArmed || pGrey != pGreyWas))
        {
            PWhitebalanceToolChange?.Invoke(true, pTarget);
        }
        else if (!pArmed && pWasArmed)
        {
            PWhitebalanceToolChange?.Invoke(false, pTarget);
        }
    }

    private void PWhitebalanceReadoutUpdate()
    {
        LNeutralDisplay pNeutralDisplay = LWhitebalance.LWhitebalanceDisplayRead();

        pInspectorNeutralGroup.Visibility = pNeutralDisplay.LNeutralDisplaySampled
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!pNeutralDisplay.LNeutralDisplaySampled)
        {
            pInspectorNeutralSwatch.Fill = Brushes.Transparent;
            pInspectorNeutralReadout.Text = string.Empty;
            return;
        }

        pInspectorNeutralSwatch.Fill = new SolidColorBrush(Color.FromRgb(
            (byte)pNeutralDisplay.LNeutralDisplayRed,
            (byte)pNeutralDisplay.LNeutralDisplayGreen,
            (byte)pNeutralDisplay.LNeutralDisplayBlue));
        pInspectorNeutralReadout.Text = string.Format(
            CultureInfo.InvariantCulture,
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceSample"),
            pNeutralDisplay.LNeutralDisplayRed,
            pNeutralDisplay.LNeutralDisplayGreen,
            pNeutralDisplay.LNeutralDisplayBlue);
    }
}
