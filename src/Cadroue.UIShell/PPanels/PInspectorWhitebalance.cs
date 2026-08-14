using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private ToggleButton pInspectorNeutralTool = null!;
    private StackPanel pInspectorNeutralGroup = null!;
    private Rectangle pInspectorNeutralSwatch = null!;
    private TextBlock pInspectorNeutralReadout = null!;
    private TextBlock pInspectorNeutralStatus = null!;
    private double pInspectorWhitebalanceRedGain = 1;
    private double pInspectorWhitebalanceGreenGain = 1;
    private double pInspectorWhitebalanceBlueGain = 1;
    private int pInspectorWhitebalanceSampleRed;
    private int pInspectorWhitebalanceSampleGreen;
    private int pInspectorWhitebalanceSampleBlue;
    private bool pInspectorNeutralSuppress;

    public event Action<bool>? PInspectorNeutralToolChange;

    private UIElement PToneNeutralBuild()
    {
        pInspectorNeutralTool = new ToggleButton
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePick"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePickTooltip"),
            Height = 28,
            MinWidth = 96,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Style = PInspectorToolCreate(typeof(ToggleButton)),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        pInspectorNeutralTool.Checked += (_, _) =>
        {
            pInspectorNeutralTool.Content = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceCancel");
            if (pInspectorNeutralSuppress)
            {
                return;
            }

            pInspectorCropTool.IsChecked = false;
            PInspectorNeutralShow(LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceGuide"));
            PInspectorNeutralToolChange?.Invoke(true);
        };
        pInspectorNeutralTool.Unchecked += (_, _) =>
        {
            pInspectorNeutralTool.Content = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePick");
            if (pInspectorNeutralSuppress)
            {
                return;
            }

            PInspectorNeutralShow(string.Empty);
            PInspectorNeutralToolChange?.Invoke(false);
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

        var pInspectorNeutralRow = new StackPanel { Orientation = Orientation.Horizontal };
        pInspectorNeutralRow.Children.Add(pInspectorNeutralTool);
        pInspectorNeutralRow.Children.Add(pInspectorNeutralSwatch);
        pInspectorNeutralRow.Children.Add(pInspectorNeutralReadout);

        var pInspectorNeutralColumn = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        pInspectorNeutralColumn.Children.Add(pInspectorNeutralRow);
        pInspectorNeutralColumn.Children.Add(pInspectorNeutralStatus);
        pInspectorNeutralGroup = pInspectorNeutralColumn;
        PToneNeutralReadoutUpdate();
        return pInspectorNeutralColumn;
    }

    public void PInspectorNeutralShow(string pNeutralStatus)
    {
        pInspectorNeutralStatus.Text = pNeutralStatus;
        pInspectorNeutralStatus.Visibility = string.IsNullOrEmpty(pNeutralStatus)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public void PInspectorNeutralToolSet(bool pNeutralArmed)
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

    public void PToneNeutralApply(LNeutralSample pNeutralSample)
    {
        PInspectorNeutralShow(string.Empty);
        pInspectorWhitebalanceRedGain = pNeutralSample.LNeutralRedGain;
        pInspectorWhitebalanceGreenGain = pNeutralSample.LNeutralGreenGain;
        pInspectorWhitebalanceBlueGain = pNeutralSample.LNeutralBlueGain;
        pInspectorWhitebalanceSampleRed = pNeutralSample.LNeutralRed;
        pInspectorWhitebalanceSampleGreen = pNeutralSample.LNeutralGreen;
        pInspectorWhitebalanceSampleBlue = pNeutralSample.LNeutralBlue;

        bool pPrevious = pInspectorVideoSuppress;
        pInspectorVideoSuppress = true;
        try
        {
            pToneWhitebalanceBox.IsChecked = true;
            pInspectorWhitebalanceMethod.SelectedIndex = 3;
            PToneApplyUpdate(pToneWhitebalanceBox, pInspectorWhitebalanceStack);
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }

        PToneNeutralReadoutUpdate();
        PInspectorNeutralToolSet(false);
        PInspectorVideoChange?.Invoke();
    }

    private void PToneNeutralRestore(LWorkWhitebalanceSettings pNeutralWhitebalance)
    {
        if (pNeutralWhitebalance.LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            pInspectorWhitebalanceRedGain = pNeutralWhitebalance.LWorkWhitebalanceRed;
            pInspectorWhitebalanceGreenGain = pNeutralWhitebalance.LWorkWhitebalanceGreen;
            pInspectorWhitebalanceBlueGain = pNeutralWhitebalance.LWorkWhitebalanceBlue;
            pInspectorWhitebalanceSampleRed = pNeutralWhitebalance.LWorkSampleRed;
            pInspectorWhitebalanceSampleGreen = pNeutralWhitebalance.LWorkSampleGreen;
            pInspectorWhitebalanceSampleBlue = pNeutralWhitebalance.LWorkSampleBlue;
        }
        else
        {
            PToneNeutralReset();
        }

        PToneNeutralReadoutUpdate();
    }

    private void PToneNeutralReset()
    {
        pInspectorWhitebalanceRedGain = 1;
        pInspectorWhitebalanceGreenGain = 1;
        pInspectorWhitebalanceBlueGain = 1;
        pInspectorWhitebalanceSampleRed = 0;
        pInspectorWhitebalanceSampleGreen = 0;
        pInspectorWhitebalanceSampleBlue = 0;
    }

    private void PToneNeutralReadoutUpdate()
    {
        LNeutralDisplay pNeutralDisplay = LNeutral.LNeutralDisplayResolve(
            pInspectorWhitebalanceMethod.SelectedIndex == 3,
            pInspectorWhitebalanceSampleRed,
            pInspectorWhitebalanceSampleGreen,
            pInspectorWhitebalanceSampleBlue);

        pInspectorNeutralGroup.Visibility = pNeutralDisplay.LNeutralDisplayVisible
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
