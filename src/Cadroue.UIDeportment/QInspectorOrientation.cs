using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QInspectorOrientation
{
    private static readonly int[] qInspectorRotations = { 0, 90, 180, 270 };

    private readonly QInspector qInspector;

    public QInspectorOrientation(QInspector lOwner)
    {
        qInspector = lOwner;
    }

    public int QInspectorRotateIndex => Math.Max(
        0,
        Array.IndexOf(
            qInspectorRotations, qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop.LWorkCropRotation));

    public bool QInspectorFlipHorizontal =>
        qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop.LWorkFlipHorizontal;

    public bool QInspectorFlipVertical =>
        qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop.LWorkFlipVertical;

    public string? QInspectorOrientationTip => qInspector.QInspectorOrientationCapable
        ? null
        : LLocalization.LLocalizationTextRead("Inspector.Crop.RequiresTranspose");

    public LRotateFlip QInspectorRotateRead() => LRotateFlip.LRotateCropResolve(
        qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop);

    public void QInspectorRotateSelect(int lIndex)
    {
        if (lIndex < 0)
        {
            return;
        }

        LWorkCrop lCrop = qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop;
        QInspectorOrientationSet(qInspectorRotations[lIndex], lCrop.LWorkFlipHorizontal, lCrop.LWorkFlipVertical);
    }

    public void QInspectorFlipSet(bool lHorizontal, bool lFlipped)
    {
        LWorkCrop lCrop = qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop;
        QInspectorOrientationSet(
            lCrop.LWorkCropRotation,
            lHorizontal ? lFlipped : lCrop.LWorkFlipHorizontal,
            lHorizontal ? lCrop.LWorkFlipVertical : lFlipped);
    }

    private void QInspectorOrientationSet(int lRotation, bool lFlipHorizontal, bool lFlipVertical)
    {
        LWorkCrop lCrop = qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop;
        if (lCrop.LWorkCropRotation == lRotation
            && lCrop.LWorkFlipHorizontal == lFlipHorizontal
            && lCrop.LWorkFlipVertical == lFlipVertical)
        {
            return;
        }

        qInspector.QInspectorCrop.QInspectorCropbox.LCropboxCropSet(lCrop.LWorkEdgeActive
            ? LCropbox.LCropboxOrientationResolve(lCrop, lRotation, lFlipHorizontal, lFlipVertical)
            : lCrop with
            {
                LWorkCropRotation = lRotation,
                LWorkFlipHorizontal = lFlipHorizontal,
                LWorkFlipVertical = lFlipVertical
            });
    }
}
