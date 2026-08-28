using Cadroue.Core;

namespace Cadroue.Application;

public sealed record LFixPlan(LWorkCrop LFixCrop, LWorkVideo LFixVideo, bool LFixCropActive)
{
    public bool LFixSkip { get; init; }
    public bool LFixRatioFixed { get; init; }
    public bool LFixRatioLenient { get; init; }
    public int LFixRatioWidth { get; init; }
    public int LFixRatioHeight { get; init; }

    public static LFixPlan LFixEmptyCreate() =>
        new(LWorkCrop.LWorkCropCreate(), LWorkVideo.LWorkVideoCreate(), false);

    public bool LFixPlanActive =>
        LFixSkip || LFixCropActive || LFixCrop.LWorkCropActive || LFixVideo.LWorkVideoActive || LFixRatioFixed;
}

public static partial class LFix
{
    public static LWorkVideo LFixVideoCreate(
        IReadOnlyList<LWorkVideoStep> lFixSteps,
        bool lFixMpvOnlyCapable,
        bool lFixEqCapable = true)
    {
        IEnumerable<LWorkVideoStep> lFixKept = lFixSteps;
        if (!lFixMpvOnlyCapable)
        {
            lFixKept = lFixKept.Where(lStep =>
                lStep.LWorkStepKind is not (LColorKind.LColorKindGamma
                    or LColorKind.LColorKindWhitebalance
                    or LColorKind.LColorKindExposure));
        }

        if (!lFixEqCapable)
        {
            lFixKept = lFixKept.Where(lStep =>
                lStep.LWorkStepKind is not (LColorKind.LColorKindBrightness
                    or LColorKind.LColorKindContrast
                    or LColorKind.LColorKindGamma
                    or LColorKind.LColorKindSaturation));
        }

        return new LWorkVideo(lFixKept.ToArray());
    }

