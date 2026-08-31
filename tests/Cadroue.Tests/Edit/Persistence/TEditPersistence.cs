using Cadroue.Application;
using Cadroue.Core;

using System.Text.Json;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEditPersistence
{
    [Fact]
    public void KindParse_KnownTokens_ResolveKinds()
    {
        Assert.Equal(LColorKind.LColorKindContrast, TInterface.TColorKindParse("Contrast"));
        Assert.Equal(LColorKind.LColorKindBrightness, TInterface.TColorKindParse("Brightness"));
        Assert.Equal(LColorKind.LColorKindGamma, TInterface.TColorKindParse("Gamma"));
        Assert.Equal(LColorKind.LColorKindWhitebalance, TInterface.TColorKindParse("Whitebalance"));
    }

    [Fact]
    public void KindParse_UnknownToken_ResolvesNull()
    {
        Assert.Null(TInterface.TColorKindParse("Rubbish"));
    }

    [Fact]
    public void KindFormat_RoundTripsKnownKinds()
    {
        Assert.Equal("Contrast", TInterface.TColorKindFormat(LColorKind.LColorKindContrast));
        Assert.Equal("Brightness", TInterface.TColorKindFormat(LColorKind.LColorKindBrightness));
        Assert.Equal("Gamma", TInterface.TColorKindFormat(LColorKind.LColorKindGamma));
        Assert.Equal("Whitebalance", TInterface.TColorKindFormat(LColorKind.LColorKindWhitebalance));
        Assert.Equal(LColorKind.LColorKindContrast, TInterface.TColorKindParse(TInterface.TColorKindFormat(LColorKind.LColorKindContrast)));
        Assert.Equal(LColorKind.LColorKindBrightness, TInterface.TColorKindParse(TInterface.TColorKindFormat(LColorKind.LColorKindBrightness)));
        Assert.Equal(LColorKind.LColorKindGamma, TInterface.TColorKindParse(TInterface.TColorKindFormat(LColorKind.LColorKindGamma)));
        Assert.Equal(LColorKind.LColorKindWhitebalance, TInterface.TColorKindParse(TInterface.TColorKindFormat(LColorKind.LColorKindWhitebalance)));
    }

    [Fact]
    public void Saturation_PersistentRecord_RoundTripsActiveValueAndToken()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[] { TInterface.TWorkSaturationCreate(true, 130) });
        LEditPlan source = TInterface.TEditPlanCreate(TInterface.TWorkCropCreate(), video, false);

        LSidecarEditRecord record = TInterface.TEditPersistentCreate(source);
        LEditPlan restored = TInterface.TEditPersistentRead(record);

        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        Assert.Equal("Saturation", stored.LSidecarKind);
        LWorkVideoStep step = Assert.Single(restored.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindSaturation, step.LWorkStepKind);
        Assert.True(step.LWorkStepActive);
        Assert.Equal(130, step.LWorkStepValue);
    }

    [Fact]
    public void Exposure_PersistentRecord_RoundTripsActiveValueAndToken()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[] { TInterface.TWorkExposureCreate(true, 1.2) });
        LEditPlan source = TInterface.TEditPlanCreate(TInterface.TWorkCropCreate(), video, false);

        LSidecarEditRecord record = TInterface.TEditPersistentCreate(source);
        LEditPlan restored = TInterface.TEditPersistentRead(record);

        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        Assert.Equal("Exposure", stored.LSidecarKind);
        LWorkVideoStep step = Assert.Single(restored.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindExposure, step.LWorkStepKind);
        Assert.True(step.LWorkStepActive);
        Assert.Equal(1.2, step.LWorkStepValue);
    }

    [Fact]
    public void Exposure_EditVideoCreate_DroppedWhenNotMpvCapableKeptWhenMpv()
    {
        var steps = new[] { TInterface.TWorkExposureCreate(true, 1.2) };

        Assert.Empty(TInterface.TEditVideoCreate(steps, false).LWorkVideoSteps);
        Assert.Single(TInterface.TEditVideoCreate(steps, true).LWorkVideoSteps);
    }

    [Fact]
    public void Gamma_PersistentRecord_RoundTripsValueAndToken()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[] { TInterface.TWorkGammaCreate(true, 75) });
        LEditPlan source = TInterface.TEditPlanCreate(TInterface.TWorkCropCreate(), video, false);

        LSidecarEditRecord record = TInterface.TEditPersistentCreate(source);
        LEditPlan restored = TInterface.TEditPersistentRead(record);

        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        Assert.Equal("Gamma", stored.LSidecarKind);
        LWorkVideoStep step = Assert.Single(restored.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindGamma, step.LWorkStepKind);
        Assert.Equal(75, step.LWorkStepValue);
    }

    [Fact]
    public void Gamma_PersistentRecord_RoundTripsCompletePayload()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkGammaCreate(true, 20.25, -90.5, 10.75, 30.125, 25.5)
        });

        LSidecarEditRecord record = TInterface.TEditPersistentCreate(
            TInterface.TEditPlanCreate(TInterface.TWorkCropCreate(), video, false));
        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        LWorkVideoStep restored = Assert.Single(
            TInterface.TEditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(-90.5, stored.LSidecarGammaRed);
        Assert.Equal(10.75, stored.LSidecarGammaGreen);
        Assert.Equal(30.125, stored.LSidecarGammaBlue);
        Assert.Equal(25.5, stored.LSidecarGammaHighlight);
        Assert.NotNull(restored.LWorkStepGamma);
        Assert.Equal(20.25, restored.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(-90.5, restored.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(10.75, restored.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(30.125, restored.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(25.5, restored.LWorkStepGamma.LWorkGammaHighlight);
    }

    [Fact]
    public void Gamma_LegacyRecord_UsesValueAsGlobalAndNeutralAdvancedDefaults()
    {
        LSidecarEditRecord record = TInterface.TSidecarEditCreate("Gamma", true, 35);

        LWorkVideoStep restored = Assert.Single(
            TInterface.TEditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(35, restored.LWorkStepValue);
        Assert.NotNull(restored.LWorkStepGamma);
        Assert.Equal(35, restored.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaHighlight);
    }

    [Fact]
    public void NonGammaRecord_IgnoresGammaFields()
    {
        LSidecarEditRecord record = TInterface.TSidecarEditCreate(
            "Contrast", true, 125, 50, null, null, 100);

        LWorkVideoStep restored = Assert.Single(
            TInterface.TEditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(LColorKind.LColorKindContrast, restored.LWorkStepKind);
        Assert.Null(restored.LWorkStepGamma);
    }

    [Theory]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodAverage)]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMinmax)]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMedian)]
    public void Whitebalance_PersistentRecord_RoundTripsCompletePayload(LWhitebalanceMethod method)
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkWhitebalanceCreate(true, method, 137.625)
        });

        LSidecarEditRecord record = TInterface.TEditPersistentCreate(
            TInterface.TEditPlanCreate(TInterface.TWorkCropCreate(), video, false));
        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        LWorkVideoStep restored = Assert.Single(
            TInterface.TEditPersistentRead(record).LEditVideo.LWorkVideoSteps);
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(restored);

        Assert.Equal("Whitebalance", stored.LSidecarKind);
        Assert.Equal(method, stored.LSidecarWhitebalanceMethod);
        Assert.Equal(137.625, stored.LSidecarWhitebalanceSaturation);
        Assert.Equal(method, settings.LWorkWhitebalanceMethod);
        Assert.Equal(137.625, settings.LWorkWhitebalanceSaturation);
        Assert.Equal(137.625, restored.LWorkStepValue);
    }

    [Fact]
    public void Whitebalance_LegacyRecord_UsesMedianAndOneHundredPercent()
    {
        LSidecarEditRecord record = TInterface.TSidecarEditCreate("Whitebalance", true, 0);

        LWorkVideoStep restored = Assert.Single(
            TInterface.TEditPersistentRead(record).LEditVideo.LWorkVideoSteps);
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(restored);

        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodMedian, settings.LWorkWhitebalanceMethod);
        Assert.Equal(100, settings.LWorkWhitebalanceSaturation);
        Assert.Equal(100, restored.LWorkStepValue);
    }

    [Fact]
    public void WhitebalanceManual_PersistentRecord_RoundTripsCoefficientsAndSamples()
    {
        LSidecarEditRecord record = TInterface.TEditPersistentCreate(TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(new[]
            {
                TInterface.TWorkManualCreate(true, 137.5, 1.5, 0.5, 1.1, 200, 150, 100)
            }),
            false));
        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);

        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodManual, stored.LSidecarWhitebalanceMethod);
        Assert.Equal(1.5, stored.LSidecarWhitebalanceRed);
        Assert.Equal(0.5, stored.LSidecarWhitebalanceGreen);
        Assert.Equal(1.1, stored.LSidecarWhitebalanceBlue);
        Assert.Equal(200, stored.LSidecarSampleRed);
        Assert.Equal(150, stored.LSidecarSampleGreen);
        Assert.Equal(100, stored.LSidecarSampleBlue);

        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(Assert.Single(
            TInterface.TEditPersistentRead(record).LEditVideo.LWorkVideoSteps));
        Assert.Equal(1.5, settings.LWorkWhitebalanceRed);
        Assert.Equal(0.5, settings.LWorkWhitebalanceGreen);
        Assert.Equal(1.1, settings.LWorkWhitebalanceBlue);
        Assert.Equal(200, settings.LWorkSampleRed);
        Assert.Equal(150, settings.LWorkSampleGreen);
        Assert.Equal(100, settings.LWorkSampleBlue);
    }

    [Fact]
    public void WhitebalanceManual_SidecarJson_RoundTripsInvariantNumbers()
    {
        LSidecarEditRecord record = TInterface.TEditPersistentCreate(TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(new[]
            {
                TInterface.TWorkManualCreate(true, 137.5, 1.25, 0.5, 1.125, 200, 150, 100)
            }),
            false));
        Assert.Contains("1.125", JsonSerializer.Serialize(record));

        LSidecarVideoStep stored = Assert.Single(
            TInterface.TSidecarEditMatch(record).LSidecarSteps);
        Assert.Equal(1.25, stored.LSidecarWhitebalanceRed);
        Assert.Equal(1.125, stored.LSidecarWhitebalanceBlue);
        Assert.Equal(200, stored.LSidecarSampleRed);
    }

    [Fact]
    public void WhitebalanceAutomatic_PersistentRecord_OmitsManualFields()
    {
        LSidecarEditRecord record = TInterface.TEditPersistentCreate(TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(new[]
            {
                TInterface.TWorkWhitebalanceCreate(true, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 120)
            }),
            false));
        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);

        Assert.Null(stored.LSidecarWhitebalanceRed);
        Assert.Null(stored.LSidecarSampleRed);
        Assert.DoesNotContain("SampleRed", JsonSerializer.Serialize(record));
    }

    [Fact]
    public void NonWhitebalanceStep_OmitsAndIgnoresWhitebalanceFields()
    {
        LSidecarEditRecord storedRecord = TInterface.TEditPersistentCreate(TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(new[] { TInterface.TWorkContrastCreate(true, 125) }),
            false));
        string json = JsonSerializer.Serialize(storedRecord);
        Assert.DoesNotContain("WhitebalanceMethod", json);
        Assert.DoesNotContain("WhitebalanceSaturation", json);

        LSidecarVideoStep sidecarStep = Assert.Single(storedRecord.LSidecarSteps);
        sidecarStep.LSidecarWhitebalanceMethod = LWhitebalanceMethod.LWhitebalanceMethodAverage;
        sidecarStep.LSidecarWhitebalanceSaturation = 250;
        LWorkVideoStep restored = Assert.Single(
            TInterface.TEditPersistentRead(storedRecord).LEditVideo.LWorkVideoSteps);

        Assert.Null(restored.LWorkStepWhitebalance);
    }

    [Fact]
    public void EditPlanResolve_PersistentWhitebalanceOverridesFileAndKeepsOtherFileSteps()
    {
        LEditPlan file = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(new[]
            {
                TInterface.TWorkBrightnessCreate(true, 25),
                TInterface.TWorkWhitebalanceCreate(true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 80)
            }),
            false);
        LEditPlan persistent = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(new[]
            {
                TInterface.TWorkWhitebalanceCreate(false, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 175.25)
            }),
            false);

        LEditPlan resolved = TInterface.TEditPlanResolve(file, persistent);

        Assert.Contains(resolved.LEditVideo.LWorkVideoSteps,
            step => step.LWorkStepKind == LColorKind.LColorKindBrightness && step.LWorkStepValue == 25);
        LWorkVideoStep whitebalance = Assert.Single(resolved.LEditVideo.LWorkVideoSteps,
            step => step.LWorkStepKind == LColorKind.LColorKindWhitebalance);
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(whitebalance);
        Assert.False(whitebalance.LWorkStepActive);
        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodMinmax, settings.LWorkWhitebalanceMethod);
        Assert.Equal(175.25, settings.LWorkWhitebalanceSaturation);
    }

    [Fact]
    public void EditPlanResolve_PersistentCropApplyOff_OverridesFileCropApplyOn()
    {
        LEditPlan file = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(10, 10, 10, 10, 0, false, false),
            TInterface.TWorkVideoCreate(),
            true);
        LEditPlan persistent = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(20, 20, 20, 20, 0, false, false),
            TInterface.TWorkVideoCreate(),
            false);

        LEditPlan resolved = TInterface.TEditPlanResolve(file, persistent, cropPersistent: true, skipPersistent: false);

        Assert.False(resolved.LEditCropActive);
        Assert.Equal(20, resolved.LEditCrop.LWorkCropLeft);
    }

    [Fact]
    public void EditPlanResolve_PersistentSkipOff_OverridesFileSkipOn()
    {
        LEditPlan file = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(), TInterface.TWorkVideoCreate(), false) with { LEditSkip = true };
        LEditPlan persistent = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(), TInterface.TWorkVideoCreate(), false) with { LEditSkip = false };

        LEditPlan resolved = TInterface.TEditPlanResolve(file, persistent, cropPersistent: false, skipPersistent: true);

        Assert.False(resolved.LEditSkip);
    }

    [Fact]
    public void EditPlanResolve_SkipNotPersistent_KeepsFileSkip()
    {
        LEditPlan file = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(), TInterface.TWorkVideoCreate(), false) with { LEditSkip = true };
        LEditPlan persistent = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(), TInterface.TWorkVideoCreate(), false) with { LEditSkip = false };

        LEditPlan resolved = TInterface.TEditPlanResolve(file, persistent, cropPersistent: false, skipPersistent: false);

        Assert.True(resolved.LEditSkip);
    }

    [Fact]
    public void Curve_PersistentRecord_RoundTripsChannelsAndOmitsIdentity()
    {
        LWorkVideoStep source = TInterface.TWorkCurveCreate(
            true,
            master: new[] { TInterface.TWorkPointCreate(0, 0.1), TInterface.TWorkPointCreate(1, 0.9) },
            red: new[]
            {
                TInterface.TWorkPointCreate(0, 0),
                TInterface.TWorkPointCreate(0.5, 0.75),
                TInterface.TWorkPointCreate(1, 1)
            });

        LSidecarEditRecord record = TInterface.TEditPersistentCreate(TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(), TInterface.TWorkVideoCreate(new[] { source }), false));
        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);

        Assert.Equal("Curve", stored.LSidecarKind);
        Assert.NotNull(stored.LSidecarCurveChannels);
        Assert.Equal(2, stored.LSidecarCurveChannels.Count);
        Assert.DoesNotContain(stored.LSidecarCurveChannels, channel => channel.LSidecarCurveName == "Green");
        Assert.DoesNotContain(stored.LSidecarCurveChannels, channel => channel.LSidecarCurveName == "Blue");

        LWorkVideoStep restored = Assert.Single(
            TInterface.TEditPersistentRead(TInterface.TSidecarEditMatch(record))
                .LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindCurve, restored.LWorkStepKind);
        Assert.True(restored.LWorkStepActive);
        Assert.Equal(TInterface.TWorkCurveFormat(source), TInterface.TWorkCurveFormat(restored));
    }

    [Fact]
    public void EditPersistentRead_UnknownStepToken_CreatesBrightnessStep()
    {
        LSidecarEditRecord record = TInterface.TSidecarEditCreate("Rubbish", true, 40);

        LEditPlan plan = TInterface.TEditPersistentRead(record);

        LWorkVideoStep step = Assert.Single(plan.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindBrightness, step.LWorkStepKind);
    }
}
