using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
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
    private bool pWhitebalancePreview;
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
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Custom", "Inspector.Video.WhitebalanceMethodCustom"));
        pWhitebalanceMethod.SelectedIndex = 2;
        pWhitebalanceMethod.SelectionChanged += (_, _) =>
        {
            pWhitebalanceManual = pWhitebalanceMethod.SelectedIndex == 3;
            PWhitebalanceReadoutUpdate();
            PWhitebalanceWheelUpdate();
            if (pInspectorVideoSuppress)
            {
                return;
            }

            if (!pWhitebalanceManual)
            {
                pWhitebalanceWheelPresent = false;
                PWhitebalanceWheelPlace();
                PWhitebalanceEstimateRaise();
            }

            PInspectorVideoChange?.Invoke();
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
        pWhitebalanceStack.Children.Add(PWhitebalanceWheelBuild());
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

    public LWhitebalanceMethod PWhitebalanceMethodRead() =>
        pWhitebalanceMethod.SelectedIndex switch
        {
            0 => LWhitebalanceMethod.LWhitebalanceMethodAverage,
            1 => LWhitebalanceMethod.LWhitebalanceMethodMinmax,
            3 => LWhitebalanceMethod.LWhitebalanceMethodManual,
            _ => LWhitebalanceMethod.LWhitebalanceMethodMedian
        };

    private static int PWhitebalanceIndexRead(LWhitebalanceMethod pMethod) => pMethod switch
    {
        LWhitebalanceMethod.LWhitebalanceMethodAverage => 0,
        LWhitebalanceMethod.LWhitebalanceMethodMinmax => 1,
        LWhitebalanceMethod.LWhitebalanceMethodManual => 3,
        _ => 2
    };

    // Method and Saturation stay live in every mode; Custom is just the combo's face
    // for a manual pick, so this only keeps the combo selection and manual flag aligned.
    private void PWhitebalanceManualUpdate()
    {
        int pWhitebalanceTarget = pWhitebalanceManual ? 3 : pWhitebalanceMethod.SelectedIndex;
        if (pWhitebalanceTarget == 3 && pWhitebalanceMethod.SelectedIndex != 3)
        {
            pWhitebalanceMethod.SelectedIndex = 3;
        }
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
            pWhitebalanceWheelPresent = false;
            PWhitebalanceWheelPlace();
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }

        if (!pPrevious)
        {
            PWhitebalanceEstimateRaise();
        }

        if (!pPrevious && pChanged)
        {
            PInspectorVideoChange?.Invoke();
        }
    }

    public void PWhitebalanceCapabilitySet(bool pWhitebalanceCapable, bool pWhitebalancePreview)
    {
        this.pWhitebalanceCapable = pWhitebalanceCapable;
        this.pWhitebalancePreview = pWhitebalancePreview;
        pWhitebalanceBox.IsEnabled = pWhitebalanceCapable;
        pWhitebalancePersistent.IsEnabled = pWhitebalanceCapable;
        pWhitebalanceStack.IsEnabled =
            pWhitebalanceCapable && pWhitebalanceBox.IsChecked == true;
        pWhitebalanceStack.Opacity =
            pWhitebalanceCapable && pWhitebalanceBox.IsChecked == true ? 1 : 0.4;
        string? pNotice = !pWhitebalanceCapable
            ? LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceRequiresEq")
            : !pWhitebalancePreview
                ? LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePreviewMpv")
                : null;
        pWhitebalanceBody.ToolTip = pNotice;
        pWhitebalanceBox.ToolTip = pNotice
            ?? LLocalization.LLocalizationTextRead("Inspector.Video.ApplyWhitebalance");
        pWhitebalancePersistent.ToolTip = pNotice
            ?? LLocalization.LLocalizationTextRead("Inspector.Video.PersistWhitebalance");
        pInspectorNeutralTool.IsEnabled = pWhitebalanceCapable;
        pInspectorNeutralTool.ToolTip = pNotice
            ?? LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePickTooltip");
        pInspectorWhiteTool.IsEnabled = pWhitebalanceCapable;
        pInspectorWhiteTool.ToolTip = pNotice
            ?? LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalancePickWhiteTooltip");
        ToolTipService.SetShowOnDisabled(pWhitebalanceBody, true);
        ToolTipService.SetShowOnDisabled(pWhitebalanceBox, true);
        ToolTipService.SetShowOnDisabled(pWhitebalancePersistent, true);
        ToolTipService.SetShowOnDisabled(pInspectorNeutralTool, true);
        ToolTipService.SetShowOnDisabled(pInspectorWhiteTool, true);
        if (!pWhitebalanceCapable)
        {
            PWhitebalanceToolReset();
        }
    }
}
