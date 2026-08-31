using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TWhitebalance
{
    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(137.625, 137.625, 1.37625)]
    [InlineData(301, 300, 3)]
    public void Whitebalance_FactoryClampsSaturationAndMapsFfmpegValue(
        double saturation, double expectedPercent, double expectedFactor)
    {
        LWorkVideoStep step = TInterface.TWorkWhitebalanceCreate(
            true, LWhitebalanceMethod.LWhitebalanceMethodAverage, saturation);
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(step);

        Assert.Equal(expectedPercent, step.LWorkStepValue);
        Assert.Equal(expectedPercent, settings.LWorkWhitebalanceSaturation);
        Assert.Equal(expectedFactor, step.LWorkFfmpegValue, 12);
    }

    [Fact]
    public void Whitebalance_UnknownMethodNormalizesToMedian()
    {
        LWorkVideoStep step = TInterface.TWorkWhitebalanceCreate(true, (LWhitebalanceMethod)999, 100);

        Assert.Equal(
            LWhitebalanceMethod.LWhitebalanceMethodMedian,
            TInterface.TWorkWhitebalanceRead(step).LWorkWhitebalanceMethod);
    }

    [Fact]
    public void Whitebalance_ReadHelperNormalizesMalformedPayload()
    {
        LWorkVideoStep step = TInterface.TWorkMalformedCreate(
            (LWhitebalanceMethod)999, 150, 500);

        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(step);

        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodMedian, settings.LWorkWhitebalanceMethod);
        Assert.Equal(300, settings.LWorkWhitebalanceSaturation);
    }

    [Fact]
    public void Whitebalance_IsIndependentOfGammaPayload()
    {
        LWorkVideoStep whitebalance = TInterface.TWorkWhitebalanceCreate(
            true, LWhitebalanceMethod.LWhitebalanceMethodMedian, 100);
        LWorkVideoStep gamma = TInterface.TWorkGammaCreate(true, 20);

        Assert.Null(whitebalance.LWorkStepGamma);
        Assert.Null(gamma.LWorkStepWhitebalance);
    }

    [Fact]
    public void Whitebalance_DiagnosticIncludesMethodAndSaturation()
    {
        LWorkVideoStep step = TInterface.TWorkWhitebalanceCreate(
            true, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 123.75);

        Assert.Equal(
            "LColorKindWhitebalance 123.75 (method Minmax, saturation 123.75)",
            TInterface.TWorkDiagnosticRead(step));
    }

    [Fact]
    public void WhitebalanceManual_FactoryClampsCoefficientsAndSamples()
    {
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(
            TInterface.TWorkManualCreate(true, 100, 2.5, -1, 1.37, 300, -5, 128));

        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodManual, settings.LWorkWhitebalanceMethod);
        Assert.Equal(2, settings.LWorkWhitebalanceRed);
        Assert.Equal(0, settings.LWorkWhitebalanceGreen);
        Assert.Equal(1.37, settings.LWorkWhitebalanceBlue);
        Assert.Equal(255, settings.LWorkSampleRed);
        Assert.Equal(0, settings.LWorkSampleGreen);
        Assert.Equal(128, settings.LWorkSampleBlue);
    }

    [Fact]
    public void WhitebalanceManual_ReadHelperNormalizesMalformedPayload()
    {
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(
            TInterface.TWorkBrokenCreate(
                150, double.NaN, double.PositiveInfinity, -3, 999, -10, 128));

        Assert.Equal(150, settings.LWorkWhitebalanceSaturation);
        Assert.Equal(1, settings.LWorkWhitebalanceRed);
        Assert.Equal(1, settings.LWorkWhitebalanceGreen);
        Assert.Equal(0, settings.LWorkWhitebalanceBlue);
        Assert.Equal(255, settings.LWorkSampleRed);
        Assert.Equal(0, settings.LWorkSampleGreen);
        Assert.Equal(128, settings.LWorkSampleBlue);
    }

    [Fact]
    public void WhitebalanceAutomatic_ReadDiscardsStrayManualCoefficients()
    {
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(
            TInterface.TWorkStrayCreate(
                LWhitebalanceMethod.LWhitebalanceMethodAverage, 1.5, 200));

        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodAverage, settings.LWorkWhitebalanceMethod);
        Assert.Equal(1, settings.LWorkWhitebalanceRed);
        Assert.Equal(0, settings.LWorkSampleRed);
    }

    [Fact]
    public void WhitebalanceManual_DiagnosticIncludesGainAndSample()
    {
        LWorkVideoStep step = TInterface.TWorkManualCreate(
            true, 150, 1.25, 0.8, 1.1, 210, 180, 170);

        Assert.Equal(
            "LColorKindWhitebalance 150 (method Manual, saturation 150, gain 1.25/0.8/1.1, sample 210/180/170)",
            TInterface.TWorkDiagnosticRead(step));
    }

    [Fact]
    public void WhitebalanceManual_QueuedWorkJsonRoundTripPreservesCoefficients()
    {
        const double blue = 1.234567890123;
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkManualCreate(true, 137.5, 1.5, 0.5, blue, 200, 150, 100)
        });
        LWorkItem source = Assert.Single(TInterface.TEditItemsCreate(
            LWorkPriority.LWorkPriorityNormal,
            TInterface.TEditDescriptionCreate(
                "source.mov",
                TimeSpan.FromMinutes(1),
                TInterface.TWorkCropCreate(),
                video,
                TWorkOutput.TWorkOutputCreate("{OriginalName}_edit", "mp4")),
            "edit-tab",
            _ => { },
            _ => { },
            Guid.NewGuid()));

        LWorkItem restored = Assert.IsType<LWorkItem>(TInterface.TWorkRecordMatch(source));
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(
            Assert.Single(restored.LWorkVideo.LWorkVideoSteps));

        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodManual, settings.LWorkWhitebalanceMethod);
        Assert.Equal(137.5, settings.LWorkWhitebalanceSaturation);
        Assert.Equal(1.5, settings.LWorkWhitebalanceRed);
        Assert.Equal(0.5, settings.LWorkWhitebalanceGreen);
        Assert.Equal(blue, settings.LWorkWhitebalanceBlue);
        Assert.Equal(200, settings.LWorkSampleRed);
        Assert.Equal(150, settings.LWorkSampleGreen);
        Assert.Equal(100, settings.LWorkSampleBlue);
    }

    [Fact]
    public void Whitebalance_QueuedWorkJsonRoundTripPreservesNestedPayload()
    {
        const double saturation = 123.456789012345;
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodMinmax, saturation)
        });
        LWorkItem source = Assert.Single(TInterface.TEditItemsCreate(
            LWorkPriority.LWorkPriorityNormal,
            TInterface.TEditDescriptionCreate(
                "source.mov",
                TimeSpan.FromMinutes(1),
                TInterface.TWorkCropCreate(),
                video,
                TWorkOutput.TWorkOutputCreate("{OriginalName}_edit", "mp4")),
            "edit-tab",
            _ => { },
            _ => { },
            Guid.NewGuid()));

        LWorkItem restored = Assert.IsType<LWorkItem>(TInterface.TWorkRecordMatch(source));
        LWorkVideoStep step = Assert.Single(restored.LWorkVideo.LWorkVideoSteps);
        LWorkWhitebalanceSettings settings = TInterface.TWorkWhitebalanceRead(step);

        Assert.Equal(LColorKind.LColorKindWhitebalance, step.LWorkStepKind);
        Assert.True(step.LWorkStepActive);
        Assert.Equal(saturation, step.LWorkStepValue);
        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodMinmax, settings.LWorkWhitebalanceMethod);
        Assert.Equal(saturation, settings.LWorkWhitebalanceSaturation);
    }
}
