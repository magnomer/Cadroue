using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LCropboxState
{
    private LWorkCrop lCropboxStateCrop = LWorkCrop.LWorkCropCreate();
    private bool lCropboxStateActive;
    private bool lCropboxStatePersistent;
    private bool lCropboxRatioFixed;
    private bool lCropboxRatioLenient;
    private int lCropboxRatioWidth;
    private int lCropboxRatioHeight;

    public event Action? LCropboxStateChange;

    public LWorkCrop LCropboxStateCrop => lCropboxStateCrop;

    public bool LCropboxStateActive => lCropboxStateActive;

    public bool LCropboxStatePersistent => lCropboxStatePersistent;

    public LCropboxRatio LCropboxStateRatio =>
        new(lCropboxRatioFixed, lCropboxRatioLenient, lCropboxRatioWidth, lCropboxRatioHeight);

    public void LCropboxCropSet(LWorkCrop lCrop)
    {
        if (lCropboxStateCrop == lCrop)
        {
            return;
        }

        lCropboxStateCrop = lCrop;
        LCropboxStateRaise();
    }

    public void LCropboxApplySet(bool lApply)
    {
        if (lCropboxStateActive == lApply)
        {
            return;
        }

        lCropboxStateActive = lApply;
        LCropboxStateRaise();
    }

    public void LCropboxPersistentSet(bool lPersistent)
    {
        if (lCropboxStatePersistent == lPersistent)
        {
            return;
        }

        lCropboxStatePersistent = lPersistent;
        LCropboxStateRaise();
    }

    public void LCropboxRatioSet(bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight)
    {
        var lRatio = new LCropboxRatio(lRatioFixed, lRatioLenient, lRatioWidth, lRatioHeight);
        if (LCropboxStateRatio == lRatio)
        {
            return;
        }

        lCropboxRatioFixed = lRatioFixed;
        lCropboxRatioLenient = lRatioLenient;
        lCropboxRatioWidth = lRatioWidth;
        lCropboxRatioHeight = lRatioHeight;
        LCropboxStateRaise();
    }

    public void LCropboxStateSet(
        LWorkCrop lCrop,
        bool lApply,
        bool lRatioFixed,
        bool lRatioLenient,
        int lRatioWidth,
        int lRatioHeight)
    {
        var lRatio = new LCropboxRatio(lRatioFixed, lRatioLenient, lRatioWidth, lRatioHeight);
        if (lCropboxStateCrop == lCrop && lCropboxStateActive == lApply && LCropboxStateRatio == lRatio)
        {
            return;
        }

        lCropboxStateCrop = lCrop;
        lCropboxStateActive = lApply;
        lCropboxRatioFixed = lRatioFixed;
        lCropboxRatioLenient = lRatioLenient;
        lCropboxRatioWidth = lRatioWidth;
        lCropboxRatioHeight = lRatioHeight;
        LCropboxStateRaise();
    }

    public void LCropboxStateReset() =>
        LCropboxStateSet(LWorkCrop.LWorkCropCreate(), false, false, false, 0, 0);

    private void LCropboxStateRaise() => LCropboxStateChange?.Invoke();
}

public sealed class LCropboxEdgeLock
{
    private readonly bool[] lCropboxEdgeLocked = new bool[4];

    public void LCropboxEdgeSet(int lEdge, bool lLocked) => lCropboxEdgeLocked[lEdge] = lLocked;

    public void LCropboxEdgeClear() => Array.Clear(lCropboxEdgeLocked);

    public LCropboxEdges? LCropboxEdgeResolve(
        double lSourceWidth,
        double lSourceHeight,
        LCropboxEdges lInsets,
        double lRatioWidth,
        double lRatioHeight,
        bool lHorizontal) =>
        LCropbox.LCropboxLockResolve(
            lSourceWidth,
            lSourceHeight,
            lInsets.LCropboxLeft,
            lInsets.LCropboxTop,
            lInsets.LCropboxRight,
            lInsets.LCropboxBottom,
            lCropboxEdgeLocked[0],
            lCropboxEdgeLocked[1],
            lCropboxEdgeLocked[2],
            lCropboxEdgeLocked[3],
            lRatioWidth,
            lRatioHeight,
            lHorizontal);
}
