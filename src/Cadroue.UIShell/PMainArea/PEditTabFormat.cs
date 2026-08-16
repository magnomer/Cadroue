using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PEditTab
{
    private static string PEditRectFormat(System.Windows.Rect? pEditRect) =>
        pEditRect is { } pRect
            ? $"rect {pRect.X:0},{pRect.Y:0} {pRect.Width:0}x{pRect.Height:0}"
            : "rect none";

    private static string PEditCropFormat(LWorkCrop? pEditCrop)
    {
        if (pEditCrop is not { } pCrop)
        {
            return "none";
        }

        if (!pCrop.LWorkCropActive)
        {
            return "inactive";
        }

        string pEdges = pCrop.LWorkEdgeActive
            ? $"edges {pCrop.LWorkCropLeft}/{pCrop.LWorkCropTop}/{pCrop.LWorkCropRight}/{pCrop.LWorkCropBottom}"
            : "no edges";
        string pFlip = pCrop.LWorkFlipHorizontal || pCrop.LWorkFlipVertical
            ? $"flip {(pCrop.LWorkFlipHorizontal ? "H" : "")}{(pCrop.LWorkFlipVertical ? "V" : "")}"
            : "no flip";
        return $"{pEdges}, rotate {pCrop.LWorkCropRotation}, {pFlip}";
    }

    private static string PEditPlanFormat(LEditPlan? pEditPlan) =>
        pEditPlan is null
            ? "none"
            : $"{PEditCropFormat(pEditPlan.LEditCrop)}, {PEditVideoFormat(pEditPlan.LEditVideo)}";

    private static string PEditVideoFormat(LWorkVideo pEditVideo)
    {
        if (!pEditVideo.LWorkVideoActive)
        {
            return "video inactive";
        }

        return string.Join(", ", pEditVideo.LWorkVideoSteps
            .Where(pStep => pStep.LWorkStepActive)
            .Select(pStep => pStep.LWorkDiagnosticRead()));
    }
}
