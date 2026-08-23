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
    private ToggleButton pInspectorWhiteTool = null!;
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

    public event Action<bool, LNeutralTarget>? PWhitebalanceToolChange;

    private UIElement PToneNeutralBuild()
    {
        pInspectorNeutralTool = PWhitebalancePickerBuild(
            LNeutralTarget.LNeutralTargetGrey,
            "Inspector.Video.WhitebalancePickTooltip",
            "Inspector.Video.WhitebalanceGuide");
        pInspectorWhiteTool = PWhitebalancePickerBuild(
            LNeutralTarget.LNeutralTargetWhite,
            "Inspector.Video.WhitebalancePickWhiteTooltip",
            "Inspector.Video.WhitebalanceGuideWhite");

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
        PWhitebalanceReadoutUpdate();
        return pInspectorNeutralColumn;
    }

    // One eyedropper toggle. Grey samples a neutral point (strict); White samples any
    // point on the black-to-white axis (lenient). Both feed one correction pipeline,
    // differing only in the target they hand the viewer's sampler.
    private ToggleButton PWhitebalancePickerBuild(
        LNeutralTarget pTarget, string pTooltipKey, string pGuideKey)
    {
        var pPickerIcon = new Image
        {
            Width = 18,
            Height = 18,
            Source = PIcon.PIconRead(PPickerIcon, pInspectorIconBrush),
            Stretch = Stretch.Uniform
        };
        var pPickerTool = new ToggleButton
        {
            Content = pPickerIcon,
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorToolCreate(typeof(ToggleButton))
        };
        pPickerTool.Checked += (_, _) =>
        {
            pPickerIcon.Source = PIcon.PIconRead(PPickerIcon, pInspectorAccentBrush);
            if (pInspectorNeutralSuppress)
            {
                return;
            }

            PWhitebalancePickerSelect(pTarget);
            PInspectorNeutralShow(LLocalization.LLocalizationTextRead(pGuideKey));
            PWhitebalanceToolChange?.Invoke(true, pTarget);
        };
        pPickerTool.Unchecked += (_, _) =>
        {
            pPickerIcon.Source = PIcon.PIconRead(PPickerIcon, pInspectorIconBrush);
            if (pInspectorNeutralSuppress)
            {
                return;
            }

            if (PWhitebalancePeerRead(pTarget).IsChecked == true)
            {
                // Switching to the other picker: it will arm the tool itself.
                return;
            }

            PInspectorNeutralShow(string.Empty);
            PWhitebalanceToolChange?.Invoke(false, pTarget);
        };
        return pPickerTool;
    }

    private ToggleButton PWhitebalancePeerRead(LNeutralTarget pTarget) =>
        pTarget == LNeutralTarget.LNeutralTargetWhite ? pInspectorNeutralTool : pInspectorWhiteTool;

    // Make this picker the active one: deactivate its peer and the crop tool without
    // firing their disarm side effects.
    private void PWhitebalancePickerSelect(LNeutralTarget pTarget)
    {
        bool pPrevious = pInspectorNeutralSuppress;
        pInspectorNeutralSuppress = true;
        PWhitebalancePeerRead(pTarget).IsChecked = false;
        pInspectorNeutralSuppress = pPrevious;
        pInspectorCropTool.IsChecked = false;
    }

    public void PInspectorNeutralShow(string pNeutralStatus)
    {
        pInspectorNeutralStatus.Text = pNeutralStatus;
        pInspectorNeutralStatus.Visibility = string.IsNullOrEmpty(pNeutralStatus)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public void PWhitebalanceToolSet(bool pNeutralArmed, LNeutralTarget pTarget)
    {
        ToggleButton pTool = pTarget == LNeutralTarget.LNeutralTargetWhite
            ? pInspectorWhiteTool
            : pInspectorNeutralTool;
        bool pPrevious = pInspectorNeutralSuppress;
        pInspectorNeutralSuppress = true;
        if (pNeutralArmed)
        {
            pTool.IsChecked = true;
        }
        else
        {
            pInspectorNeutralTool.IsChecked = false;
            pInspectorWhiteTool.IsChecked = false;
        }

        pInspectorNeutralSuppress = pPrevious;
    }

    private void PWhitebalanceToolReset()
    {
        bool pPrevious = pInspectorNeutralSuppress;
        pInspectorNeutralSuppress = true;
        pInspectorNeutralTool.IsChecked = false;
        pInspectorWhiteTool.IsChecked = false;
        pInspectorNeutralSuppress = pPrevious;
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
        PWhitebalanceWheelUpdate();
        PWhitebalanceToolSet(false, LNeutralTarget.LNeutralTargetGrey);
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
        PWhitebalanceWheelUpdate();
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
