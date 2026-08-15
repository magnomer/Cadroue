using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const string PPickerIcon = "/PAssets/PPanels/PProcessingPicker.svg";

    private ToggleButton pInspectorNeutralTool = null!;
    private Image pInspectorNeutralIcon = null!;
    private StackPanel pInspectorNeutralGroup = null!;
    private Rectangle pInspectorNeutralSwatch = null!;
    private TextBlock pInspectorNeutralReadout = null!;
    private TextBlock pInspectorNeutralStatus = null!;
    private double pWhitebalanceRedGain = 1;
    private double pWhitebalanceGreenGain = 1;
    private double pWhitebalanceBlueGain = 1;
    private int pWhitebalanceSampleRed;
    private int pWhitebalanceSampleGreen;
    private int pWhitebalanceSampleBlue;
    private bool pInspectorNeutralSuppress;

    public event Action<bool>? PWhitebalanceToolChange;

    private UIElement PToneNeutralBuild()
    {
        pInspectorNeutralIcon = new Image
        {
            Width = 18,
            Height = 18,
            Source = PIcon.PIconRead(PPickerIcon, pInspectorIconBrush),
            Stretch = Stretch.Uniform
        };
        pInspectorNeutralTool = new ToggleButton
        {
            Content = pInspectorNeutralIcon,
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePickTooltip"),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorToolCreate(typeof(ToggleButton))
        };
        pInspectorNeutralTool.Checked += (_, _) =>
        {
            pInspectorNeutralIcon.Source = PIcon.PIconRead(PPickerIcon, pInspectorAccentBrush);
            if (pInspectorNeutralSuppress)
            {
                return;
            }

            pInspectorCropTool.IsChecked = false;
            PInspectorNeutralShow(LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceGuide"));
            PWhitebalanceToolChange?.Invoke(true);
        };
        pInspectorNeutralTool.Unchecked += (_, _) =>
        {
            pInspectorNeutralIcon.Source = PIcon.PIconRead(PPickerIcon, pInspectorIconBrush);
            if (pInspectorNeutralSuppress)
            {
                return;
            }

            PInspectorNeutralShow(string.Empty);
            PWhitebalanceToolChange?.Invoke(false);
        };

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
            pInspectorNeutralTool);

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
        PWhitebalanceReadoutUpdate();
        return pInspectorNeutralColumn;
    }

    public void PInspectorNeutralShow(string pNeutralStatus)
    {
        pInspectorNeutralStatus.Text = pNeutralStatus;
        pInspectorNeutralStatus.Visibility = string.IsNullOrEmpty(pNeutralStatus)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public void PWhitebalanceToolSet(bool pNeutralArmed)
    {
        if (pInspectorNeutralTool.IsChecked == pNeutralArmed)
        {
            return;
        }

        bool pPrevious = pInspectorNeutralSuppress;
        pInspectorNeutralSuppress = true;
        pInspectorNeutralTool.IsChecked = pNeutralArmed;
        pInspectorNeutralSuppress = pPrevious;
    }

    private void PWhitebalanceToolReset()
    {
        if (pInspectorNeutralTool.IsChecked == true)
        {
            pInspectorNeutralTool.IsChecked = false;
        }
    }

    public void PToneNeutralApply(LNeutralSample pNeutralSample)
    {
        PInspectorNeutralShow(string.Empty);
        pWhitebalanceRedGain = pNeutralSample.LNeutralRedGain;
        pWhitebalanceGreenGain = pNeutralSample.LNeutralGreenGain;
        pWhitebalanceBlueGain = pNeutralSample.LNeutralBlueGain;
        pWhitebalanceSampleRed = pNeutralSample.LNeutralRed;
        pWhitebalanceSampleGreen = pNeutralSample.LNeutralGreen;
        pWhitebalanceSampleBlue = pNeutralSample.LNeutralBlue;

        bool pPrevious = pInspectorVideoSuppress;
        pInspectorVideoSuppress = true;
        try
        {
            pWhitebalanceBox.IsChecked = true;
            pWhitebalanceManual = true;
            PWhitebalanceManualUpdate();
            PToneApplyUpdate(pWhitebalanceBox, pWhitebalanceStack);
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }

        PWhitebalanceReadoutUpdate();
        PWhitebalanceToolSet(false);
        PInspectorVideoChange?.Invoke();
    }

    private void PToneNeutralRestore(LWorkWhitebalanceSettings pNeutralWhitebalance)
    {
        if (pNeutralWhitebalance.LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            pWhitebalanceRedGain = pNeutralWhitebalance.LWorkWhitebalanceRed;
            pWhitebalanceGreenGain = pNeutralWhitebalance.LWorkWhitebalanceGreen;
            pWhitebalanceBlueGain = pNeutralWhitebalance.LWorkWhitebalanceBlue;
            pWhitebalanceSampleRed = pNeutralWhitebalance.LWorkSampleRed;
            pWhitebalanceSampleGreen = pNeutralWhitebalance.LWorkSampleGreen;
            pWhitebalanceSampleBlue = pNeutralWhitebalance.LWorkSampleBlue;
        }
        else
        {
            PToneNeutralReset();
        }

        PWhitebalanceReadoutUpdate();
    }

    private void PToneNeutralReset()
    {
        pWhitebalanceRedGain = 1;
        pWhitebalanceGreenGain = 1;
        pWhitebalanceBlueGain = 1;
        pWhitebalanceSampleRed = 0;
        pWhitebalanceSampleGreen = 0;
        pWhitebalanceSampleBlue = 0;
    }

    private void PWhitebalanceReadoutUpdate()
    {
        LNeutralDisplay pNeutralDisplay = LNeutral.LNeutralDisplayResolve(
            pWhitebalanceManual,
            pWhitebalanceSampleRed,
            pWhitebalanceSampleGreen,
            pWhitebalanceSampleBlue);

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