    public static LFixPlan LFixPlanResolve(
        LFixPlan? lFixSaved,
        LFixPlan? lFixPersistent,
        bool lFixCropPersistent,
        bool lFixSkipPersistent)
    {
        if (lFixPersistent is not { } lPersistent)
        {
            return lFixSaved ?? LFixPlan.LFixEmptyCreate();
        }

        bool lFixSkip = lFixSkipPersistent ? lPersistent.LFixSkip : (lFixSaved?.LFixSkip ?? false);

        LWorkCrop lCrop = lFixCropPersistent
            ? lPersistent.LFixCrop
            : lFixSaved?.LFixCrop ?? LWorkCrop.LWorkCropCreate();
        bool lCropApply = lFixCropPersistent ? lPersistent.LFixCropActive : (lFixSaved?.LFixCropActive ?? false);
        var lSteps = new List<LWorkVideoStep>();
        foreach (LColorKind lKind in Enum.GetValues<LColorKind>())
        {
            LWorkVideoStep? lPersistentStep = lPersistent.LFixVideo.LWorkVideoSteps
                .FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
            LWorkVideoStep? lSavedStep = lFixSaved?.LFixVideo.LWorkVideoSteps
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

        bool lRatioFixed = lFixCropPersistent ? lPersistent.LFixRatioFixed : lFixSaved?.LFixRatioFixed ?? false;
        bool lRatioLenient = lFixCropPersistent ? lPersistent.LFixRatioLenient : lFixSaved?.LFixRatioLenient ?? false;
        int lRatioWidth = lFixCropPersistent ? lPersistent.LFixRatioWidth : lFixSaved?.LFixRatioWidth ?? 0;
        int lRatioHeight = lFixCropPersistent ? lPersistent.LFixRatioHeight : lFixSaved?.LFixRatioHeight ?? 0;

        return new LFixPlan(lCrop, new LWorkVideo(lSteps), lCropApply)
        {
            LFixSkip = lFixSkip,
            LFixRatioFixed = lRatioFixed,
            LFixRatioLenient = lRatioLenient,
            LFixRatioWidth = lRatioWidth,
            LFixRatioHeight = lRatioHeight
        };
    }

    public static LSidecarFixRecord LFixPersistentCreate(LFixPlan lFixPlan) => new()
    {
        LSidecarCropLeft = lFixPlan.LFixCrop.LWorkCropLeft,
        LSidecarCropTop = lFixPlan.LFixCrop.LWorkCropTop,
        LSidecarCropRight = lFixPlan.LFixCrop.LWorkCropRight,
        LSidecarCropBottom = lFixPlan.LFixCrop.LWorkCropBottom,
        LSidecarRotation = lFixPlan.LFixCrop.LWorkCropRotation,
        LSidecarFlipHorizontal = lFixPlan.LFixCrop.LWorkFlipHorizontal,
        LSidecarFlipVertical = lFixPlan.LFixCrop.LWorkFlipVertical,
        LSidecarCropActive = lFixPlan.LFixCropActive,
        LSidecarRatioFixed = lFixPlan.LFixRatioFixed,
        LSidecarRatioLenient = lFixPlan.LFixRatioLenient,
        LSidecarRatioWidth = lFixPlan.LFixRatioWidth,
        LSidecarRatioHeight = lFixPlan.LFixRatioHeight,
        LSidecarSkip = lFixPlan.LFixSkip,
        LSidecarSteps = lFixPlan.LFixVideo.LWorkVideoSteps.Select(LFixRecordCreate).ToList()
    };

    public static LFixPlan LFixPersistentRead(LSidecarFixRecord lFixRecord) =>
        LFixPlanCreate(lFixRecord);

    public static LFixPlan? LFixPlanRead(string lFixSourcePath, Func<string, LSidecarFixRecord?> lSidecarRead) =>
        lSidecarRead(lFixSourcePath) is { } lFixRecord ? LFixPlanCreate(lFixRecord) : null;

    private static LFixPlan LFixPlanCreate(LSidecarFixRecord lFixRecord) => new(
        new LWorkCrop(
            lFixRecord.LSidecarCropLeft,
            lFixRecord.LSidecarCropTop,
            lFixRecord.LSidecarCropRight,
            lFixRecord.LSidecarCropBottom,
            lFixRecord.LSidecarRotation,
            lFixRecord.LSidecarFlipHorizontal,
            lFixRecord.LSidecarFlipVertical),
        new LWorkVideo(lFixRecord.LSidecarSteps.Select(LFixStepCreate).ToList()),
        lFixRecord.LSidecarCropActive)
    {
        LFixSkip = lFixRecord.LSidecarSkip,
        LFixRatioFixed = lFixRecord.LSidecarRatioFixed,
        LFixRatioLenient = lFixRecord.LSidecarRatioLenient,
        LFixRatioWidth = lFixRecord.LSidecarRatioWidth,
        LFixRatioHeight = lFixRecord.LSidecarRatioHeight
    };

    private static LWorkVideoStep LFixStepCreate(LSidecarVideoStep lFixRecord)
    {
        LColorKind lKind = LColor.LColorKindParse(lFixRecord.LSidecarKind) ?? LColorKind.LColorKindBrightness;
        return lKind switch
        {
            LColorKind.LColorKindContrast =>
                LWorkVideoStep.LWorkContrastCreate(lFixRecord.LSidecarActive, lFixRecord.LSidecarValue),
            LColorKind.LColorKindSaturation =>
                LWorkVideoStep.LWorkSaturationCreate(lFixRecord.LSidecarActive, lFixRecord.LSidecarValue),
            LColorKind.LColorKindExposure =>
                LWorkVideoStep.LWorkExposureCreate(lFixRecord.LSidecarActive, lFixRecord.LSidecarValue),
            LColorKind.LColorKindGamma =>
                LWorkVideoStep.LWorkGammaCreate(
                    lFixRecord.LSidecarActive,
                    lFixRecord.LSidecarValue,
                    lFixRecord.LSidecarGammaRed ?? 0,
                    lFixRecord.LSidecarGammaGreen ?? 0,
                    lFixRecord.LSidecarGammaBlue ?? 0,
                    lFixRecord.LSidecarGammaHighlight ?? 0),
            LColorKind.LColorKindWhitebalance =>
                LWorkVideoStep.LWorkWhitebalanceCreate(
                    lFixRecord.LSidecarActive,
                    lFixRecord.LSidecarWhitebalanceMethod ?? LWhitebalanceMethod.LWhitebalanceMethodMedian,
                    lFixRecord.LSidecarWhitebalanceSaturation ?? 100,
                    lFixRecord.LSidecarWhitebalanceRed ?? 1,
                    lFixRecord.LSidecarWhitebalanceGreen ?? 1,
                    lFixRecord.LSidecarWhitebalanceBlue ?? 1,
                    lFixRecord.LSidecarSampleRed ?? 0,
                    lFixRecord.LSidecarSampleGreen ?? 0,
                    lFixRecord.LSidecarSampleBlue ?? 0),
            LColorKind.LColorKindCurve =>
                LWorkVideoStep.LWorkCurveCreate(
                    lFixRecord.LSidecarActive,
                    LFixCurveRead(lFixRecord, "Master"),
                    LFixCurveRead(lFixRecord, "Red"),
                    LFixCurveRead(lFixRecord, "Green"),
                    LFixCurveRead(lFixRecord, "Blue")),
            _ => LWorkVideoStep.LWorkBrightnessCreate(lFixRecord.LSidecarActive, lFixRecord.LSidecarValue)
        };
    }

    private static IReadOnlyList<LWorkCurvePoint>? LFixCurveRead(
        LSidecarVideoStep lFixRecord, string lChannelName)
    {
        LSidecarCurveChannel? lChannel = lFixRecord.LSidecarCurveChannels?
            .FirstOrDefault(lEntry => lEntry.LSidecarCurveName == lChannelName);
        return lChannel is null
            ? null
            : lChannel.LSidecarCurvePoints
                .Select(lPoint => new LWorkCurvePoint(lPoint.LSidecarCurveInput, lPoint.LSidecarCurveOutput))
                .ToList();
    }

    public static void LFixPlanSave(
        string lFixSourcePath, LFixPlan lFixPlan, Func<string, LSidecarFixRecord?, bool> lSidecarSave) =>
        lSidecarSave(lFixSourcePath, LFixPersistentCreate(lFixPlan));

    private static LSidecarVideoStep LFixRecordCreate(LWorkVideoStep lFixStep)
    {
        var lRecord = new LSidecarVideoStep
        {
            LSidecarKind = LColor.LColorKindFormat(lFixStep.LWorkStepKind),
            LSidecarActive = lFixStep.LWorkStepActive,
            LSidecarValue = lFixStep.LWorkStepValue
        };
        if (lFixStep.LWorkStepKind == LColorKind.LColorKindGamma)
        {
            LWorkGammaSettings lGamma = lFixStep.LWorkGammaRead();
            lRecord.LSidecarGammaRed = lGamma.LWorkGammaRed;
            lRecord.LSidecarGammaGreen = lGamma.LWorkGammaGreen;
            lRecord.LSidecarGammaBlue = lGamma.LWorkGammaBlue;
            lRecord.LSidecarGammaHighlight = lGamma.LWorkGammaHighlight;
        }
        else if (lFixStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
        {
            LWorkWhitebalanceSettings lWhitebalance = lFixStep.LWorkWhitebalanceRead();
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
        else if (lFixStep.LWorkStepKind == LColorKind.LColorKindCurve)
        {
            LWorkCurveSettings lCurve = lFixStep.LWorkCurveRead();
            var lChannels = new List<LSidecarCurveChannel>();
            LFixCurveAdd(lChannels, "Master", lCurve.LWorkCurveMaster);
            LFixCurveAdd(lChannels, "Red", lCurve.LWorkCurveRed);
            LFixCurveAdd(lChannels, "Green", lCurve.LWorkCurveGreen);
            LFixCurveAdd(lChannels, "Blue", lCurve.LWorkCurveBlue);
            if (lChannels.Count > 0)
            {
                lRecord.LSidecarCurveChannels = lChannels;
            }
        }

        return lRecord;
    }

    private static void LFixCurveAdd(
        List<LSidecarCurveChannel> lChannels, string lChannelName, IReadOnlyList<LWorkCurvePoint> lPoints)
    {
        if (LWorkCurveSettings.LWorkIdentityCheck(lPoints))
        {
            return;
        }

        lChannels.Add(new LSidecarCurveChannel
        {
            LSidecarCurveName = lChannelName,
            LSidecarCurvePoints = lPoints
                .Select(lPoint => new LSidecarCurvePoint
                {
                    LSidecarCurveInput = lPoint.LWorkCurveInput,
                    LSidecarCurveOutput = lPoint.LWorkCurveOutput
                })
                .ToList()
        });
    }
}
