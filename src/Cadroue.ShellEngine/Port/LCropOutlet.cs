using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One inspector's crop record behind the crop gate: its state read as plain values, and each edit applied to the
// record. The edge lock remembers which edges were typed; every edit but a typed edge clears it. The source size is
// read from the owner that holds it, each time it is needed.
public sealed class LCropOutlet : LCropPort
{
    private readonly LCropboxState _lCropOutletState;
    private readonly Func<(bool LCropPresent, double LCropWidth, double LCropHeight)> _lCropOutletSource;
    private readonly LCropboxEdgeLock _lCropOutletLock = new();

    public LCropOutlet(LCropboxState lState, Func<(bool LCropPresent, double LCropWidth, double LCropHeight)> lSource)
    {
        ArgumentNullException.ThrowIfNull(lState);
        ArgumentNullException.ThrowIfNull(lSource);
        _lCropOutletState = lState;
        _lCropOutletSource = lSource;
    }

    public event Action? LCropChanged
    {
        add => _lCropOutletState.LCropboxStateChanged += value;
        remove => _lCropOutletState.LCropboxStateChanged -= value;
    }

    public (int LCropRotation, bool LCropHorizontal, bool LCropVertical) LCropOrientationRead()
    {
        LWorkCrop lCrop = _lCropOutletState.LCropboxStateCrop;
        return (lCrop.LWorkCropRotation, lCrop.LWorkFlipHorizontal, lCrop.LWorkFlipVertical);
    }

    public (bool LCropActive, bool LCropPersistent, bool LCropFixed, bool LCropLenient, int LCropWidth, int LCropHeight)
        LCropStateRead()
    {
        LCropboxRatio lRatio = _lCropOutletState.LCropboxStateRatio;
        return (_lCropOutletState.LCropboxStateActive, _lCropOutletState.LCropboxStatePersistent,
            lRatio.LCropboxRatioFixed, lRatio.LCropboxRatioLenient, lRatio.LCropboxRatioWidth,
            lRatio.LCropboxRatioHeight);
    }

    public (int LCropLeft, int LCropTop, int LCropRight, int LCropBottom) LCropEdgesRead() =>
        LCropEdgesRead(_lCropOutletState.LCropboxStateCrop);

    // The edges held inside the source, as the crop is applied.
    public (int LCropLeft, int LCropTop, int LCropRight, int LCropBottom) LCropNormalRead() =>
        LCropEdgesRead(LCropWorkRead());

    public (bool LCropPresent, double LCropWidth, double LCropHeight) LCropSourceRead() => _lCropOutletSource();

    // A crop size against the held ratio: whether the lenient tolerance accepts it, and the whole pixels it runs over
    // the ratio, with true when the excess is on the width.
    public (bool LCropTolerated, int LCropExcess, bool LCropWide) LCropNoticeRead(double lWidth, double lHeight)
    {
        LCropboxRatio lRatio = _lCropOutletState.LCropboxStateRatio;
        bool lTolerated = LCropboxAspect.LCropboxToleranceCheck(
            lWidth, lHeight, lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight);
        (int lExcess, bool lWide) = LCropboxAspect.LCropboxExcessResolve(
            lWidth, lHeight, lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight);
        return (lTolerated, lExcess, lWide);
    }

    public bool LCropRectCheck() => LCropRectRead() is not null;

    public (int LCropWidth, int LCropHeight) LCropRatioNormalize(int lWidth, int lHeight)
    {
        LCropboxExtent lExtent = LCropboxAspect.LCropboxRatioNormalize(lWidth, lHeight);
        return (lExtent.LCropboxExtentWidth, lExtent.LCropboxExtentHeight);
    }

    public LWorkCrop LCropWorkRead()
    {
        (_, double lWidth, double lHeight) = _lCropOutletSource();
        return LCropboxInset.LCropboxEdgeNormalize(_lCropOutletState.LCropboxStateCrop, lWidth, lHeight);
    }

    public LCropbox? LCropRectRead()
    {
        (_, double lWidth, double lHeight) = _lCropOutletSource();
        return LCropboxInset.LCropboxRectResolve(_lCropOutletState.LCropboxStateCrop, lWidth, lHeight);
    }

    public void LCropRotateSet(int lRotation)
    {
        LWorkCrop lCrop = _lCropOutletState.LCropboxStateCrop;
        LCropOrientationApply(lRotation, lCrop.LWorkFlipHorizontal, lCrop.LWorkFlipVertical);
    }

    // The flip on the named axis takes the new state; the other axis keeps its own.
    public void LCropFlipSet(bool lHorizontal, bool lFlipped)
    {
        LWorkCrop lCrop = _lCropOutletState.LCropboxStateCrop;
        LCropOrientationApply(
            lCrop.LWorkCropRotation,
            lHorizontal ? lFlipped : lCrop.LWorkFlipHorizontal,
            lHorizontal ? lCrop.LWorkFlipVertical : lFlipped);
    }

    // A typed edge locks or frees its side; under a fixed ratio with a source the other axis follows the lock.
    public void LCropEdgeSet(int lSide, double lEdge, bool lLocked)
    {
        _lCropOutletLock.LCropboxEdgeSet(lSide, lLocked);
        LCropEdgesApply(LCropInsetsResolve(lSide, lEdge));
    }

    public void LCropEdgeReset()
    {
        _lCropOutletLock.LCropboxEdgeClear();
        LCropEdgesApply(new LCropboxEdges(0, 0, 0, 0));
    }

