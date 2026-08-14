using Cadroue.Application;
using Cadroue.Core;

using System.Text.Json;

using Xunit;

namespace Cadroue.Tests;

public sealed class EditPersistenceTests
{
    [Fact]
    public void KindParse_KnownTokens_ResolveKinds()
    {
        Assert.Equal(LColorKind.LColorKindContrast, TInterface.ColorKindParse("Contrast"));
        Assert.Equal(LColorKind.LColorKindBrightness, TInterface.ColorKindParse("Brightness"));
        Assert.Equal(LColorKind.LColorKindGamma, TInterface.ColorKindParse("Gamma"));
        Assert.Equal(LColorKind.LColorKindWhitebalance, TInterface.ColorKindParse("Whitebalance"));
    }

    [Fact]
    public void KindParse_UnknownToken_ResolvesNull()
    {
        Assert.Null(TInterface.ColorKindParse("Rubbish"));
    }

    [Fact]
    public void KindFormat_RoundTripsKnownKinds()
    {
        Assert.Equal("Contrast", TInterface.ColorKindFormat(LColorKind.LColorKindContrast));
        Assert.Equal("Brightness", TInterface.ColorKindFormat(LColorKind.LColorKindBrightness));
        Assert.Equal("Gamma", TInterface.ColorKindFormat(LColorKind.LColorKindGamma));
        Assert.Equal("Whitebalance", TInterface.ColorKindFormat(LColorKind.LColorKindWhitebalance));
        Assert.Equal(LColorKind.LColorKindContrast, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindContrast)));
        Assert.Equal(LColorKind.LColorKindBrightness, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindBrightness)));
        Assert.Equal(LColorKind.LColorKindGamma, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindGamma)));
        Assert.Equal(LColorKind.LColorKindWhitebalance, TInterface.ColorKindParse(TInterface.ColorKindFormat(LColorKind.LColorKindWhitebalance)));
    }

    [Fact]
    public void Gamma_PersistentRecord_RoundTripsValueAndToken()
    {
        LWorkVideo video = TInterface.WorkVideoCreate(new[] { TInterface.WorkGammaCreate(true, 75) });
        LEditPlan source = TInterface.EditPlanCreate(TInterface.WorkCropCreate(), video, false);

        LSidecarEditRecord record = TInterface.EditPersistentCreate(source);
        LEditPlan restored = TInterface.EditPersistentRead(record);

        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        Assert.Equal("Gamma", stored.LSidecarKind);
        LWorkVideoStep step = Assert.Single(restored.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindGamma, step.LWorkStepKind);
        Assert.Equal(75, step.LWorkStepValue);
    }

    [Fact]
    public void Gamma_PersistentRecord_RoundTripsCompletePayload()
    {
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 20.25, -90.5, 10.75, 30.125, 25.5)
        });

        LSidecarEditRecord record = TInterface.EditPersistentCreate(
            TInterface.EditPlanCreate(TInterface.WorkCropCreate(), video, false));
        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(-90.5, stored.LSidecarGammaRed);
        Assert.Equal(10.75, stored.LSidecarGammaGreen);
        Assert.Equal(30.125, stored.LSidecarGammaBlue);
        Assert.Equal(25.5, stored.LSidecarGammaHighlightProtection);
        Assert.NotNull(restored.LWorkStepGamma);
        Assert.Equal(20.25, restored.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(-90.5, restored.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(10.75, restored.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(30.125, restored.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(25.5, restored.LWorkStepGamma.LWorkGammaHighlightProtection);
    }

    [Fact]
    public void Gamma_LegacyRecord_UsesValueAsGlobalAndNeutralAdvancedDefaults()
    {
        LSidecarEditRecord record = TInterface.SidecarEditRecordCreate("Gamma", true, 35);

        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(35, restored.LWorkStepValue);
        Assert.NotNull(restored.LWorkStepGamma);
        Assert.Equal(35, restored.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(0, restored.LWorkStepGamma.LWorkGammaHighlightProtection);
    }

    [Fact]
    public void NonGammaRecord_IgnoresGammaFields()
    {
        LSidecarEditRecord record = TInterface.SidecarEditRecordCreate(
            "Contrast", true, 125, 50, null, null, 100);

        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps);

        Assert.Equal(LColorKind.LColorKindContrast, restored.LWorkStepKind);
        Assert.Null(restored.LWorkStepGamma);
    }

    [Theory]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodAverage)]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMinmax)]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMedian)]
    public void Whitebalance_PersistentRecord_RoundTripsCompletePayload(LWhitebalanceMethod method)
    {
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkWhitebalanceCreate(true, method, 137.625)
        });

        LSidecarEditRecord record = TInterface.EditPersistentCreate(
            TInterface.EditPlanCreate(TInterface.WorkCropCreate(), video, false));
        LSidecarVideoStep stored = Assert.Single(record.LSidecarSteps);
        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps);
        LWorkWhitebalanceSettings settings = TInterface.WorkWhitebalanceRead(restored);

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
        LSidecarEditRecord record = TInterface.SidecarEditRecordCreate("Whitebalance", true, 0);

        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps);
        LWorkWhitebalanceSettings settings = TInterface.WorkWhitebalanceRead(restored);

        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodMedian, settings.LWorkWhitebalanceMethod);
        Assert.Equal(100, settings.LWorkWhitebalanceSaturation);
        Assert.Equal(100, restored.LWorkStepValue);
    }

    [Fact]
    public void WhitebalanceManual_PersistentRecord_RoundTripsCoefficientsAndSamples()
    {
        LSidecarEditRecord record = TInterface.EditPersistentCreate(TInterface.EditPlanCreate(
            TInterface.WorkCropCreate(),
            TInterface.WorkVideoCreate(new[]
            {
                TInterface.WorkWhitebalanceManualCreate(true, 137.5, 1.5, 0.5, 1.1, 200, 150, 100)
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

        LWorkWhitebalanceSettings settings = TInterface.WorkWhitebalanceRead(Assert.Single(
            TInterface.EditPersistentRead(record).LEditVideo.LWorkVideoSteps));
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
        LSidecarEditRecord record = TInterface.EditPersistentCreate(TInterface.EditPlanCreate(
            TInterface.WorkCropCreate(),
            TInterface.WorkVideoCreate(new[]
            {
                TInterface.WorkWhitebalanceManualCreate(true, 137.5, 1.25, 0.5, 1.125, 200, 150, 100)
            }),
            false));
        Assert.Contains("1.125", JsonSerializer.Serialize(record));

        LSidecarVideoStep stored = Assert.Single(
            TInterface.SidecarEditRecordRoundTrip(record).LSidecarSteps);
        Assert.Equal(1.25, stored.LSidecarWhitebalanceRed);
        Assert.Equal(1.125, stored.LSidecarWhitebalanceBlue);
        Assert.Equal(200, stored.LSidecarSampleRed);
    }

    [Fact]
    public void WhitebalanceAutomatic_PersistentRecord_OmitsManualFields()
    {
        LSidecarEditRecord record = TInterface.EditPersistentCreate(TInterface.EditPlanCreate(
            TInterface.WorkCropCreate(),
            TInterface.WorkVideoCreate(new[]
            {
                TInterface.WorkWhitebalanceCreate(true, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 120)
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
        LSidecarEditRecord storedRecord = TInterface.EditPersistentCreate(TInterface.EditPlanCreate(
            TInterface.WorkCropCreate(),
            TInterface.WorkVideoCreate(new[] { TInterface.WorkContrastCreate(true, 125) }),
            false));
        string json = JsonSerializer.Serialize(storedRecord);
        Assert.DoesNotContain("WhitebalanceMethod", json);
        Assert.DoesNotContain("WhitebalanceSaturation", json);

        LSidecarVideoStep sidecarStep = Assert.Single(storedRecord.LSidecarSteps);
        sidecarStep.LSidecarWhitebalanceMethod = LWhitebalanceMethod.LWhitebalanceMethodAverage;
        sidecarStep.LSidecarWhitebalanceSaturation = 250;
        LWorkVideoStep restored = Assert.Single(
            TInterface.EditPersistentRead(storedRecord).LEditVideo.LWorkVideoSteps);

        Assert.Null(restored.LWorkStepWhitebalance);
    }

    [Fact]
    public void EditPlanResolve_PersistentWhitebalanceOverridesFileAndKeepsOtherFileSteps()
    {
        LEditPlan file = TInterface.EditPlanCreate(
            TInterface.WorkCropCreate(),
            TInterface.WorkVideoCreate(new[]
            {
                TInterface.WorkBrightnessCreate(true, 25),
                TInterface.WorkWhitebalanceCreate(true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 80)
            }),
            false);
        LEditPlan persistent = TInterface.EditPlanCreate(
            TInterface.WorkCropCreate(),
            TInterface.WorkVideoCreate(new[]
            {
                TInterface.WorkWhitebalanceCreate(false, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 175.25)
            }),
            false);

        LEditPlan resolved = TInterface.EditPlanResolve(file, persistent);

        Assert.Contains(resolved.LEditVideo.LWorkVideoSteps,
            step => step.LWorkStepKind == LColorKind.LColorKindBrightness && step.LWorkStepValue == 25);
        LWorkVideoStep whitebalance = Assert.Single(resolved.LEditVideo.LWorkVideoSteps,
            step => step.LWorkStepKind == LColorKind.LColorKindWhitebalance);
        LWorkWhitebalanceSettings settings = TInterface.WorkWhitebalanceRead(whitebalance);
        Assert.False(whitebalance.LWorkStepActive);
        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodMinmax, settings.LWorkWhitebalanceMethod);
        Assert.Equal(175.25, settings.LWorkWhitebalanceSaturation);
    }

    [Fact]
    public void EditPersistentRead_UnknownStepToken_CreatesBrightnessStep()
    {
        LSidecarEditRecord record = TInterface.SidecarEditRecordCreate("Rubbish", true, 40);

        LEditPlan plan = TInterface.EditPersistentRead(record);

        LWorkVideoStep step = Assert.Single(plan.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindBrightness, step.LWorkStepKind);
    }
}
