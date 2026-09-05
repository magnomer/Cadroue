using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    public bool PTonePersistentCheck() =>
        pInspectorBrightnessPersistent.IsChecked == true
        || pInspectorContrastPersistent.IsChecked == true
        || pInspectorSaturationPersistent.IsChecked == true
        || pGammaPersistent.IsChecked == true
        || pWhitebalancePersistent.IsChecked == true
        || pExposurePersistent.IsChecked == true
        || pCurvePersistent.IsChecked == true;

    public void PTonePersistentApply(LWorkVideo pVideo)
    {
        foreach (LWorkVideoStep pStep in pVideo.LWorkVideoSteps)
        {
            if (pStep.LWorkStepKind == LColorKind.LColorKindContrast)
            {
                pInspectorContrastPersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindSaturation)
            {
                pInspectorSaturationPersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            {
                pGammaPersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
            {
                pWhitebalancePersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindExposure)
            {
                pExposurePersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindCurve)
            {
                pCurvePersistent.IsChecked = true;
            }
            else
            {
                pInspectorBrightnessPersistent.IsChecked = true;
            }
        }
    }

    public LWorkVideo PTonePersistentRead()
    {
        var pSteps = new List<LWorkVideoStep>();
        if (pInspectorBrightnessPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindBrightness));
        }

        if (pInspectorContrastPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindContrast));
        }

        if (pInspectorSaturationPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindSaturation));
        }

        if (pGammaPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindGamma));
        }

        if (pWhitebalancePersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindWhitebalance));
        }

        if (pExposurePersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindExposure));
        }

        if (pCurvePersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindCurve));
        }

        return new LWorkVideo(pSteps);
    }
}
