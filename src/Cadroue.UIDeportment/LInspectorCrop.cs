using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LInspectorCrop
{
    private static readonly (int, int)[] lInspectorPresets =
    {
        (0, 0), (16, 9), (9, 16), (4, 3), (3, 4), (1, 1), (21, 9)
    };

    private static readonly int[] lInspectorRotations = { 0, 90, 180, 270 };

    private readonly LInspector lInspector;
    private readonly LCropboxEdgeLock lInspectorEdgeLock = new();

    public LInspectorCrop(LInspector lOwner)
    {
        lInspector = lOwner;
        LInspectorCropbox.LCropboxStateChange += LInspectorCropHandle;
    }

    public LCropboxState LInspectorCropbox { get; } = new();

    public bool LInspectorActive => LInspectorCropbox.LCropboxStateActive;

    public bool LInspectorPersistent => LInspectorCropbox.LCropboxStatePersistent;

    public bool LInspectorRatioFixed => LInspectorCropbox.LCropboxStateRatio.LCropboxRatioFixed;

    public bool LInspectorRatioLenient => LInspectorCropbox.LCropboxStateRatio.LCropboxRatioLenient;

    public int LInspectorPresetIndex
    {
        get
        {
            LCropboxRatio lRatio = LInspectorCropbox.LCropboxStateRatio;
            return lRatio.LCropboxRatioFixed
                ? LInspectorPresetResolve(lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight)
                : 0;
        }
    }

    public bool LInspectorCustomShown => LInspectorPresetIndex == 0;

    public bool LInspectorCropEnabled => LInspectorActive && lInspector.LInspectorCropCapable;

    public int LInspectorRotateIndex =>
        Math.Max(0, Array.IndexOf(lInspectorRotations, LInspectorCropbox.LCropboxStateCrop.LWorkCropRotation));

    public bool LInspectorFlipHorizontal => LInspectorCropbox.LCropboxStateCrop.LWorkFlipHorizontal;

    public bool LInspectorFlipVertical => LInspectorCropbox.LCropboxStateCrop.LWorkFlipVertical;

    public string LInspectorApplyTip => LLocalization.LLocalizationTextRead(
        lInspector.LInspectorCropCapable ? "Inspector.Crop.ApplyTooltip" : "Inspector.Crop.RequiresCrop");

    public string? LInspectorOrientationTip => lInspector.LInspectorOrientationCapable
        ? null
        : LLocalization.LLocalizationTextRead("Inspector.Crop.RequiresTranspose");

    public bool LInspectorToolActive => lInspector.LInspectorToolArmed && LInspectorRectRead() is not null;

    public string LInspectorToolKey =>
        LInspectorToolActive ? "Active" : lInspector.LInspectorToolArmed ? "Armed" : "Idle";

    public bool LInspectorNoticeShown => LInspectorNoticeResolve() is not null;

    public string LInspectorNoticeRead() => LInspectorNoticeResolve() ?? string.Empty;

    public LWorkCrop LInspectorCropRead() => LCropbox.LCropboxEdgeNormalize(
        LInspectorCropbox.LCropboxStateCrop, lInspector.LInspectorSourceWidth, lInspector.LInspectorSourceHeight);

    public LCropbox? LInspectorRectRead() => LCropbox.LCropboxRectResolve(
        LInspectorCropbox.LCropboxStateCrop, lInspector.LInspectorSourceWidth, lInspector.LInspectorSourceHeight);

    public LRotateFlip LInspectorRotateRead() => LRotateFlip.LRotateCropResolve(LInspectorCropbox.LCropboxStateCrop);

    public string LInspectorResolutionRead()
    {
        if (!lInspector.LInspectorSourcePresent)
        {
            return "—";
        }

        LWorkCrop lCrop = LInspectorCropRead();
        double lWidth = lInspector.LInspectorSourceWidth - lCrop.LWorkCropLeft - lCrop.LWorkCropRight;
        double lHeight = lInspector.LInspectorSourceHeight - lCrop.LWorkCropTop - lCrop.LWorkCropBottom;
        return lWidth > 0 && lHeight > 0
            ? $"{Math.Round(lWidth).ToString(CultureInfo.InvariantCulture)} × "
                + $"{Math.Round(lHeight).ToString(CultureInfo.InvariantCulture)}"
            : "—";
    }

    public string LInspectorEdgeFormat(int lSide, string lText) =>
        LInspectorWholeFormat(lText, LInspectorEdgeRead(LInspectorCropbox.LCropboxStateCrop, lSide));

    public string LInspectorRatioFormat(bool lWidth, string lText)
    {
        LCropboxRatio lRatio = LInspectorCropbox.LCropboxStateRatio;
        int lShown = lWidth ? lRatio.LCropboxRatioWidth : lRatio.LCropboxRatioHeight;
        if (lInspector.LInspectorSourcePresent && !lRatio.LCropboxRatioFixed
            && lRatio.LCropboxRatioWidth == 0 && lRatio.LCropboxRatioHeight == 0
            && LInspectorRectRead() is not null)
        {
            LCropboxExtent lExtent = LCropbox.LCropboxRatioNormalize(
                (int)Math.Round(LInspectorWidthRead()),
                (int)Math.Round(LInspectorHeightRead()));
            lShown = lWidth ? lExtent.LCropboxExtentWidth : lExtent.LCropboxExtentHeight;
        }

        return LInspectorWholeFormat(lText, lShown);
    }

    public void LInspectorEdgeCommit(int lSide, string lText)
    {
        lInspectorEdgeLock.LCropboxEdgeSet(lSide, !string.IsNullOrWhiteSpace(lText));
        LWorkCrop lCrop = LInspectorCropbox.LCropboxStateCrop;
        double[] lEdges = { lCrop.LWorkCropLeft, lCrop.LWorkCropTop, lCrop.LWorkCropRight, lCrop.LWorkCropBottom };
        lEdges[lSide] = Math.Max(0, LInspectorNumberParse(lText));
        var lInsets = new LCropboxEdges(lEdges[0], lEdges[1], lEdges[2], lEdges[3]);
        LCropboxRatio lRatio = LInspectorCropbox.LCropboxStateRatio;
        if (lRatio.LCropboxRatioFixed && lInspector.LInspectorSourcePresent)
        {
            lInsets = lInspectorEdgeLock.LCropboxEdgeResolve(
                lInspector.LInspectorSourceWidth,
                lInspector.LInspectorSourceHeight,
                lInsets,
                lRatio.LCropboxRatioWidth,
                lRatio.LCropboxRatioHeight,
                lSide is 0 or 2) ?? lInsets;
        }

        LInspectorEdgesSet(lInsets);
    }

    public void LInspectorEdgeReset()
    {
        lInspectorEdgeLock.LCropboxEdgeClear();
        LInspectorEdgesSet(new LCropboxEdges(0, 0, 0, 0));
    }

    public void LInspectorRatioCommit(string lWidthText, string lHeightText)
    {
        LCropboxRatio lRatio = LInspectorCropbox.LCropboxStateRatio;
        LInspectorCropbox.LCropboxRatioSet(
            lRatio.LCropboxRatioFixed,
            lRatio.LCropboxRatioLenient,
            (int)Math.Round(LInspectorNumberParse(lWidthText)),
            (int)Math.Round(LInspectorNumberParse(lHeightText)));
    }

    public void LInspectorFixedSet(bool lFixed) => LInspectorRatioCommit(lFixed, null);

    public void LInspectorLenientSet(bool lLenient) => LInspectorRatioCommit(null, lLenient);

    public void LInspectorPresetSelect(int lIndex)
    {
        if (lIndex < 0)
        {
            return;
        }

        LCropboxRatio lRatio = LInspectorCropbox.LCropboxStateRatio;
        if (lIndex == 0)
        {
            if (lRatio.LCropboxRatioFixed
                && LInspectorPresetResolve(lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight) != 0)
            {
                LInspectorRatioReset();
            }

            return;
        }

        (int lRatioWidth, int lRatioHeight) = lInspectorPresets[lIndex];
        if (lRatioWidth == lRatio.LCropboxRatioWidth && lRatioHeight == lRatio.LCropboxRatioHeight
            && lRatio.LCropboxRatioFixed)
        {
            return;
        }

        if (!lInspector.LInspectorSourcePresent)
        {
            return;
        }

        LWorkCrop lCrop = LInspectorCropbox.LCropboxStateCrop;
        double lBoundsWidth = LInspectorWidthRead();
        double lBoundsHeight = LInspectorHeightRead();
        LCropbox? lPreset = lBoundsWidth > 0 && lBoundsHeight > 0
            ? LCropbox.LCropboxRatioResolve(
                new LCropbox(lCrop.LWorkCropLeft, lCrop.LWorkCropTop, lBoundsWidth, lBoundsHeight),
                lRatioWidth,
                lRatioHeight)
            : null;
        LWorkCrop lFitted = lPreset is { } lCropbox
            ? LInspectorEdgesResolve(lCrop, new LCropboxEdges(
                lCropbox.LCropboxX,
                lCropbox.LCropboxY,
                lInspector.LInspectorSourceWidth - lCropbox.LCropboxRight,
                lInspector.LInspectorSourceHeight - lCropbox.LCropboxBottom))
            : lCrop;
        lInspectorEdgeLock.LCropboxEdgeClear();
        LInspectorCropbox.LCropboxStateSet(
            lFitted, LInspectorActive, true, lRatio.LCropboxRatioLenient, lRatioWidth, lRatioHeight);
    }

    public void LInspectorRotateSelect(int lIndex)
    {
        if (lIndex < 0)
        {
            return;
        }

        LWorkCrop lCrop = LInspectorCropbox.LCropboxStateCrop;
        LInspectorOrientationSet(lInspectorRotations[lIndex], lCrop.LWorkFlipHorizontal, lCrop.LWorkFlipVertical);
    }

    public void LInspectorFlipSet(bool lHorizontal, bool lFlipped)
    {
        LWorkCrop lCrop = LInspectorCropbox.LCropboxStateCrop;
        LInspectorOrientationSet(
            lCrop.LWorkCropRotation,
            lHorizontal ? lFlipped : lCrop.LWorkFlipHorizontal,
            lHorizontal ? lCrop.LWorkFlipVertical : lFlipped);
    }

    public void LInspectorApplySet(bool lApply)
    {
        bool lFlipped = LInspectorActive != lApply;
        LInspectorCropbox.LCropboxApplySet(lApply);
        if (lFlipped && !lApply)
        {
            LInspectorRatioReset();
        }
    }

    public void LInspectorPersistentSet(bool lPersistent) => LInspectorCropbox.LCropboxPersistentSet(lPersistent);

    public void LInspectorCropSet(LCropbox? lDrawn, int lDriveAxis, int lAnchorX, int lAnchorY)
    {
        lInspectorEdgeLock.LCropboxEdgeClear();
        LCropbox? lSnapped = lDrawn is { LCropboxWidth: > 0, LCropboxHeight: > 0 } lDesired
            ? LCropbox.LCropboxFitResolve(
                lDesired,
                new LCropbox(0, 0, lInspector.LInspectorSourceWidth, lInspector.LInspectorSourceHeight),
                LInspectorCropbox.LCropboxStateRatio,
                lDriveAxis,
                lAnchorX,
                lAnchorY) ?? lDesired
            : lDrawn;
        if (lSnapped is not { LCropboxWidth: > 0, LCropboxHeight: > 0 } lRect)
        {
            LInspectorEdgesSet(new LCropboxEdges(0, 0, 0, 0));
            return;
        }

        LInspectorEdgesSet(new LCropboxEdges(
            lRect.LCropboxX,
            lRect.LCropboxY,
            lInspector.LInspectorSourceWidth - lRect.LCropboxX - lRect.LCropboxWidth,
            lInspector.LInspectorSourceHeight - lRect.LCropboxY - lRect.LCropboxHeight));
    }

    public void LInspectorCropApply(LWorkCrop lCrop, bool lApply)
    {
        lInspectorEdgeLock.LCropboxEdgeClear();
        LInspectorCropbox.LCropboxCropSet(lCrop);
        LInspectorCropbox.LCropboxApplySet(lApply);
    }

    public void LInspectorRatioApply(bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight)
    {
        bool lValid = lRatioWidth > 0 && lRatioHeight > 0;
        LInspectorCropbox.LCropboxRatioSet(
            lRatioFixed && lValid, lRatioLenient && lRatioFixed && lValid, lRatioWidth, lRatioHeight);
    }

    public void LInspectorRatioReset()
    {
        lInspectorEdgeLock.LCropboxEdgeClear();
        LInspectorCropbox.LCropboxRatioSet(false, false, 0, 0);
    }

    public void LInspectorCropReset()
    {
        if (LInspectorPersistent)
        {
            return;
        }

        lInspectorEdgeLock.LCropboxEdgeClear();
        lInspector.LInspectorToolSet(false);
        LInspectorCropbox.LCropboxStateReset();
    }

    private void LInspectorCropHandle()
    {
        if (!LInspectorActive)
        {
            lInspector.LInspectorToolSet(false);
        }
    }

    private string? LInspectorNoticeResolve()
    {
        if (!lInspector.LInspectorSourcePresent)
        {
            return null;
        }

        double lCropWidth = LInspectorWidthRead();
        double lCropHeight = LInspectorHeightRead();
        if (lCropWidth <= 0 || lCropHeight <= 0)
        {
            return LLocalization.LLocalizationTextRead("Inspector.Crop.FrameError");
        }

        LCropboxRatio lRatio = LInspectorCropbox.LCropboxStateRatio;
        if (!lRatio.LCropboxRatioFixed)
        {
            return null;
        }

        if (lRatio.LCropboxRatioWidth <= 0 || lRatio.LCropboxRatioHeight <= 0)
        {
            return LLocalization.LLocalizationTextRead("Inspector.Crop.RatioError");
        }

        if (lRatio.LCropboxRatioLenient
            && LCropbox.LCropboxToleranceCheck(
                lCropWidth, lCropHeight, lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight))
        {
            return null;
        }

        (int lExcess, bool lWide) = LCropbox.LCropboxExcessResolve(
            lCropWidth, lCropHeight, lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight);
        if (lExcess <= 0)
        {
            return null;
        }

        return LLocalization.LLocalizationFormat(
            lWide ? "Inspector.Crop.WidthMismatch" : "Inspector.Crop.HeightMismatch", lExcess);
    }

    private void LInspectorRatioCommit(bool? lFixed, bool? lLenient)
    {
        LCropboxRatio lRatio = LInspectorCropbox.LCropboxStateRatio;
        bool lRatioFixed = lFixed ?? lRatio.LCropboxRatioFixed;
        lInspectorEdgeLock.LCropboxEdgeClear();
        LInspectorCropbox.LCropboxRatioSet(
            lRatioFixed,
            lRatioFixed && (lLenient ?? lRatio.LCropboxRatioLenient),
            lRatio.LCropboxRatioWidth,
            lRatio.LCropboxRatioHeight);
    }

    private void LInspectorOrientationSet(int lRotation, bool lFlipHorizontal, bool lFlipVertical)
    {
        LWorkCrop lCrop = LInspectorCropbox.LCropboxStateCrop;
        if (lCrop.LWorkCropRotation == lRotation
            && lCrop.LWorkFlipHorizontal == lFlipHorizontal
            && lCrop.LWorkFlipVertical == lFlipVertical)
        {
            return;
        }

        LInspectorCropbox.LCropboxCropSet(lCrop.LWorkEdgeActive
            ? LCropbox.LCropboxOrientationResolve(lCrop, lRotation, lFlipHorizontal, lFlipVertical)
            : lCrop with
            {
                LWorkCropRotation = lRotation,
                LWorkFlipHorizontal = lFlipHorizontal,
                LWorkFlipVertical = lFlipVertical
            });
    }

    private void LInspectorEdgesSet(LCropboxEdges lInsets)
    {
        LWorkCrop lCrop = LInspectorEdgesResolve(LInspectorCropbox.LCropboxStateCrop, lInsets);
        if (LInspectorRatioFixed)
        {
            LInspectorCropbox.LCropboxCropSet(lCrop);
            return;
        }

        LInspectorCropbox.LCropboxStateSet(lCrop, LInspectorActive, false, false, 0, 0);
    }

    private double LInspectorWidthRead()
    {
        LWorkCrop lCrop = LInspectorCropbox.LCropboxStateCrop;
        return lInspector.LInspectorSourceWidth - lCrop.LWorkCropLeft - lCrop.LWorkCropRight;
    }

    private double LInspectorHeightRead()
    {
        LWorkCrop lCrop = LInspectorCropbox.LCropboxStateCrop;
        return lInspector.LInspectorSourceHeight - lCrop.LWorkCropTop - lCrop.LWorkCropBottom;
    }

    private static LWorkCrop LInspectorEdgesResolve(LWorkCrop lCrop, LCropboxEdges lInsets) => lCrop with
    {
        LWorkCropLeft = LInspectorEdgeNormalize(lInsets.LCropboxLeft),
        LWorkCropTop = LInspectorEdgeNormalize(lInsets.LCropboxTop),
        LWorkCropRight = LInspectorEdgeNormalize(lInsets.LCropboxRight),
        LWorkCropBottom = LInspectorEdgeNormalize(lInsets.LCropboxBottom)
    };

    private static int LInspectorEdgeRead(LWorkCrop lCrop, int lSide) => lSide switch
    {
        0 => lCrop.LWorkCropLeft,
        1 => lCrop.LWorkCropTop,
        2 => lCrop.LWorkCropRight,
        _ => lCrop.LWorkCropBottom
    };

    private static int LInspectorEdgeNormalize(double lEdge) => (int)Math.Max(0, Math.Round(lEdge));

    private static int LInspectorPresetResolve(int lRatioWidth, int lRatioHeight) =>
        Math.Max(0, Array.IndexOf(lInspectorPresets, (lRatioWidth, lRatioHeight)));

    private static double LInspectorNumberParse(string lText) =>
        double.TryParse(lText, NumberStyles.Integer, CultureInfo.InvariantCulture, out double lNumber) ? lNumber : 0;

    private static string LInspectorWholeFormat(string lText, double lNumber)
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
