using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The edit tab's trace wording: each plan piece written as the text its trace lines carry.
public static class QEditTabTrace
{
    public static string QEditRectFormat(LCropbox? lRect) =>
        lRect is { } lBox
            ? $"rect {lBox.LCropboxX:0},{lBox.LCropboxY:0} {lBox.LCropboxWidth:0}x{lBox.LCropboxHeight:0}"
            : "rect none";

    public static string QEditCropFormat(LWorkCrop? lCrop)
    {
        if (lCrop is not { } lValue)
        {
            return "none";
        }

        if (!lValue.LWorkCropActive)
        {
            return "inactive";
        }

        string lEdges = lValue.LWorkEdgeActive
            ? $"edges {lValue.LWorkCropLeft}/{lValue.LWorkCropTop}/{lValue.LWorkCropRight}/{lValue.LWorkCropBottom}"
            : "no edges";
        string lFlip = lValue.LWorkFlipHorizontal || lValue.LWorkFlipVertical
            ? $"flip {(lValue.LWorkFlipHorizontal ? "H" : "")}{(lValue.LWorkFlipVertical ? "V" : "")}"
            : "no flip";
        return $"{lEdges}, rotate {lValue.LWorkCropRotation}, {lFlip}";
    }

    public static string QEditPlanFormat(LEditPlan? lPlan) =>
        lPlan is null ? "none" : $"{QEditCropFormat(lPlan.LEditCrop)}, {QEditVideoFormat(lPlan.LEditVideo)}";

    public static string QEditVideoFormat(LWorkVideo lVideo) =>
        lVideo.LWorkVideoActive
            ? string.Join(
                ", ",
                lVideo.LWorkVideoSteps
                    .Where(lStep => lStep.LWorkStepActive)
                    .Select(LWorkVideoDiagnostic.LWorkDiagnosticRead))
            : "video inactive";
}
