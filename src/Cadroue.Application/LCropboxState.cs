using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LCropboxState
{
    private LWorkCrop lCropboxStateCrop = LWorkCrop.LWorkCropCreate();
    private bool lCropboxStateApply;
    private bool lCropboxStatePersistent;
    private bool lCropboxRatioFixed;
    private bool lCropboxRatioLenient;
    private int lCropboxRatioWidth;
    private int lCropboxRatioHeight;

    public event Action? LCropboxStateChange;

    public LWorkCrop LCropboxStateCrop =>
        lCropboxStateApply ? lCropboxStateCrop : LWorkCrop.LWorkCropCreate();

    public bool LCropboxStateApply => lCropboxStateApply;

    public bool LCropboxStatePersistent => lCropboxStatePersistent;

    public (bool RatioFixed, bool RatioLenient, int RatioWidth, int RatioHeight) LCropboxStateRatio =>
        (lCropboxRatioFixed, lCropboxRatioLenient, lCropboxRatioWidth, lCropboxRatioHeight);

    public void LCropboxCropSet(LWorkCrop lCrop)
    {
        lCropboxStateCrop = lCrop;
        LCropboxStateRaise();
    }

    public void LCropboxApplySet(bool lApply)
    {
        lCropboxStateApply = lApply;
        LCropboxStateRaise();
    }

    public void LCropboxPersistentSet(bool lPersistent)
    {
        lCropboxStatePersistent = lPersistent;
        LCropboxStateRaise();
    }

    public void LCropboxRatioSet(bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight)
    {
        lCropboxRatioFixed = lRatioFixed;
        lCropboxRatioLenient = lRatioLenient;
        lCropboxRatioWidth = lRatioWidth;
        lCropboxRatioHeight = lRatioHeight;
        LCropboxStateRaise();
    }

    public void LCropboxStateSet(LWorkCrop lCrop, bool lApply, bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight)
    {
        lCropboxStateCrop = lCrop;
        lCropboxStateApply = lApply;
        lCropboxRatioFixed = lRatioFixed;
        lCropboxRatioLenient = lRatioLenient;
        lCropboxRatioWidth = lRatioWidth;
        lCropboxRatioHeight = lRatioHeight;
        LCropboxStateRaise();
    }

    public void LCropboxStateReset()
    {
        lCropboxStateCrop = LWorkCrop.LWorkCropCreate();
        lCropboxStateApply = false;
        lCropboxRatioFixed = false;
        lCropboxRatioLenient = false;
        lCropboxRatioWidth = 0;
        lCropboxRatioHeight = 0;
        LCropboxStateRaise();
    }

    private void LCropboxStateRaise() => LCropboxStateChange?.Invoke();
}
