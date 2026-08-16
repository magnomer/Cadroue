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

    private CheckBox pWhitebalanceBox = null!;
    private CheckBox pWhitebalancePersistent = null!;
    private ComboBox pWhitebalanceMethod = null!;
    private Slider pWhitebalanceSaturationSlider = null!;
    private TextBox pWhitebalanceSaturationValue = null!;
    private StackPanel pWhitebalanceStack = null!;
    private StackPanel pWhitebalanceBody = null!;
    private UIElement pWhitebalanceMethodField = null!;
    private UIElement pWhitebalanceSaturationField = null!;
    private bool pWhitebalanceCapable;
    private bool pWhitebalanceManual;

    private StackPanel PWhitebalanceBuild()
    {
        pWhitebalanceBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyWhitebalance"));
        pWhitebalancePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistWhitebalance"));
        pWhitebalanceMethod = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pWhitebalanceMethod);
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Average", "Inspector.Video.WhitebalanceMethodAverage"));
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Minmax", "Inspector.Video.WhitebalanceMethodMinmax"));
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Median", "Inspector.Video.WhitebalanceMethodMedian"));
        pWhitebalanceMethod.SelectedIndex = 2;
        pWhitebalanceMethod.SelectionChanged += (_, _) =>
        {
            PWhitebalanceReadoutUpdate();
            if (!pInspectorVideoSuppress)
            {
                PInspectorVideoChange?.Invoke();
            }
        };

        pWhitebalanceSaturationSlider = PToneSliderBuild(0, 300, 100);
        pWhitebalanceSaturationValue = PInspectorDecimalBuild();
        pWhitebalanceSaturationValue.Text = "100";
        pWhitebalanceStack = new StackPanel();
        PInspectorVideoAttach(
            pWhitebalanceBox,
            pWhitebalanceStack,
            pWhitebalanceSaturationSlider,
            pWhitebalanceSaturationValue,
            0,
            300,
            "0.#");
        pWhitebalanceMethodField = PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceMethod"),
            pWhitebalanceMethod);
        pWhitebalanceSaturationField = PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceSaturation"),
            pWhitebalanceSaturationSlider,
            "%",
            pWhitebalanceSaturationValue);
        pWhitebalanceStack.Children.Add(PToneNeutralBuild());
        pWhitebalanceStack.Children.Add(pWhitebalanceMethodField);
        pWhitebalanceStack.Children.Add(pWhitebalanceSaturationField);
        pWhitebalanceStack.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceWarning"),
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x64, 0x70, 0x82)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 8)
        });
        var pWhitebalanceReset = new Button
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceReset"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceResetTooltip"),
            Height = 28,
            MinWidth = 64,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Style = PButton.PButtonPanelCreate(),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pWhitebalanceReset.Click += (_, _) => PWhitebalanceReset();
        pWhitebalanceStack.Children.Add(pWhitebalanceReset);
        pWhitebalanceBody = PToneBodyBuild(pWhitebalanceBox, pWhitebalanceStack);
        PToneApplyUpdate(pWhitebalanceBox, pWhitebalanceStack);
        PWhitebalanceManualUpdate();
        return pWhitebalanceBody;
    }

    private LWhitebalanceMethod PWhitebalanceMethodRead() =>
        pWhitebalanceManual
            ? LWhitebalanceMethod.LWhitebalanceMethodManual
            : pWhitebalanceMethod.SelectedIndex switch
            {
                0 => LWhitebalanceMethod.LWhitebalanceMethodAverage,
                1 => LWhitebalanceMethod.LWhitebalanceMethodMinmax,
                _ => LWhitebalanceMethod.LWhitebalanceMethodMedian
            };

    private static int PWhitebalanceIndexRead(LWhitebalanceMethod pMethod) => pMethod switch
    {
        LWhitebalanceMethod.LWhitebalanceMethodAverage => 0,
        LWhitebalanceMethod.LWhitebalanceMethodMinmax => 1,
        _ => 2
    };

    private void PWhitebalanceManualUpdate()
    {
        bool pEnabled = !pWhitebalanceManual;
        pWhitebalanceMethodField.IsEnabled = pEnabled;
        pWhitebalanceMethodField.Opacity = pEnabled ? 1 : 0.4;
        pWhitebalanceSaturationField.IsEnabled = pEnabled;
        pWhitebalanceSaturationField.Opacity = pEnabled ? 1 : 0.4;
    }

    private void PWhitebalanceReset()
    {
        bool pPrevious = pInspectorVideoSuppress;
        bool pChanged = PWhitebalanceMethodRead() != LWhitebalanceMethod.LWhitebalanceMethodMedian
            || PInspectorDecimalRead(
                pWhitebalanceSaturationValue,
                pWhitebalanceSaturationSlider.Value) != 100
            || pWhitebalanceSampleRed != 0
            || pWhitebalanceSampleGreen != 0
            || pWhitebalanceSampleBlue != 0
            || pWhitebalanceRedGain != 1
            || pWhitebalanceGreenGain != 1
            || pWhitebalanceBlueGain != 1;
        pInspectorVideoSuppress = true;
        try
        {
            PWhitebalanceToolReset();
            pWhitebalanceManual = false;
            pWhitebalanceMethod.SelectedIndex = 2;
            PInspectorValueSet(
                pWhitebalanceSaturationSlider,
                pWhitebalanceSaturationValue,
                100);
            PToneNeutralReset();
            PWhitebalanceReadoutUpdate();
            PWhitebalanceManualUpdate();
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }

        if (!pPrevious && pChanged)
        {
            PInspectorVideoChange?.Invoke();
        }
    }

    public void PWhitebalanceCapabilitySet(bool pWhitebalanceCapable)
    {
        this.pWhitebalanceCapable = pWhitebalanceCapable;
        pWhitebalanceBox.IsEnabled = pWhitebalanceCapable;
        pWhitebalancePersistent.IsEnabled = pWhitebalanceCapable;
        pWhitebalanceStack.IsEnabled =
            pWhitebalanceCapable && pWhitebalanceBox.IsChecked == true;
        pWhitebalanceStack.Opacity =
            pWhitebalanceCapable && pWhitebalanceBox.IsChecked == true ? 1 : 0.4;
        string? pDisabledTooltip = pWhitebalanceCapable
            ? null
            : LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceRequiresMpv");
        pWhitebalanceBody.ToolTip = pDisabledTooltip;
        pWhitebalanceBox.ToolTip = pDisabledTooltip
            ?? LLocalization.LLocalizationTextRead("Inspector.Video.ApplyWhitebalance");
        pWhitebalancePersistent.ToolTip = pDisabledTooltip
            ?? LLocalization.LLocalizationTextRead("Inspector.Video.PersistWhitebalance");
        pInspectorNeutralTool.IsEnabled = pWhitebalanceCapable;
        pInspectorNeutralTool.ToolTip = pDisabledTooltip
            ?? LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePickTooltip");
        ToolTipService.SetShowOnDisabled(pWhitebalanceBody, true);
        ToolTipService.SetShowOnDisabled(pWhitebalanceBox, true);
        ToolTipService.SetShowOnDisabled(pWhitebalancePersistent, true);
        ToolTipService.SetShowOnDisabled(pInspectorNeutralTool, true);
        if (!pWhitebalanceCapable)
        {
            PWhitebalanceToolReset();
        }
    }
}
