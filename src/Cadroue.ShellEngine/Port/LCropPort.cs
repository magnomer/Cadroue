namespace Cadroue.ShellEngine;

public interface LCropPort
{
    event Action? LCropChanged;

    (int LCropRotation, bool LCropHorizontal, bool LCropVertical) LCropOrientationRead();

    (bool LCropActive, bool LCropPersistent, bool LCropFixed, bool LCropLenient, int LCropWidth, int LCropHeight)
        LCropStateRead();

    (int LCropLeft, int LCropTop, int LCropRight, int LCropBottom) LCropEdgesRead();

    (int LCropLeft, int LCropTop, int LCropRight, int LCropBottom) LCropNormalRead();

    (bool LCropPresent, double LCropWidth, double LCropHeight) LCropSourceRead();

    bool LCropRectCheck();

    (bool LCropTolerated, int LCropExcess, bool LCropWide) LCropNoticeRead(double lWidth, double lHeight);

    (int LCropWidth, int LCropHeight) LCropRatioNormalize(int lWidth, int lHeight);

    void LCropRotateSet(int lRotation);

    void LCropFlipSet(bool lHorizontal, bool lFlipped);

    void LCropEdgeSet(int lSide, double lEdge, bool lLocked);

    void LCropEdgeReset();

    void LCropApplySet(bool lApply, bool lRatioReset);

    void LCropWorkApply(LInspectorPort lPlan);

    void LCropPersistentSet(bool lPersistent);

    void LCropReset();

    void LCropRatioSet(bool lFixed, bool lLenient, int lWidth, int lHeight, bool lUnlock);

    void LCropRatioReset();

    void LCropPresetSet(int lWidth, int lHeight);
}
