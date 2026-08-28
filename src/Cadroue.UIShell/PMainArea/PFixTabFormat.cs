using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PFixTab
{
    private static string PFixRectFormat(System.Windows.Rect? pFixRect) =>
        pFixRect is { } pRect
            ? $"rect {pRect.X:0},{pRect.Y:0} {pRect.Width:0}x{pRect.Height:0}"
            : "rect none";

    private static string PFixCropFormat(LWorkCrop? pFixCrop)
    {
        if (pFixCrop is not { } pCrop)
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

    private static string PFixPlanFormat(LFixPlan? pFixPlan) =>
        pFixPlan is null
            ? "none"
            : $"{PFixCropFormat(pFixPlan.LFixCrop)}, {PFixVideoFormat(pFixPlan.LFixVideo)}";

    private static string PFixVideoFormat(LWorkVideo pFixVideo)
    {
        if (!pFixVideo.LWorkVideoActive)
        {
            return "video inactive";
        }

        return string.Join(", ", pFixVideo.LWorkVideoSteps
            .Where(pStep => pStep.LWorkStepActive)
            .Select(pStep => pStep.LWorkDiagnosticRead()));
    }
}
