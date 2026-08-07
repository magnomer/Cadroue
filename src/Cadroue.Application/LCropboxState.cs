using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LCropboxState
{
    private LWorkCrop lCropboxStateCrop = LWorkCrop.LWorkCropCreate();
    private bool lCropboxStateApply;
    private bool lCropboxStatePersistent;
    private bool lCropboxStateRatioFixed;
    private int lCropboxStateRatioWidth;
    private int lCropboxStateRatioHeight;

    public event Action? LCropboxStateChange;

    public LWorkCrop LCropboxStateCrop =>
        lCropboxStateApply ? lCropboxStateCrop : LWorkCrop.LWorkCropCreate();

    public bool LCropboxStateApply => lCropboxStateApply;

    public bool LCropboxStatePersistent => lCropboxStatePersistent;

    public (bool RatioFixed, int RatioWidth, int RatioHeight) LCropboxStateRatio =>
        (lCropboxStateRatioFixed, lCropboxStateRatioWidth, lCropboxStateRatioHeight);

    public void LCropboxStateCropSet(LWorkCrop lCrop)
    {
        lCropboxStateCrop = lCrop;
        LCropboxStateRaise();
    }

    public void LCropboxStateApplySet(bool lApply)
    {
        lCropboxStateApply = lApply;
        LCropboxStateRaise();
    }

    public void LCropboxStatePersistentSet(bool lPersistent)
    {
        lCropboxStatePersistent = lPersistent;
        LCropboxStateRaise();
    }

    public void LCropboxStateRatioSet(bool lRatioFixed, int lRatioWidth, int lRatioHeight)
    {
        lCropboxStateRatioFixed = lRatioFixed;
        lCropboxStateRatioWidth = lRatioWidth;
        lCropboxStateRatioHeight = lRatioHeight;
        LCropboxStateRaise();
    }

    public void LCropboxStateSet(LWorkCrop lCrop, bool lApply, bool lRatioFixed, int lRatioWidth, int lRatioHeight)
    {
        lCropboxStateCrop = lCrop;
        lCropboxStateApply = lApply;
        lCropboxStateRatioFixed = lRatioFixed;
        lCropboxStateRatioWidth = lRatioWidth;
        lCropboxStateRatioHeight = lRatioHeight;
        LCropboxStateRaise();
    }

    public void LCropboxStateReset()
    {
        lCropboxStateCrop = LWorkCrop.LWorkCropCreate();
        lCropboxStateApply = false;
        lCropboxStateRatioFixed = false;
        lCropboxStateRatioWidth = 0;
        lCropboxStateRatioHeight = 0;
        LCropboxStateRaise();
    }

    private void LCropboxStateRaise() => LCropboxStateChange?.Invoke();
}
