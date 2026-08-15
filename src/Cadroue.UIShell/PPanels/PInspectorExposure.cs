using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pExposureBox = null!;
    private CheckBox pExposurePersistent = null!;
    private Slider pExposureSlider = null!;
    private TextBox pExposureValue = null!;
    private StackPanel pExposureStack = null!;
    private StackPanel pExposureBody = null!;
    private bool pExposureCapable;

    private StackPanel PExposureBuild()
    {
        pExposureBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyExposure"));
        pExposurePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistExposure"));
        pExposureSlider = PToneSliderBuild(-3, 3, 0);
        pExposureValue = PInspectorDecimalBuild();
        pExposureValue.Text = "0";
        pExposureStack = new StackPanel();
        PInspectorVideoAttach(
            pExposureBox,
            pExposureStack,
            pExposureSlider,
            pExposureValue,
            -3,
            3,
            "0.#");
        pExposureStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Amount"),
            pExposureSlider,
            "EV",
            pExposureValue));
        pExposureBody = PToneBodyBuild(pExposureBox, pExposureStack);
        PToneApplyUpdate(pExposureBox, pExposureStack);
        return pExposureBody;
    }

    public void PExposureCapabilitySet(bool pExposureCapable)
    {
        this.pExposureCapable = pExposureCapable;
        pExposureBox.IsEnabled = pExposureCapable;
        pExposurePersistent.IsEnabled = pExposureCapable;
        pExposureStack.IsEnabled = pExposureCapable && pExposureBox.IsChecked == true;
        pExposureStack.Opacity = pExposureCapable && pExposureBox.IsChecked == true ? 1 : 0.4;
        string? pDisabledTooltip = pExposureCapable
            ? null
            : LLocalization.LLocalizationTextRead("Inspector.Video.ExposureRequiresMpv");
        pExposureBody.ToolTip = pDisabledTooltip;
        pExposureBox.ToolTip = pDisabledTooltip ?? LLocalization.LLocalizationTextRead("Inspector.Video.ApplyExposure");
        pExposurePersistent.ToolTip = pDisabledTooltip ?? LLocalization.LLocalizationTextRead("Inspector.Video.PersistExposure");
        ToolTipService.SetShowOnDisabled(pExposureBody, true);
        ToolTipService.SetShowOnDisabled(pExposureBox, true);
        ToolTipService.SetShowOnDisabled(pExposurePersistent, true);
    }
}
