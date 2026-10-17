using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The orientation tools' wiring: the rotate choice and both flips show the crop gate's turn and hand their raw input
// to it.
public sealed class QInspectorOrientation
{
    private readonly QInspector qInspector;
    private readonly CCrop qInspectorOrientationGate;

    public QInspectorOrientation(QInspector lOwner)
    {
        qInspector = lOwner;
        qInspectorOrientationGate = lOwner.QInspectorGate.CInspectorCrop;
    }

    public int QInspectorRotateIndex => qInspectorOrientationGate.CCropRotateIndex;

    public bool QInspectorFlipHorizontal => qInspectorOrientationGate.CCropFlipHorizontal;

    public bool QInspectorFlipVertical => qInspectorOrientationGate.CCropFlipVertical;

    public string? QInspectorOrientationTip => qInspector.QInspectorOrientationCapable
        ? null
        : QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.RequiresTranspose");

    public LRotateFlip QInspectorRotateRead() => LRotateFlip.LRotateCropResolve(
        qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop);

    public void QInspectorRotateSelect(int lIndex) => qInspectorOrientationGate.CCropRotateSelect(lIndex);

    public void QInspectorFlipSet(bool lHorizontal, bool lFlipped) =>
        qInspectorOrientationGate.CCropFlipSet(lHorizontal, lFlipped);
}
