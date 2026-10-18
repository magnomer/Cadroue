using Cadroue.Core;

namespace Cadroue.Application;

public static class LEditSidecar
{
    public static LSidecarEditEntry LEditPersistentCreate(LEditPlan lEditPlan) => new()
    {
        LSidecarCropLeft = lEditPlan.LEditCrop.LWorkCropLeft,
        LSidecarCropTop = lEditPlan.LEditCrop.LWorkCropTop,
        LSidecarCropRight = lEditPlan.LEditCrop.LWorkCropRight,
        LSidecarCropBottom = lEditPlan.LEditCrop.LWorkCropBottom,
        LSidecarRotation = lEditPlan.LEditCrop.LWorkCropRotation,
        LSidecarFlipHorizontal = lEditPlan.LEditCrop.LWorkFlipHorizontal,
        LSidecarFlipVertical = lEditPlan.LEditCrop.LWorkFlipVertical,
        LSidecarCropActive = lEditPlan.LEditCropActive,
        LSidecarRatioFixed = lEditPlan.LEditRatioFixed,
        LSidecarRatioLenient = lEditPlan.LEditRatioLenient,
        LSidecarRatioWidth = lEditPlan.LEditRatioWidth,
        LSidecarRatioHeight = lEditPlan.LEditRatioHeight,
        LSidecarSkip = lEditPlan.LEditSkip,
        LSidecarSteps = lEditPlan.LEditVideo.LWorkVideoSteps.Select(LEditRecordCreate).ToList()
    };

    public static LEditPlan LEditPersistentRead(LSidecarEditEntry lEditRecord) => new(
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

    public static LEditPlan? LEditSidecarRead(string lEditSourcePath, Func<string, LSidecarEditEntry?> lSidecarRead) =>
        lSidecarRead(lEditSourcePath) is { } lEditRecord ? LEditPersistentRead(lEditRecord) : null;

    public static bool LEditSidecarSave(
        string lEditSourcePath, LEditPlan lEditPlan, Func<string, LSidecarEditEntry?, bool> lSidecarSave) =>
        lSidecarSave(lEditSourcePath, LEditPersistentCreate(lEditPlan));

    private static LWorkVideoStep LEditStepCreate(LSidecarVideoStep lEditRecord)
    {
        LColorKind lKind = LColorToken.LColorTokenParse(lEditRecord.LSidecarKind)
            ?? LColorKind.LColorKindBrightness;
        return lKind switch
        {
            LColorKind.LColorKindContrast =>
                LWorkVideoStep.LWorkContrastCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue),
            LColorKind.LColorKindSaturation =>
                LWorkVideoStep.LWorkSaturationCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue),
            LColorKind.LColorKindExposure =>
                LWorkVideoStep.LWorkExposureCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue),
            LColorKind.LColorKindGamma =>
                LWorkVideoStep.LWorkGammaCreate(
                    lEditRecord.LSidecarActive,
                    lEditRecord.LSidecarValue,
                    lEditRecord.LSidecarGammaRed ?? 0,
                    lEditRecord.LSidecarGammaGreen ?? 0,
                    lEditRecord.LSidecarGammaBlue ?? 0,
                    lEditRecord.LSidecarGammaHighlight ?? 0),
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
            LColorKind.LColorKindCurve =>
                LWorkVideoStep.LWorkCurveCreate(
                    lEditRecord.LSidecarActive,
                    LEditCurveRead(lEditRecord, "Master"),
                    LEditCurveRead(lEditRecord, "Red"),
                    LEditCurveRead(lEditRecord, "Green"),
                    LEditCurveRead(lEditRecord, "Blue")),
            _ => LWorkVideoStep.LWorkBrightnessCreate(lEditRecord.LSidecarActive, lEditRecord.LSidecarValue)
        };
    }

    private static IReadOnlyList<LWorkCurvePoint>? LEditCurveRead(
        LSidecarVideoStep lEditRecord, string lChannelName)
    {
        LSidecarCurveChannel? lChannel = lEditRecord.LSidecarCurveChannels?
            .FirstOrDefault(lEntry => lEntry.LSidecarCurveName == lChannelName);
        return lChannel is null
            ? null
            : lChannel.LSidecarCurvePoints
                .Select(lPoint => new LWorkCurvePoint(lPoint.LSidecarCurveInput, lPoint.LSidecarCurveOutput))
                .ToList();
    }

    private static LSidecarVideoStep LEditRecordCreate(LWorkVideoStep lEditStep)
    {
        var lRecord = new LSidecarVideoStep
        {
            LSidecarKind = LColorToken.LColorTokenFormat(lEditStep.LWorkStepKind),
            LSidecarActive = lEditStep.LWorkStepActive,
            LSidecarValue = lEditStep.LWorkStepValue
        };
        if (lEditStep.LWorkStepKind == LColorKind.LColorKindGamma)
        {
            LWorkGammaSettings lGamma = lEditStep.LWorkGammaRead();
            lRecord.LSidecarGammaRed = lGamma.LWorkGammaRed;
            lRecord.LSidecarGammaGreen = lGamma.LWorkGammaGreen;
            lRecord.LSidecarGammaBlue = lGamma.LWorkGammaBlue;
            lRecord.LSidecarGammaHighlight = lGamma.LWorkGammaHighlight;
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
        else if (lEditStep.LWorkStepKind == LColorKind.LColorKindCurve)
        {
            LWorkCurveSettings lCurve = lEditStep.LWorkCurveRead();
            var lChannels = new List<LSidecarCurveChannel>();
            LEditCurveAdd(lChannels, "Master", lCurve.LWorkCurveMaster);
            LEditCurveAdd(lChannels, "Red", lCurve.LWorkCurveRed);
            LEditCurveAdd(lChannels, "Green", lCurve.LWorkCurveGreen);
            LEditCurveAdd(lChannels, "Blue", lCurve.LWorkCurveBlue);
            if (lChannels.Count > 0)
            {
                lRecord.LSidecarCurveChannels = lChannels;
            }
        }

        return lRecord;
    }

    private static void LEditCurveAdd(
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
