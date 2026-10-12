using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QInspectorCrop
{
    private static readonly (int, int)[] qInspectorPresets =
    {
        (0, 0), (16, 9), (9, 16), (4, 3), (3, 4), (1, 1), (21, 9)
    };

    private readonly QInspector qInspector;
    private readonly LCropboxEdgeLock qInspectorEdgeLock = new();

    public QInspectorCrop(QInspector lOwner)
    {
        qInspector = lOwner;
        QInspectorCropbox.LCropboxStateChanged += QInspectorCropHandle;
    }

    public LCropboxState QInspectorCropbox { get; } = new();

    public bool QInspectorActive => QInspectorCropbox.LCropboxStateActive;

    public bool QInspectorPersistent => QInspectorCropbox.LCropboxStatePersistent;

    public bool QInspectorRatioFixed => QInspectorCropbox.LCropboxStateRatio.LCropboxRatioFixed;

    public bool QInspectorRatioLenient => QInspectorCropbox.LCropboxStateRatio.LCropboxRatioLenient;

    public int QInspectorPresetIndex
    {
        get
        {
            LCropboxRatio lRatio = QInspectorCropbox.LCropboxStateRatio;
            return lRatio.LCropboxRatioFixed
                ? QInspectorPresetResolve(lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight)
                : 0;
        }
    }

    public bool QInspectorCustomShown => QInspectorPresetIndex == 0;

    public bool QInspectorCropEnabled => QInspectorActive && qInspector.QInspectorCropCapable;

    public string QInspectorApplyTip => LLocalization.LLocalizationTextRead(
        qInspector.QInspectorCropCapable ? "Inspector.Crop.ApplyTooltip" : "Inspector.Crop.RequiresCrop");

    public bool QInspectorToolActive => qInspector.QInspectorToolArmed && QInspectorRectRead() is not null;

    public string QInspectorToolKey =>
        QInspectorToolActive ? "Active" : qInspector.QInspectorToolArmed ? "Armed" : "Idle";

    public bool QInspectorNoticeShown => QInspectorNoticeResolve() is not null;

    public string QInspectorNoticeRead() => QInspectorNoticeResolve() ?? string.Empty;

    public LWorkCrop QInspectorCropRead() => LCropbox.LCropboxEdgeNormalize(
        QInspectorCropbox.LCropboxStateCrop, qInspector.QInspectorSourceWidth, qInspector.QInspectorSourceHeight);

    public LCropbox? QInspectorRectRead() => LCropbox.LCropboxRectResolve(
        QInspectorCropbox.LCropboxStateCrop, qInspector.QInspectorSourceWidth, qInspector.QInspectorSourceHeight);

    public string QInspectorResolutionRead()
    {
        if (!qInspector.QInspectorSourcePresent)
        {
            return "—";
        }

        LWorkCrop lCrop = QInspectorCropRead();
        double lWidth = qInspector.QInspectorSourceWidth - lCrop.LWorkCropLeft - lCrop.LWorkCropRight;
        double lHeight = qInspector.QInspectorSourceHeight - lCrop.LWorkCropTop - lCrop.LWorkCropBottom;
        return lWidth > 0 && lHeight > 0
            ? $"{Math.Round(lWidth).ToString(CultureInfo.InvariantCulture)} × "
                + $"{Math.Round(lHeight).ToString(CultureInfo.InvariantCulture)}"
            : "—";
    }

    public string QInspectorEdgeFormat(int lSide, string lText) =>
        QInspectorWholeFormat(lText, QInspectorEdgeRead(QInspectorCropbox.LCropboxStateCrop, lSide));

    public string QInspectorRatioFormat(bool lWidth, string lText)
    {
        LCropboxRatio lRatio = QInspectorCropbox.LCropboxStateRatio;
        int lShown = lWidth ? lRatio.LCropboxRatioWidth : lRatio.LCropboxRatioHeight;
        if (qInspector.QInspectorSourcePresent && !lRatio.LCropboxRatioFixed
            && lRatio.LCropboxRatioWidth == 0 && lRatio.LCropboxRatioHeight == 0
            && QInspectorRectRead() is not null)
        {
            LCropboxExtent lExtent = LCropbox.LCropboxRatioNormalize(
                (int)Math.Round(QInspectorWidthRead()),
                (int)Math.Round(QInspectorHeightRead()));
            lShown = lWidth ? lExtent.LCropboxExtentWidth : lExtent.LCropboxExtentHeight;
        }

        return QInspectorWholeFormat(lText, lShown);
    }

    public void QInspectorEdgeCommit(int lSide, string lText, bool lTyped)
    {
        if (!lTyped)
        {
            return;
        }

        qInspectorEdgeLock.LCropboxEdgeSet(lSide, !string.IsNullOrWhiteSpace(lText));
        LWorkCrop lCrop = QInspectorCropbox.LCropboxStateCrop;
        double[] lEdges = { lCrop.LWorkCropLeft, lCrop.LWorkCropTop, lCrop.LWorkCropRight, lCrop.LWorkCropBottom };
        lEdges[lSide] = Math.Max(0, QInspectorNumberParse(lText));
        var lInsets = new LCropboxEdges(lEdges[0], lEdges[1], lEdges[2], lEdges[3]);
        LCropboxRatio lRatio = QInspectorCropbox.LCropboxStateRatio;
        if (lRatio.LCropboxRatioFixed && qInspector.QInspectorSourcePresent)
        {
            lInsets = qInspectorEdgeLock.LCropboxEdgeResolve(
                qInspector.QInspectorSourceWidth,
                qInspector.QInspectorSourceHeight,
                lInsets,
                lRatio.LCropboxRatioWidth,
                lRatio.LCropboxRatioHeight,
                lSide is 0 or 2) ?? lInsets;
        }

        QInspectorEdgesSet(lInsets);
    }

    public void QInspectorEdgeReset()
    {
        qInspectorEdgeLock.LCropboxEdgeClear();
        QInspectorEdgesSet(new LCropboxEdges(0, 0, 0, 0));
    }

    public void QInspectorRatioCommit(string lWidthText, string lHeightText, bool lTyped)
    {
        if (!lTyped)
        {
            return;
        }

        LCropboxRatio lRatio = QInspectorCropbox.LCropboxStateRatio;
        QInspectorCropbox.LCropboxRatioSet(
            lRatio.LCropboxRatioFixed,
            lRatio.LCropboxRatioLenient,
            (int)Math.Round(QInspectorNumberParse(lWidthText)),
            (int)Math.Round(QInspectorNumberParse(lHeightText)));
    }

    public void QInspectorFixedSet(bool lFixed) => QInspectorRatioCommit(lFixed, null);

    public void QInspectorLenientSet(bool lLenient) => QInspectorRatioCommit(null, lLenient);

    public void QInspectorPresetSelect(int lIndex)
    {
        if (lIndex < 0)
        {
            return;
        }

        LCropboxRatio lRatio = QInspectorCropbox.LCropboxStateRatio;
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

        LWorkCrop lCrop = QInspectorCropbox.LCropboxStateCrop;
        double lBoundsWidth = QInspectorWidthRead();
        double lBoundsHeight = QInspectorHeightRead();
        LCropbox? lPreset = lBoundsWidth > 0 && lBoundsHeight > 0
            ? LCropbox.LCropboxRatioResolve(
                new LCropbox(lCrop.LWorkCropLeft, lCrop.LWorkCropTop, lBoundsWidth, lBoundsHeight),
                lRatioWidth,
                lRatioHeight)
            : null;
        LWorkCrop lFitted = lPreset is { } lCropbox
            ? QInspectorEdgesResolve(lCrop, new LCropboxEdges(
                lCropbox.LCropboxX,
                lCropbox.LCropboxY,
                qInspector.QInspectorSourceWidth - lCropbox.LCropboxRight,
                qInspector.QInspectorSourceHeight - lCropbox.LCropboxBottom))
            : lCrop;
        qInspectorEdgeLock.LCropboxEdgeClear();
        QInspectorCropbox.LCropboxStateSet(
            lFitted, QInspectorActive, true, lRatio.LCropboxRatioLenient, lRatioWidth, lRatioHeight);
    }

    public void QInspectorApplySet(bool lApply)
    {
        bool lFlipped = QInspectorActive != lApply;
        QInspectorCropbox.LCropboxApplySet(lApply);
        if (lFlipped && !lApply)
        {
            QInspectorRatioReset();
        }
    }

    public void QInspectorPersistentSet(bool lPersistent) => QInspectorCropbox.LCropboxPersistentSet(lPersistent);

    public void QInspectorCropSet(LCropbox? lDrawn, int lDriveAxis, int lAnchorX, int lAnchorY)
    {
        qInspectorEdgeLock.LCropboxEdgeClear();
        LCropbox? lSnapped = lDrawn is { LCropboxWidth: > 0, LCropboxHeight: > 0 } lDesired
            ? LCropbox.LCropboxFitResolve(
                lDesired,
                new LCropbox(0, 0, qInspector.QInspectorSourceWidth, qInspector.QInspectorSourceHeight),
                QInspectorCropbox.LCropboxStateRatio,
                lDriveAxis,
                lAnchorX,
                lAnchorY) ?? lDesired
            : lDrawn;
        if (lSnapped is not { LCropboxWidth: > 0, LCropboxHeight: > 0 } lRect)
        {
            QInspectorEdgesSet(new LCropboxEdges(0, 0, 0, 0));
            return;
        }

        QInspectorEdgesSet(new LCropboxEdges(
            lRect.LCropboxX,
            lRect.LCropboxY,
            qInspector.QInspectorSourceWidth - lRect.LCropboxX - lRect.LCropboxWidth,
            qInspector.QInspectorSourceHeight - lRect.LCropboxY - lRect.LCropboxHeight));
    }

    public void QInspectorCropApply(LWorkCrop lCrop, bool lApply)
    {
        qInspectorEdgeLock.LCropboxEdgeClear();
        QInspectorCropbox.LCropboxCropSet(lCrop);
        QInspectorCropbox.LCropboxApplySet(lApply);
    }

    public void QInspectorRatioApply(bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight)
    {
        bool lValid = lRatioWidth > 0 && lRatioHeight > 0;
        QInspectorCropbox.LCropboxRatioSet(
            lRatioFixed && lValid, lRatioLenient && lRatioFixed && lValid, lRatioWidth, lRatioHeight);
    }

    public void QInspectorRatioReset()
    {
        qInspectorEdgeLock.LCropboxEdgeClear();
        QInspectorCropbox.LCropboxRatioSet(false, false, 0, 0);
    }

    public void QInspectorCropReset()
    {
        if (QInspectorPersistent)
        {
            return;
        }

        qInspectorEdgeLock.LCropboxEdgeClear();
        qInspector.QInspectorToolSet(false);
        QInspectorCropbox.LCropboxStateReset();
    }

    private void QInspectorCropHandle()
    {
        if (!QInspectorActive)
        {
            qInspector.QInspectorToolSet(false);
        }
    }

    private string? QInspectorNoticeResolve() => QInspectorNotice.QInspectorNoticeResolve(
        qInspector.QInspectorSourcePresent,
        QInspectorWidthRead(),
        QInspectorHeightRead(),
        QInspectorCropbox.LCropboxStateRatio);

    private void QInspectorRatioCommit(bool? lFixed, bool? lLenient)
    {
        LCropboxRatio lRatio = QInspectorCropbox.LCropboxStateRatio;
        bool lRatioFixed = lFixed ?? lRatio.LCropboxRatioFixed;
        qInspectorEdgeLock.LCropboxEdgeClear();
        QInspectorCropbox.LCropboxRatioSet(
            lRatioFixed,
            lRatioFixed && (lLenient ?? lRatio.LCropboxRatioLenient),
            lRatio.LCropboxRatioWidth,
            lRatio.LCropboxRatioHeight);
    }

    private void QInspectorEdgesSet(LCropboxEdges lInsets)
    {
        LWorkCrop lCrop = QInspectorEdgesResolve(QInspectorCropbox.LCropboxStateCrop, lInsets);
        if (QInspectorRatioFixed)
        {
            QInspectorCropbox.LCropboxCropSet(lCrop);
            return;
        }

        QInspectorCropbox.LCropboxStateSet(lCrop, QInspectorActive, false, false, 0, 0);
    }

    private double QInspectorWidthRead()
    {
        LWorkCrop lCrop = QInspectorCropbox.LCropboxStateCrop;
        return qInspector.QInspectorSourceWidth - lCrop.LWorkCropLeft - lCrop.LWorkCropRight;
    }

    private double QInspectorHeightRead()
    {
        LWorkCrop lCrop = QInspectorCropbox.LCropboxStateCrop;
        return qInspector.QInspectorSourceHeight - lCrop.LWorkCropTop - lCrop.LWorkCropBottom;
    }

    private static LWorkCrop QInspectorEdgesResolve(LWorkCrop lCrop, LCropboxEdges lInsets) => lCrop with
    {
        LWorkCropLeft = QInspectorEdgeNormalize(lInsets.LCropboxLeft),
        LWorkCropTop = QInspectorEdgeNormalize(lInsets.LCropboxTop),
        LWorkCropRight = QInspectorEdgeNormalize(lInsets.LCropboxRight),
        LWorkCropBottom = QInspectorEdgeNormalize(lInsets.LCropboxBottom)
    };

    private static int QInspectorEdgeRead(LWorkCrop lCrop, int lSide) => lSide switch
    {
        0 => lCrop.LWorkCropLeft,
        1 => lCrop.LWorkCropTop,
        2 => lCrop.LWorkCropRight,
        _ => lCrop.LWorkCropBottom
    };

    private static int QInspectorEdgeNormalize(double lEdge) => (int)Math.Max(0, Math.Round(lEdge));

    private static int QInspectorPresetResolve(int lRatioWidth, int lRatioHeight) =>
        Math.Max(0, Array.IndexOf(qInspectorPresets, (lRatioWidth, lRatioHeight)));

    private static double QInspectorNumberParse(string lText) =>
        double.TryParse(lText, NumberStyles.Integer, CultureInfo.InvariantCulture, out double lNumber) ? lNumber : 0;

    private static string QInspectorWholeFormat(string lText, double lNumber)
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
