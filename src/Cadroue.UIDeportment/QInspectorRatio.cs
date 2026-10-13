using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QInspectorRatio
{
    private static readonly (int, int)[] qInspectorPresets =
    {
        (0, 0), (16, 9), (9, 16), (4, 3), (3, 4), (1, 1), (21, 9)
    };

    private readonly QInspector qInspector;
    private readonly QInspectorCrop qInspectorCrop;

    public QInspectorRatio(QInspector lOwner, QInspectorCrop lCrop)
    {
        qInspector = lOwner;
        qInspectorCrop = lCrop;
    }

    public string QInspectorRatioFormat(bool lWidth, string lText)
    {
        LCropboxRatio lRatio = qInspectorCrop.QInspectorCropbox.LCropboxStateRatio;
        int lShown = lWidth ? lRatio.LCropboxRatioWidth : lRatio.LCropboxRatioHeight;
        if (qInspector.QInspectorSourcePresent && !lRatio.LCropboxRatioFixed
            && lRatio.LCropboxRatioWidth == 0 && lRatio.LCropboxRatioHeight == 0
            && qInspectorCrop.QInspectorRectRead() is not null)
        {
            LCropboxExtent lExtent = LCropbox.LCropboxRatioNormalize(
                (int)Math.Round(qInspectorCrop.QInspectorWidthRead()),
                (int)Math.Round(qInspectorCrop.QInspectorHeightRead()));
            lShown = lWidth ? lExtent.LCropboxExtentWidth : lExtent.LCropboxExtentHeight;
        }

        return QInspectorCrop.QInspectorWholeFormat(lText, lShown);
    }

    public void QInspectorRatioCommit(string lWidthText, string lHeightText, bool lTyped)
    {
        if (!lTyped)
        {
            return;
        }

        LCropboxRatio lRatio = qInspectorCrop.QInspectorCropbox.LCropboxStateRatio;
        qInspectorCrop.QInspectorCropbox.LCropboxRatioSet(
            lRatio.LCropboxRatioFixed,
            lRatio.LCropboxRatioLenient,
            (int)Math.Round(QInspectorCrop.QInspectorNumberParse(lWidthText)),
            (int)Math.Round(QInspectorCrop.QInspectorNumberParse(lHeightText)));
    }

    public void QInspectorFixedSet(bool lFixed) => QInspectorRatioCommit(lFixed, null);

    public void QInspectorLenientSet(bool lLenient) => QInspectorRatioCommit(null, lLenient);

    public void QInspectorPresetSelect(int lIndex)
    {
        if (lIndex < 0)
        {
            return;
        }

        LCropboxRatio lRatio = qInspectorCrop.QInspectorCropbox.LCropboxStateRatio;
        if (lIndex == 0)
        {
            if (lRatio.LCropboxRatioFixed
                && QInspectorPresetResolve(lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight) != 0)
            {
                QInspectorRatioReset();
            }

            return;
        }

        (int lRatioWidth, int lRatioHeight) = qInspectorPresets[lIndex];
        if (lRatioWidth == lRatio.LCropboxRatioWidth && lRatioHeight == lRatio.LCropboxRatioHeight
            && lRatio.LCropboxRatioFixed)
        {
            return;
        }

        if (!qInspector.QInspectorSourcePresent)
        {
            return;
        }

        LWorkCrop lCrop = qInspectorCrop.QInspectorCropbox.LCropboxStateCrop;
        LCropbox? lPreset = qInspectorCrop.QInspectorBoundsResolve(lRatioWidth, lRatioHeight);
        LWorkCrop lFitted = lPreset is { } lCropbox
            ? QInspectorCrop.QInspectorEdgesResolve(lCrop, new LCropboxEdges(
                lCropbox.LCropboxX,
                lCropbox.LCropboxY,
                qInspector.QInspectorSourceWidth - lCropbox.LCropboxRight,
                qInspector.QInspectorSourceHeight - lCropbox.LCropboxBottom))
            : lCrop;
        qInspectorCrop.QInspectorEdgeClear();
        qInspectorCrop.QInspectorCropbox.LCropboxStateSet(
            lFitted, qInspectorCrop.QInspectorActive, true, lRatio.LCropboxRatioLenient, lRatioWidth, lRatioHeight);
    }

    public void QInspectorRatioApply(bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight)
    {
        bool lValid = lRatioWidth > 0 && lRatioHeight > 0;
        qInspectorCrop.QInspectorCropbox.LCropboxRatioSet(
            lRatioFixed && lValid, lRatioLenient && lRatioFixed && lValid, lRatioWidth, lRatioHeight);
    }

    public void QInspectorRatioReset()
    {
        qInspectorCrop.QInspectorEdgeClear();
        qInspectorCrop.QInspectorCropbox.LCropboxRatioSet(false, false, 0, 0);
    }

    internal static int QInspectorPresetResolve(int lRatioWidth, int lRatioHeight) =>
        Math.Max(0, Array.IndexOf(qInspectorPresets, (lRatioWidth, lRatioHeight)));

    private void QInspectorRatioCommit(bool? lFixed, bool? lLenient)
    {
        LCropboxRatio lRatio = qInspectorCrop.QInspectorCropbox.LCropboxStateRatio;
        bool lRatioFixed = lFixed ?? lRatio.LCropboxRatioFixed;
        qInspectorCrop.QInspectorEdgeClear();
        qInspectorCrop.QInspectorCropbox.LCropboxRatioSet(
            lRatioFixed,
            lRatioFixed && (lLenient ?? lRatio.LCropboxRatioLenient),
            lRatio.LCropboxRatioWidth,
            lRatio.LCropboxRatioHeight);
    }
}