    public void LCropApplySet(bool lApply, bool lRatioReset)
    {
        _lCropOutletState.LCropboxApplySet(lApply);
        if (lRatioReset)
        {
            LCropRatioReset();
        }
    }

    public void LCropPersistentSet(bool lPersistent) => _lCropOutletState.LCropboxPersistentSet(lPersistent);

    public void LCropReset()
    {
        _lCropOutletLock.LCropboxEdgeClear();
        _lCropOutletState.LCropboxStateReset();
    }

    public void LCropRatioSet(bool lFixed, bool lLenient, int lWidth, int lHeight, bool lUnlock)
    {
        if (lUnlock)
        {
            _lCropOutletLock.LCropboxEdgeClear();
        }

        _lCropOutletState.LCropboxRatioSet(lFixed, lLenient, lWidth, lHeight);
    }

    public void LCropRatioReset()
    {
        _lCropOutletLock.LCropboxEdgeClear();
        _lCropOutletState.LCropboxRatioSet(false, false, 0, 0);
    }

    // The preset ratio fixes the crop, fitted inside its present extent, and keeps its leniency and switch.
    public void LCropPresetSet(int lWidth, int lHeight)
    {
        LWorkCrop lCrop = _lCropOutletState.LCropboxStateCrop;
        (_, double lSourceWidth, double lSourceHeight) = _lCropOutletSource();
        LWorkCrop lFitted = LCropboxAspect.LCropboxPresetResolve(lCrop, lSourceWidth, lSourceHeight, lWidth, lHeight);
        _lCropOutletLock.LCropboxEdgeClear();
        _lCropOutletState.LCropboxStateSet(
            lFitted,
            _lCropOutletState.LCropboxStateActive,
            true,
            _lCropOutletState.LCropboxStateRatio.LCropboxRatioLenient,
            lWidth,
            lHeight);
    }

    public void LCropDrawnSet(LCropbox? lDrawn, int lDriveAxis, int lAnchorX, int lAnchorY)
    {
        _lCropOutletLock.LCropboxEdgeClear();
        (_, double lWidth, double lHeight) = _lCropOutletSource();
        LCropEdgesApply(LCropboxAspect.LCropboxDrawnResolve(
            lDrawn, lWidth, lHeight, () => _lCropOutletState.LCropboxStateRatio, lDriveAxis, lAnchorX, lAnchorY));
    }

    public void LCropWorkApply(LWorkCrop lCrop, bool lApply)
    {
        _lCropOutletLock.LCropboxEdgeClear();
        _lCropOutletState.LCropboxCropSet(lCrop);
        _lCropOutletState.LCropboxApplySet(lApply);
    }

    // A plan's crop is applied as the plan holds it, with whether it applies.
    public void LCropWorkApply(LInspectorPort lPlan)
    {
        ArgumentNullException.ThrowIfNull(lPlan);
        (LWorkCrop lCrop, bool lApply) = lPlan.LInspectorCropRead();
        LCropWorkApply(lCrop, lApply);
    }

    private static (int, int, int, int) LCropEdgesRead(LWorkCrop lCrop) =>
        (lCrop.LWorkCropLeft, lCrop.LWorkCropTop, lCrop.LWorkCropRight, lCrop.LWorkCropBottom);

    private LCropboxEdges LCropInsetsResolve(int lSide, double lEdge)
    {
        LCropboxEdges lInsets = LCropboxInset.LCropboxSideResolve(_lCropOutletState.LCropboxStateCrop, lSide, lEdge);
        LCropboxRatio lRatio = _lCropOutletState.LCropboxStateRatio;
        (bool lPresent, double lWidth, double lHeight) = _lCropOutletSource();
        if (lRatio.LCropboxRatioFixed && lPresent)
        {
            lInsets = _lCropOutletLock.LCropboxEdgeResolve(
                lWidth,
                lHeight,
                lInsets,
                lRatio.LCropboxRatioWidth,
                lRatio.LCropboxRatioHeight,
                lSide is 0 or 2) ?? lInsets;
        }

        return lInsets;
    }

    // A fixed ratio keeps the ratio and moves the edges; a free crop drops any ratio it had.
    private void LCropEdgesApply(LCropboxEdges lInsets)
    {
        LWorkCrop lCrop = LCropboxInset.LCropboxInsetResolve(_lCropOutletState.LCropboxStateCrop, lInsets);
        if (_lCropOutletState.LCropboxStateRatio.LCropboxRatioFixed)
        {
            _lCropOutletState.LCropboxCropSet(lCrop);
            return;
        }

        _lCropOutletState.LCropboxStateSet(lCrop, _lCropOutletState.LCropboxStateActive, false, false, 0, 0);
    }

    private void LCropOrientationApply(int lRotation, bool lFlipHorizontal, bool lFlipVertical)
    {
        LWorkCrop lCrop = _lCropOutletState.LCropboxStateCrop;
        if (lCrop.LWorkCropRotation == lRotation
            && lCrop.LWorkFlipHorizontal == lFlipHorizontal
            && lCrop.LWorkFlipVertical == lFlipVertical)
        {
            return;
        }

        _lCropOutletState.LCropboxCropSet(
            LCropboxOrientation.LCropboxOrientedResolve(lCrop, lRotation, lFlipHorizontal, lFlipVertical));
    }
}
