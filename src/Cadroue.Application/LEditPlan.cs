using Cadroue.Core;

namespace Cadroue.Application;

public sealed record LEditPlan(LWorkCrop LEditCrop, LWorkVideo LEditVideo, bool LEditCropApply)
{
    public bool LEditSkip { get; init; }
    public bool LEditRatioFixed { get; init; }
    public bool LEditRatioLenient { get; init; }
    public int LEditRatioWidth { get; init; }
    public int LEditRatioHeight { get; init; }

    public static LEditPlan LEditEmptyCreate() =>
        new(LWorkCrop.LWorkCropCreate(), LWorkVideo.LWorkVideoCreate(), false);

    public bool LEditPlanActive =>
        LEditSkip || LEditCropApply || LEditCrop.LWorkCropActive || LEditVideo.LWorkVideoActive || LEditRatioFixed;
}

public static partial class LEdit
{
    public static LWorkVideo LEditVideoCreate(
        IReadOnlyList<LWorkVideoStep> lEditSteps,
        bool lEditMpvOnlyCapable) =>
        new(lEditMpvOnlyCapable
            ? lEditSteps
            : lEditSteps.Where(lStep =>
                lStep.LWorkStepKind is not (LColorKind.LColorKindGamma or LColorKind.LColorKindWhitebalance)).ToArray());

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

        bool lRatioFixed = lPersistent.LEditCropApply ? lPersistent.LEditRatioFixed : lEditSaved?.LEditRatioFixed ?? false;
        bool lRatioLenient = lPersistent.LEditCropApply ? lPersistent.LEditRatioLenient : lEditSaved?.LEditRatioLenient ?? false;
        int lRatioWidth = lPersistent.LEditCropApply ? lPersistent.LEditRatioWidth : lEditSaved?.LEditRatioWidth ?? 0;
        int lRatioHeight = lPersistent.LEditCropApply ? lPersistent.LEditRatioHeight : lEditSaved?.LEditRatioHeight ?? 0;

