using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.MigrationInterface;

public sealed record LEditPlan(LWorkCrop LEditCrop, LWorkVideo LEditVideo, bool LEditCropApply)
{
    public bool LEditSkip { get; init; }

    public static LEditPlan LEditEmptyCreate() =>
        new(LWorkCrop.LWorkCropCreate(), LWorkVideo.LWorkVideoCreate(), false);

    public bool LEditPlanActive =>
        LEditSkip || LEditCropApply || LEditCrop.LWorkCropActive || LEditVideo.LWorkVideoActive;
}

public static class LEdit
{
    public static LEditPlan LEditPlanResolve(LEditPlan? lEditSaved, LEditPlan? lEditPersistent)
    {
        if (lEditPersistent is not { } lPersistent)
        {
            return lEditSaved ?? LEditPlan.LEditEmptyCreate();
        }

        bool lEditSkip = lPersistent.LEditSkip || (lEditSaved?.LEditSkip ?? false);

        LWorkCrop lCrop = lPersistent.LEditCropApply
            ? lPersistent.LEditCrop
            : lEditSaved?.LEditCrop ?? LWorkCrop.LWorkCropCreate();
        bool lCropApply = lPersistent.LEditCropApply || (lEditSaved?.LEditCropApply ?? false);
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

        return new LEditPlan(lCrop, new LWorkVideo(lSteps), lCropApply) { LEditSkip = lEditSkip };
    }

    public static Cadroue.Core.LSidecarEditRecord LEditPersistentCreate(LEditPlan lEditPlan) => new()
    {
        LSidecarCropLeft = lEditPlan.LEditCrop.LWorkCropLeft,
        LSidecarCropTop = lEditPlan.LEditCrop.LWorkCropTop,
        LSidecarCropRight = lEditPlan.LEditCrop.LWorkCropRight,
        LSidecarCropBottom = lEditPlan.LEditCrop.LWorkCropBottom,
        LSidecarRotation = lEditPlan.LEditCrop.LWorkCropRotation,
        LSidecarFlipHorizontal = lEditPlan.LEditCrop.LWorkFlipHorizontal,
        LSidecarFlipVertical = lEditPlan.LEditCrop.LWorkFlipVertical,
        LSidecarCropActive = lEditPlan.LEditCropApply,
        LSidecarSkip = lEditPlan.LEditSkip,
        LSidecarSteps = lEditPlan.LEditVideo.LWorkVideoSteps.Select(LEditRecordCreate).ToList()
    };

    public static LEditPlan LEditPersistentRead(Cadroue.Core.LSidecarEditRecord lEditRecord) =>
        LEditPlanCreate(lEditRecord);

    public static LEditPlan? LEditPlanRead(string lEditSourcePath) =>
        LEditSidecarRead(lEditSourcePath)?.LSidecarEdit is { } lEditRecord ? LEditPlanCreate(lEditRecord) : null;

    private static Cadroue.Media.LSidecar? LEditSidecarRead(string lEditSourcePath)
    {
        try
        {
            return Cadroue.Media.LSidecarStore.LSidecarRead(
                Cadroue.Media.LSidecarStore.LSidecarPathRead(lEditSourcePath));
        }
        catch (Exception lEditException)
        {
            LTraceLog.LTraceErrorRecord($"Edit plan could not be read for '{lEditSourcePath}'", lEditException);
            return null;
        }
    }

    private static LEditPlan LEditPlanCreate(Cadroue.Core.LSidecarEditRecord lEditRecord) => new(
        new LWorkCrop(
            lEditRecord.LSidecarCropLeft,
            lEditRecord.LSidecarCropTop,
            lEditRecord.LSidecarCropRight,
            lEditRecord.LSidecarCropBottom,
            lEditRecord.LSidecarRotation,
            lEditRecord.LSidecarFlipHorizontal,
            lEditRecord.LSidecarFlipVertical),
        new LWorkVideo(lEditRecord.LSidecarSteps.Select(LEditStepCreate).ToList()),
        lEditRecord.LSidecarCropActive) { LEditSkip = lEditRecord.LSidecarSkip };

    private static LWorkVideoStep LEditStepCreate(Cadroue.Core.LSidecarVideoStep lEditRecord) =>
        string.Equals(lEditRecord.LSidecarKind, "Contrast", StringComparison.Ordinal)
            ? LWorkVideoStep.LWorkContrastCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue)
            : LWorkVideoStep.LWorkBrightnessCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue);

    public static void LEditPlanSave(string lEditSourcePath, LEditPlan lEditPlan)
    {
        Cadroue.Media.LSidecarStore.LSidecarEditSave(
            lEditSourcePath,
            new Cadroue.Core.LSidecarEditRecord
            {
                LSidecarCropLeft = lEditPlan.LEditCrop.LWorkCropLeft,
                LSidecarCropTop = lEditPlan.LEditCrop.LWorkCropTop,
                LSidecarCropRight = lEditPlan.LEditCrop.LWorkCropRight,
                LSidecarCropBottom = lEditPlan.LEditCrop.LWorkCropBottom,
                LSidecarRotation = lEditPlan.LEditCrop.LWorkCropRotation,
                LSidecarFlipHorizontal = lEditPlan.LEditCrop.LWorkFlipHorizontal,
                LSidecarFlipVertical = lEditPlan.LEditCrop.LWorkFlipVertical,
                LSidecarCropActive = lEditPlan.LEditCropApply,
                LSidecarSkip = lEditPlan.LEditSkip,
                LSidecarSteps = lEditPlan.LEditVideo.LWorkVideoSteps.Select(LEditRecordCreate).ToList()
            });
    }

    private static Cadroue.Core.LSidecarVideoStep LEditRecordCreate(LWorkVideoStep lEditStep) => new()
    {
        LSidecarKind = lEditStep.LWorkStepKind == LColorKind.LColorKindContrast ? "Contrast" : "Brightness",
        LSidecarActive = lEditStep.LWorkStepActive,
        LSidecarValue = lEditStep.LWorkStepValue
    };
}
