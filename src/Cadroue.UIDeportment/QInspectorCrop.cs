using System.Globalization;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

// The crop section's wiring: shows the crop gate's answers and hands every input to it. The record stays here for the
// owners that still read it; edits that carry a crop record go to its outlet, which the gate also reaches.
public sealed class QInspectorCrop
{
    private readonly QInspector qInspector;
    private readonly CCrop qInspectorCropGate;
    private readonly LCropOutlet qInspectorCropOutlet;

    public QInspectorCrop(QInspector lOwner)
    {
        qInspector = lOwner;
        qInspectorCropGate = lOwner.QInspectorGate.CInspectorCrop;
        qInspectorCropOutlet = new LCropOutlet(
            QInspectorCropbox,
            () => (lOwner.QInspectorSourcePresent, lOwner.QInspectorSourceWidth, lOwner.QInspectorSourceHeight));
        qInspectorCropGate.CCropAccept(qInspectorCropOutlet);
        qInspectorCropGate.CCropChanged += QInspectorCropHandle;
    }

    public LCropboxState QInspectorCropbox { get; } = new();

    public bool QInspectorActive => qInspectorCropGate.CCropActive;

    public bool QInspectorPersistent => qInspectorCropGate.CCropPersistent;

    public bool QInspectorRatioFixed => qInspectorCropGate.CCropRatioFixed;

    public bool QInspectorRatioLenient => qInspectorCropGate.CCropRatioLenient;

    public int QInspectorPresetIndex => qInspectorCropGate.CCropPresetIndex;

    public bool QInspectorCustomShown => qInspectorCropGate.CCropCustomShown;

    public bool QInspectorCropEnabled => QInspectorActive && qInspector.QInspectorCropCapable;

    public string QInspectorApplyTip => QLocalizationCatalog.QLocalizationTextRead(
        qInspector.QInspectorCropCapable ? "Inspector.Crop.ApplyTooltip" : "Inspector.Crop.RequiresCrop");

    public bool QInspectorToolActive => qInspector.QInspectorToolArmed && qInspectorCropGate.CCropRectShown;

    public string QInspectorToolKey =>
        QInspectorToolActive ? "Active" : qInspector.QInspectorToolArmed ? "Armed" : "Idle";

    public bool QInspectorNoticeShown => QInspectorNoticeResolve() is not null;

    public string QInspectorNoticeRead() => QInspectorNoticeResolve() ?? string.Empty;

    public LWorkCrop QInspectorCropRead() => qInspectorCropOutlet.LCropWorkRead();

    public LCropbox? QInspectorRectRead() => qInspectorCropOutlet.LCropRectRead();

    public string QInspectorResolutionRead() => qInspectorCropGate.CCropResolution is { } lSize
        ? $"{lSize.CCropWidth.ToString(CultureInfo.InvariantCulture)} × "
            + $"{lSize.CCropHeight.ToString(CultureInfo.InvariantCulture)}"
        : "—";

    public string QInspectorEdgeFormat(int lSide, string lText) =>
        QInspectorWholeFormat(lText, qInspectorCropGate.CCropEdgeRead(lSide));

    public void QInspectorEdgeCommit(int lSide, string lText, bool lTyped) =>
        qInspectorCropGate.CCropEdgeCommit(lSide, lText, lTyped);

    public void QInspectorEdgeReset() => qInspectorCropGate.CCropEdgeReset();

    public void QInspectorApplySet(bool lApply) => qInspectorCropGate.CCropApplySet(lApply);

    public void QInspectorPersistentSet(bool lPersistent) => qInspectorCropGate.CCropPersistentSet(lPersistent);

    public void QInspectorCropSet(LCropbox? lDrawn, int lDriveAxis, int lAnchorX, int lAnchorY) =>
        qInspectorCropOutlet.LCropDrawnSet(lDrawn, lDriveAxis, lAnchorX, lAnchorY);

    public void QInspectorCropApply(LWorkCrop lCrop, bool lApply) => qInspectorCropOutlet.LCropWorkApply(lCrop, lApply);

    // A reset crop disarms the crop tool; a persistent one is left as it is.
    public void QInspectorCropReset()
    {
        if (qInspectorCropGate.CCropReset())
        {
            qInspector.QInspectorToolSet(false);
        }
    }

    private void QInspectorCropHandle()
    {
        if (!qInspectorCropGate.CCropActive)
        {
            qInspector.QInspectorToolSet(false);
        }
    }

    private string? QInspectorNoticeResolve() => QInspectorNotice.QInspectorNoticeResolve(qInspectorCropGate);

    // Typed text that already reads as the number stays as typed; an empty field stays empty at 0.
    internal static string QInspectorWholeFormat(string lText, double lNumber)
    {
        if ((string.IsNullOrWhiteSpace(lText) && lNumber == 0)
            || (double.TryParse(lText, NumberStyles.Integer, CultureInfo.InvariantCulture, out double lShown)
                && lShown == lNumber))
        {
            return lText;
        }

        return Math.Max(0, Math.Round(lNumber)).ToString(CultureInfo.InvariantCulture);
    }
}
