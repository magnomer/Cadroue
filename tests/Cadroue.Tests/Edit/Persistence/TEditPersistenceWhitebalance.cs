using Cadroue.Application;
using Cadroue.Core;

using System.Text.Json;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEditPersistenceWhitebalance
{
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
}
