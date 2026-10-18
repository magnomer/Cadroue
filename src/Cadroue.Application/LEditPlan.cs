using Cadroue.Core;

namespace Cadroue.Application;

public sealed record LEditPlan(LWorkCrop LEditCrop, LWorkVideo LEditVideo, bool LEditCropActive)
{
    public bool LEditSkip { get; init; }
    public bool LEditRatioFixed { get; init; }
    public bool LEditRatioLenient { get; init; }
    public int LEditRatioWidth { get; init; }
    public int LEditRatioHeight { get; init; }

    public static LEditPlan LEditEmptyCreate() =>
        new(LWorkCrop.LWorkCropCreate(), LWorkVideo.LWorkVideoCreate(), false);

    public bool LEditPlanActive =>
        LEditSkip
        || LEditCropActive
        || LEditVideo.LWorkVideoActive;

    public bool LEditPlanEmpty =>
        !LEditPlanActive
        && !LEditCrop.LWorkCropActive
        && !LEditRatioFixed;

    public static LWorkVideo LEditVideoCreate(
        IReadOnlyList<LWorkVideoStep> lEditSteps,
        bool lEditMpvOnlyCapable,
        bool lEditEqCapable = true)
    {
        IEnumerable<LWorkVideoStep> lEditKept = lEditSteps;
        if (!lEditMpvOnlyCapable)
        {
            lEditKept = lEditKept.Where(lStep =>
                lStep.LWorkStepKind is not (LColorKind.LColorKindGamma
                    or LColorKind.LColorKindWhitebalance
                    or LColorKind.LColorKindExposure));
        }

        if (!lEditEqCapable)
        {
            lEditKept = lEditKept.Where(lStep =>
                lStep.LWorkStepKind is not (LColorKind.LColorKindBrightness
                    or LColorKind.LColorKindContrast
                    or LColorKind.LColorKindGamma
                    or LColorKind.LColorKindSaturation));
        }

        return new LWorkVideo(lEditKept.ToArray());
    }

    public static (LWorkCrop LEditCrop, LWorkVideo LEditVideo) LEditWorkResolve(
        LEditPlan lEditPlan, bool lEditEqCapable)
    {
        if (lEditPlan.LEditSkip)
        {
            return (LWorkCrop.LWorkCropCreate(), LWorkVideo.LWorkVideoCreate());
        }

        return (
            lEditPlan.LEditCropActive ? lEditPlan.LEditCrop : LWorkCrop.LWorkCropCreate(),
            LEditVideoCreate(lEditPlan.LEditVideo.LWorkVideoSteps, true, lEditEqCapable));
    }

    public static LEditPlan LEditPlanResolve(
        LEditPlan? lEditSaved,
        LEditPlan? lEditPersistent,
        bool lEditCropPersistent,
        bool lEditSkipPersistent)
    {
        if (lEditPersistent is not { } lPersistent)
        {
            return lEditSaved ?? LEditEmptyCreate();
        }

        bool lEditSkip = lEditSkipPersistent ? lPersistent.LEditSkip : (lEditSaved?.LEditSkip ?? false);

        LWorkCrop lCrop = lEditCropPersistent
            ? lPersistent.LEditCrop
            : lEditSaved?.LEditCrop ?? LWorkCrop.LWorkCropCreate();
        bool lCropApply = lEditCropPersistent ? lPersistent.LEditCropActive : (lEditSaved?.LEditCropActive ?? false);
        var lSteps = new List<LWorkVideoStep>();
        foreach (LColorKind lKind in Enum.GetValues<LColorKind>())
        {
            LWorkVideoStep? lPersistentStep = lPersistent.LEditVideo.LWorkVideoSteps
                .FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
            LWorkVideoStep? lSavedStep = lEditSaved?.LEditVideo.LWorkVideoSteps
                .FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
            if (lPersistentStep is not null)
            {
                lSteps.Add(lPersistentStep);
            }
            else if (lSavedStep is not null)
            {
                lSteps.Add(lSavedStep);
            }
        }

        bool lRatioFixed = lEditCropPersistent ? lPersistent.LEditRatioFixed : lEditSaved?.LEditRatioFixed ?? false;
        bool lRatioLenient = lEditCropPersistent
            ? lPersistent.LEditRatioLenient
            : lEditSaved?.LEditRatioLenient ?? false;
        int lRatioWidth = lEditCropPersistent ? lPersistent.LEditRatioWidth : lEditSaved?.LEditRatioWidth ?? 0;
        int lRatioHeight = lEditCropPersistent ? lPersistent.LEditRatioHeight : lEditSaved?.LEditRatioHeight ?? 0;

        return new LEditPlan(lCrop, new LWorkVideo(lSteps), lCropApply)
        {
            LEditSkip = lEditSkip,
            LEditRatioFixed = lRatioFixed,
            LEditRatioLenient = lRatioLenient,
            LEditRatioWidth = lRatioWidth,
            LEditRatioHeight = lRatioHeight
        };
    }
}
