using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    public LRotateFlip PInspectorRotateRead() => PInspectorRotateResolve(LCropboxState.LCropboxStateCrop);

    public static LRotateFlip PInspectorRotateResolve(LWorkCrop pCrop) => new(
        pCrop.LWorkCropRotation switch
        {
            90 => LRotateKind.LRotate90,
            180 => LRotateKind.LRotate180,
            270 => LRotateKind.LRotate270,
            _ => LRotateKind.LRotateNone
        },
        pCrop.LWorkFlipHorizontal,
        pCrop.LWorkFlipVertical);

    private void PInspectorOrientationSet(int pRotation, bool pFlipHorizontal, bool pFlipVertical)
    {
        LWorkCrop pCrop = LCropboxState.LCropboxStateCrop;
        if (pCrop.LWorkCropRotation == pRotation
            && pCrop.LWorkFlipHorizontal == pFlipHorizontal
            && pCrop.LWorkFlipVertical == pFlipVertical)
        {
            return;
        }

        LWorkCrop pMapped = pCrop.LWorkEdgeActive
            ? LCropbox.LCropboxOrientationResolve(pCrop, pRotation, pFlipHorizontal, pFlipVertical)
            : pCrop with
            {
                LWorkCropRotation = pRotation,
                LWorkFlipHorizontal = pFlipHorizontal,
                LWorkFlipVertical = pFlipVertical
            };
        LCropboxState.LCropboxCropSet(pMapped);
    }

    private void PInspectorRotateChange(int pIndex)
    {
        LWorkCrop pCrop = LCropboxState.LCropboxStateCrop;
        int pRotation = pIndex switch { 1 => 90, 2 => 180, 3 => 270, _ => 0 };
        PInspectorOrientationSet(pRotation, pCrop.LWorkFlipHorizontal, pCrop.LWorkFlipVertical);
    }

    private void PInspectorFlipChange(bool pHorizontal, bool pFlipped)
    {
        LWorkCrop pCrop = LCropboxState.LCropboxStateCrop;
        PInspectorOrientationSet(
            pCrop.LWorkCropRotation,
            pHorizontal ? pFlipped : pCrop.LWorkFlipHorizontal,
            pHorizontal ? pCrop.LWorkFlipVertical : pFlipped);
    }
}
