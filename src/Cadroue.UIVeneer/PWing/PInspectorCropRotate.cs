using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    public LRotateFlip PInspectorRotateRead() => LInspector.LInspectorRotateRead();

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
