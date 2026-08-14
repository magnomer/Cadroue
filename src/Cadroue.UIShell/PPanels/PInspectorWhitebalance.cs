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
    private Rectangle pInspectorNeutralSwatch = null!;
    private TextBlock pInspectorNeutralReadout = null!;
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
            if (pInspectorNeutralSuppress)
            {
                return;
            }

            pInspectorCropTool.IsChecked = false;
            PInspectorNeutralToolChange?.Invoke(true);
        };
        pInspectorNeutralTool.Unchecked += (_, _) =>
        {
            if (!pInspectorNeutralSuppress)
            {
                PInspectorNeutralToolChange?.Invoke(false);
            }
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

        var pInspectorNeutralRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pInspectorNeutralRow.Children.Add(pInspectorNeutralTool);
        pInspectorNeutralRow.Children.Add(pInspectorNeutralSwatch);
        pInspectorNeutralRow.Children.Add(pInspectorNeutralReadout);
        PToneNeutralReadoutUpdate();
        return pInspectorNeutralRow;
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

    private void PToneNeutralClear()
    {
        PToneNeutralReset();
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
        bool pNeutralActive = pInspectorWhitebalanceMethod.SelectedIndex == 3
            && (pInspectorWhitebalanceSampleRed
                | pInspectorWhitebalanceSampleGreen
                | pInspectorWhitebalanceSampleBlue) != 0;

        if (!pNeutralActive)
        {
            pInspectorNeutralSwatch.Fill = Brushes.Transparent;
            pInspectorNeutralReadout.Text = string.Empty;
            return;
        }

        pInspectorNeutralSwatch.Fill = new SolidColorBrush(Color.FromRgb(
            (byte)pInspectorWhitebalanceSampleRed,
            (byte)pInspectorWhitebalanceSampleGreen,
            (byte)pInspectorWhitebalanceSampleBlue));
        pInspectorNeutralReadout.Text = string.Format(
            CultureInfo.InvariantCulture,
            "R {0:0.00}  G {1:0.00}  B {2:0.00}",
            pInspectorWhitebalanceRedGain,
            pInspectorWhitebalanceGreenGain,
            pInspectorWhitebalanceBlueGain);
    }
}