        return new LEditPlan(lCrop, new LWorkVideo(lSteps), lCropApply)
        {
            LEditSkip = lEditSkip,
            LEditRatioFixed = lRatioFixed,
            LEditRatioLenient = lRatioLenient,
            LEditRatioWidth = lRatioWidth,
            LEditRatioHeight = lRatioHeight
        };
    }

    public static LSidecarEditRecord LEditPersistentCreate(LEditPlan lEditPlan) => new()
    {
        LSidecarCropLeft = lEditPlan.LEditCrop.LWorkCropLeft,
        LSidecarCropTop = lEditPlan.LEditCrop.LWorkCropTop,
        LSidecarCropRight = lEditPlan.LEditCrop.LWorkCropRight,
        LSidecarCropBottom = lEditPlan.LEditCrop.LWorkCropBottom,
        LSidecarRotation = lEditPlan.LEditCrop.LWorkCropRotation,
        LSidecarFlipHorizontal = lEditPlan.LEditCrop.LWorkFlipHorizontal,
        LSidecarFlipVertical = lEditPlan.LEditCrop.LWorkFlipVertical,
        LSidecarCropActive = lEditPlan.LEditCropApply,
        LSidecarRatioFixed = lEditPlan.LEditRatioFixed,
        LSidecarRatioLenient = lEditPlan.LEditRatioLenient,
        LSidecarRatioWidth = lEditPlan.LEditRatioWidth,
        LSidecarRatioHeight = lEditPlan.LEditRatioHeight,
        LSidecarSkip = lEditPlan.LEditSkip,
        LSidecarSteps = lEditPlan.LEditVideo.LWorkVideoSteps.Select(LEditRecordCreate).ToList()
    };

    public static LEditPlan LEditPersistentRead(LSidecarEditRecord lEditRecord) =>
        LEditPlanCreate(lEditRecord);

    public static LEditPlan? LEditPlanRead(string lEditSourcePath, Func<string, LSidecarEditRecord?> lSidecarRead) =>
        lSidecarRead(lEditSourcePath) is { } lEditRecord ? LEditPlanCreate(lEditRecord) : null;

    private static LEditPlan LEditPlanCreate(LSidecarEditRecord lEditRecord) => new(
        new LWorkCrop(
            lEditRecord.LSidecarCropLeft,
            lEditRecord.LSidecarCropTop,
            lEditRecord.LSidecarCropRight,
            lEditRecord.LSidecarCropBottom,
            lEditRecord.LSidecarRotation,
            lEditRecord.LSidecarFlipHorizontal,
            lEditRecord.LSidecarFlipVertical),
        new LWorkVideo(lEditRecord.LSidecarSteps.Select(LEditStepCreate).ToList()),
        lEditRecord.LSidecarCropActive)
    {
        LEditSkip = lEditRecord.LSidecarSkip,
        LEditRatioFixed = lEditRecord.LSidecarRatioFixed,
        LEditRatioLenient = lEditRecord.LSidecarRatioLenient,
        LEditRatioWidth = lEditRecord.LSidecarRatioWidth,
        LEditRatioHeight = lEditRecord.LSidecarRatioHeight
    };

    private static LWorkVideoStep LEditStepCreate(LSidecarVideoStep lEditRecord)
    {
        LColorKind lKind = LColor.LColorKindParse(lEditRecord.LSidecarKind) ?? LColorKind.LColorKindBrightness;
        return lKind switch
        {
            LColorKind.LColorKindContrast =>
                LWorkVideoStep.LWorkContrastCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue),
            LColorKind.LColorKindGamma =>
                LWorkVideoStep.LWorkGammaCreate(
                    lEditRecord.LSidecarActive,
                    lEditRecord.LSidecarValue,
                    lEditRecord.LSidecarGammaRed ?? 0,
                    lEditRecord.LSidecarGammaGreen ?? 0,
                    lEditRecord.LSidecarGammaBlue ?? 0,
                    lEditRecord.LSidecarGammaHighlightProtection ?? 0),
            LColorKind.LColorKindWhitebalance =>
                LWorkVideoStep.LWorkWhitebalanceCreate(
                    lEditRecord.LSidecarActive,
                    lEditRecord.LSidecarWhitebalanceMethod ?? LWhitebalanceMethod.LWhitebalanceMethodMedian,
                    lEditRecord.LSidecarWhitebalanceSaturation ?? 100,
                    lEditRecord.LSidecarWhitebalanceRed ?? 1,
                    lEditRecord.LSidecarWhitebalanceGreen ?? 1,
                    lEditRecord.LSidecarWhitebalanceBlue ?? 1,
                    lEditRecord.LSidecarSampleRed ?? 0,
                    lEditRecord.LSidecarSampleGreen ?? 0,
                    lEditRecord.LSidecarSampleBlue ?? 0),
            _ => LWorkVideoStep.LWorkBrightnessCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue)
        };
    }

    public static void LEditPlanSave(
        string lEditSourcePath, LEditPlan lEditPlan, Func<string, LSidecarEditRecord?, bool> lSidecarSave) =>
        lSidecarSave(lEditSourcePath, LEditPersistentCreate(lEditPlan));

    private static LSidecarVideoStep LEditRecordCreate(LWorkVideoStep lEditStep)
    {
        var lRecord = new LSidecarVideoStep
        {
            LSidecarKind = LColor.LColorKindFormat(lEditStep.LWorkStepKind),
            LSidecarActive = lEditStep.LWorkStepActive,
            LSidecarValue = lEditStep.LWorkStepValue
        };
        if (lEditStep.LWorkStepKind == LColorKind.LColorKindGamma)
        {
            LWorkGammaSettings lGamma = lEditStep.LWorkGammaRead();
            lRecord.LSidecarGammaRed = lGamma.LWorkGammaRed;
            lRecord.LSidecarGammaGreen = lGamma.LWorkGammaGreen;
            lRecord.LSidecarGammaBlue = lGamma.LWorkGammaBlue;
            lRecord.LSidecarGammaHighlightProtection = lGamma.LWorkGammaHighlightProtection;
        }
        else if (lEditStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
        {
            LWorkWhitebalanceSettings lWhitebalance = lEditStep.LWorkWhitebalanceRead();
            lRecord.LSidecarWhitebalanceMethod = lWhitebalance.LWorkWhitebalanceMethod;
            lRecord.LSidecarWhitebalanceSaturation = lWhitebalance.LWorkWhitebalanceSaturation;
            if (lWhitebalance.LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
            {
                lRecord.LSidecarWhitebalanceRed = lWhitebalance.LWorkWhitebalanceRed;
                lRecord.LSidecarWhitebalanceGreen = lWhitebalance.LWorkWhitebalanceGreen;
                lRecord.LSidecarWhitebalanceBlue = lWhitebalance.LWorkWhitebalanceBlue;
                lRecord.LSidecarSampleRed = lWhitebalance.LWorkSampleRed;
                lRecord.LSidecarSampleGreen = lWhitebalance.LWorkSampleGreen;
                lRecord.LSidecarSampleBlue = lWhitebalance.LWorkSampleBlue;
            }
        }

        return lRecord;
    }
}
