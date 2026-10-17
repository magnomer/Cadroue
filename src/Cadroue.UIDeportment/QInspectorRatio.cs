using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The ratio fields' wiring: shows the crop gate's ratio and hands every ratio input to it.
public sealed class QInspectorRatio
{
    private readonly CCrop qInspectorRatioGate;

    public QInspectorRatio(QInspector lOwner)
    {
        qInspectorRatioGate = lOwner.QInspectorGate.CInspectorCrop;
    }

    public string QInspectorRatioFormat(bool lWidth, string lText) =>
        QInspectorCrop.QInspectorWholeFormat(lText, qInspectorRatioGate.CCropRatioRead(lWidth));

    public void QInspectorRatioCommit(string lWidthText, string lHeightText, bool lTyped) =>
        qInspectorRatioGate.CCropRatioCommit(lWidthText, lHeightText, lTyped);

    public void QInspectorFixedSet(bool lFixed) => qInspectorRatioGate.CCropFixedSet(lFixed);

    public void QInspectorLenientSet(bool lLenient) => qInspectorRatioGate.CCropLenientSet(lLenient);

    public void QInspectorPresetSelect(int lIndex) => qInspectorRatioGate.CCropPresetSelect(lIndex);

    public void QInspectorRatioApply(bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight) =>
        qInspectorRatioGate.CCropRatioApply(lRatioFixed, lRatioLenient, lRatioWidth, lRatioHeight);
}
