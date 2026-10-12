using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pExposureBox = null!;
    private CheckBox pExposurePersistent = null!;
    private Slider pExposureSlider = null!;
    private TextBox pExposureValue = null!;
    private StackPanel pExposureStack = null!;
    private StackPanel pExposureBody = null!;

    private StackPanel PExposureBuild()
    {
        pExposureBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyExposure"));
        pExposurePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistExposure"));
        PInspectorSwitchAttach(pExposureBox, QExposure.QExposureActiveSet);
        PInspectorSwitchAttach(pExposurePersistent, QExposure.QExposurePersistentSet);
        pExposureSlider = PToneSliderBuild(-3, 3, 0);
        pExposureValue = PInspectorDecimalBuild();
        pExposureValue.Text = "0";
        PInspectorValueAttach(
            pExposureSlider,
            pExposureValue,
            -3,
            3,
            () => QExposure.QExposureStep.LWorkStepValue,
            QExposure.QExposureValueSet);
        pExposureStack = new StackPanel();
        pExposureStack.Children.Add(PInspectorSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Amount"),
            pExposureSlider,
            "EV",
            pExposureValue));
        pExposureBody = PToneBodyBuild(pExposureBox, pExposureStack);
        return pExposureBody;
    }

    private void PExposureUpdate()
    {
        PInspectorSwitchUpdate(pExposureBox, QExposure.QExposureStep.LWorkStepActive);
        PInspectorSwitchUpdate(pExposurePersistent, QExposure.QExposurePersistent);
        PInspectorValueUpdate(pExposureSlider, pExposureValue, QExposure.QExposureStep.LWorkStepValue, "0.#");
        PInspectorSectionApply(
            pExposureBox, pExposurePersistent, pExposureStack, pExposureBody,
            QInspector.QInspectorTipResolve(
                QExposure.QExposureStep.LWorkStepActive,
                QExposure.QExposureCapable,
                QExposure.QExposurePreview,
                "Inspector.Video.ExposureRequiresEq",
                "Inspector.Video.ExposurePreviewMpv",
                "Inspector.Video.ApplyExposure",
                "Inspector.Video.PersistExposure"));
    }
}
