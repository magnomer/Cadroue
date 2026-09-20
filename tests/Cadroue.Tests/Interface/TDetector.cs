using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TDetector
{
    [Fact]
    public void LDetectorThresholdRead_SceneUsesScdetDomain()
    {
        LDetectorBound lBound = LDetector.LDetectorThresholdRead(LDetectorKind.LDetectorKindScene);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(100, lBound.LDetectorBoundMost);
        Assert.Equal(50, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorThresholdRead_LuminanceUsesPercentDomain()
    {
        LDetectorBound lBound = LDetector.LDetectorThresholdRead(LDetectorKind.LDetectorKindLuminance);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(50, lBound.LDetectorBoundMost);
        Assert.Equal(8, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorWindowRead_LuminanceUsesSecondsWindow()
    {
        LDetectorBound lBound = LDetector.LDetectorWindowRead(LDetectorKind.LDetectorKindLuminance);

        Assert.Equal(0.1, lBound.LDetectorBoundLeast);
        Assert.Equal(5, lBound.LDetectorBoundMost);
        Assert.Equal(0.5, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorMinimumRead_LuminanceUsesSecondsDuration()
    {
        LDetectorBound lBound = LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindLuminance);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(10, lBound.LDetectorBoundMost);
        Assert.Equal(0.5, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorMinimumRead_SilenceUsesSecondsDuration()
    {
        LDetectorBound lBound = LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindSilence);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(60, lBound.LDetectorBoundMost);
        Assert.Equal(0.5, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorThresholdRead_SilenceUsesDecibelDomain()
    {
        LDetectorBound lBound = LDetector.LDetectorThresholdRead(LDetectorKind.LDetectorKindSilence);

        Assert.Equal(-80, lBound.LDetectorBoundLeast);
        Assert.Equal(0, lBound.LDetectorBoundMost);
        Assert.Equal(-30, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorThresholdRead_VolumeUsesDeltaDomain()
    {
        LDetectorBound lBound = LDetector.LDetectorThresholdRead(LDetectorKind.LDetectorKindVolume);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(30, lBound.LDetectorBoundMost);
        Assert.Equal(20, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorWindowRead_VolumeUsesSecondsWindow()
    {
        LDetectorBound lBound = LDetector.LDetectorWindowRead(LDetectorKind.LDetectorKindVolume);

        Assert.Equal(0.1, lBound.LDetectorBoundLeast);
        Assert.Equal(5, lBound.LDetectorBoundMost);
        Assert.Equal(2, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorMinimumRead_VolumeUsesSecondsDuration()
    {
        LDetectorBound lBound = LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindVolume);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(60, lBound.LDetectorBoundMost);
        Assert.Equal(0.5, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorCreate_VolumeSeedsDefaults()
    {
        LDetectorStep lStep = LDetector.LDetectorCreate(LDetectorKind.LDetectorKindVolume);

        Assert.Equal(LDetectorKind.LDetectorKindVolume, lStep.LDetectorStepKind);
        Assert.False(lStep.LDetectorStepEnabled);
        Assert.Equal(20, lStep.LDetectorStepThreshold);
        Assert.Equal(0.5, lStep.LDetectorStepMinimum);
        Assert.Equal(2, lStep.LDetectorStepWindow);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(50, 50)]
    [InlineData(150, 100)]
    public void LDetectorThresholdClamp_SceneHoldsRange(double lValue, double lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorThresholdClamp(LDetectorKind.LDetectorKindScene, lValue));
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(50, 7.5)]
    [InlineData(100, 3)]
    public void LDetectorThresholdResolve_MapsSensitivityToScdet(double lSensitivity, double lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorThresholdResolve(lSensitivity), 9);
    }

    [Theory]
    [InlineData(200, 0)]
    [InlineData(-1000, 100)]
    public void LDetectorThresholdResolve_ClampsToScdetLimits(double lSensitivity, double lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorThresholdResolve(lSensitivity), 9);
    }

    [Theory]
    [InlineData(50, 50)]
    [InlineData(200, 400.0 / 3.0)]
    [InlineData(-1000, -8800.0 / 9.0)]
    public void LDetectorSensitivityClamp_HoldsPlausibleBand(double lSensitivity, double lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorSensitivityClamp(lSensitivity), 9);
    }

    [Theory]
    [InlineData(24, "Conservative")]
    [InlineData(20, "Normal")]
    [InlineData(16, "Sensitive")]
    public void LDetectorPresetMatch_LufsThresholdPicksPreset(double lThreshold, string lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorPresetMatch(
            LDetectorMetricMode.LDetectorMetricLufs, lThreshold, 2, 0.5));
    }

    [Theory]
    [InlineData(21, "Conservative")]
    [InlineData(19, "Normal")]
    [InlineData(16, "Sensitive")]
    public void LDetectorPresetMatch_DecibelThresholdPicksPreset(double lThreshold, string lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorPresetMatch(
            LDetectorMetricMode.LDetectorMetricRms, lThreshold, 2, 0.5));
    }

    [Fact]
    public void LDetectorPresetMatch_TweakYieldsCustom()
    {
        Assert.Null(LDetector.LDetectorPresetMatch(
            LDetectorMetricMode.LDetectorMetricLufs, 22, 2, 0.5));
    }

    [Fact]
    public void LDetectorPresetMatch_MetricMismatchYieldsCustom()
    {
        Assert.Null(LDetector.LDetectorPresetMatch(
            LDetectorMetricMode.LDetectorMetricRms, 24, 2, 0.5));
    }

    [Theory]
    [InlineData("Conservative", 14, 1.0, 1.5)]
    [InlineData("Normal", 8, 0.5, 0.5)]
    [InlineData("Sensitive", 4, 0.3, 0.3)]
    public void LDetectorLuminanceResolve_TokenYieldsTuning(
        string lToken, double lThreshold, double lWindow, double lMinimum)
    {
        LDetectorLuminanceTuning? lTuning = LDetector.LDetectorLuminanceResolve(lToken);

        Assert.NotNull(lTuning);
        Assert.Equal(lThreshold, lTuning.Value.LDetectorThreshold);
        Assert.Equal(lWindow, lTuning.Value.LDetectorWindow);
        Assert.Equal(lMinimum, lTuning.Value.LDetectorMinimum);
    }

    [Fact]
    public void LDetectorLuminanceResolve_UnknownTokenReturnsNull()
    {
        Assert.Null(LDetector.LDetectorLuminanceResolve("Custom"));
    }

    [Theory]
    [InlineData(14, 1.0, 1.5, "Conservative")]
    [InlineData(8, 0.5, 0.5, "Normal")]
    [InlineData(4, 0.3, 0.3, "Sensitive")]
    public void LDetectorLuminanceMatch_TuningPicksPreset(
        double lThreshold, double lWindow, double lMinimum, string lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorLuminanceMatch(lThreshold, lWindow, lMinimum));
    }

    [Fact]
    public void LDetectorLuminanceMatch_TweakYieldsCustom()
    {
        Assert.Null(LDetector.LDetectorLuminanceMatch(10, 0.5, 0.5));
    }

    [Fact]
    public void LDetectorBlankClamp_BoundsEveryAxis()
    {
        LDetectorBlank lClamped = LDetectorBlank.LDetectorBlankClamp(new LDetectorBlank(
            true, LDetectorType.LDetectorTypeColor, 400, 2.0, 3.0, 5.0, 0.1, 999));

        Assert.Equal(40, lClamped.LDetectorBlankHue);
        Assert.Equal(1, lClamped.LDetectorBlankSaturation);
        Assert.Equal(1, lClamped.LDetectorBlankBrightness);
        Assert.Equal(0.5, lClamped.LDetectorBlankTolerance);
        Assert.Equal(0.5, lClamped.LDetectorBlankCoverage);
        Assert.Equal(60, lClamped.LDetectorBlankMinimum);
    }

    private static LDetectorSet TDetectorSetBuild() => TInterface.TDetectorSetCreate(
        [
            new LDetectorStep(LDetectorKind.LDetectorKindScene, true, 75, 0.5, 0),
            new LDetectorStep(LDetectorKind.LDetectorKindStill, false, 0.05, 2.0, 0),
            new LDetectorStep(LDetectorKind.LDetectorKindLuminance, true, 4.0, 0.3, 0.3),
            new LDetectorStep(LDetectorKind.LDetectorKindSilence, true, -40, 1.5, 0),
            new LDetectorStep(LDetectorKind.LDetectorKindVolume, false, 16, 0.5, 2)
        ],
        new LDetectorBlank(true, LDetectorType.LDetectorTypeColor, 120, 0.5, 0.8, 0.1, 0.9, 1.0),
        LDetectorStillMode.LDetectorStillTreat,
        LDetectorLuminanceMode.LDetectorLuminanceFast,
        LDetectorMetricMode.LDetectorMetricRms,
        new Dictionary<LDetectorKind, string>
        {
            [LDetectorKind.LDetectorKindScene] = "Sensitive",
            [LDetectorKind.LDetectorKindStill] = "Conservative",
            [LDetectorKind.LDetectorKindLuminance] = "Sensitive",
            [LDetectorKind.LDetectorKindSilence] = string.Empty,
            [LDetectorKind.LDetectorKindVolume] = "Sensitive"
        });

    [Fact]
    public void LDetectorSidecarFormat_RoundTripsEveryKind()
    {
        LDetectorSet lSet = TDetectorSetBuild();

        LSidecarSplitRecord lRecord = TInterface.TDetectorSidecarFormat(lSet);
        LDetectorSet lParsed = TInterface.TDetectorSidecarParse(lRecord);

        Assert.Equal(6, lRecord.LSidecarSplitDetectors.Count);
        Assert.True(lRecord.LSidecarSplitActive);
        Assert.Equal(lSet.LDetectorSetSteps, lParsed.LDetectorSetSteps);
        Assert.Equal(lSet.LDetectorSetBlank, lParsed.LDetectorSetBlank);
        Assert.Equal(lSet.LDetectorSetStill, lParsed.LDetectorSetStill);
        Assert.Equal(lSet.LDetectorSetLuminance, lParsed.LDetectorSetLuminance);
        Assert.Equal(lSet.LDetectorSetMetric, lParsed.LDetectorSetMetric);
        Assert.Equal(lSet.LDetectorSetPresets, lParsed.LDetectorSetPresets);
    }

    [Fact]
    public void LDetectorSceneFormat_RoundTripsEveryKind()
    {
        LDetectorSet lSet = TDetectorSetBuild();

        List<LSceneDetector> lDetectors = TInterface.TDetectorSceneFormat(lSet);
        LDetectorSet lParsed = TInterface.TDetectorSceneParse(lDetectors);

        Assert.Equal(6, lDetectors.Count);
        Assert.Equal((int)LDetectorStillMode.LDetectorStillTreat, lDetectors[2].LSceneDetectorType);
        Assert.Equal((int)LDetectorMetricMode.LDetectorMetricRms, lDetectors[5].LSceneDetectorType);
        Assert.Equal(lSet.LDetectorSetSteps, lParsed.LDetectorSetSteps);
        Assert.Equal(lSet.LDetectorSetBlank, lParsed.LDetectorSetBlank);
        Assert.Equal(lSet.LDetectorSetPresets, lParsed.LDetectorSetPresets);
    }

    [Fact]
    public void LDetectorSidecarParse_UnknownKindSkipped()
    {
        var lRecord = new LSidecarSplitRecord
        {
            LSidecarSplitDetectors =
            [
                new LSidecarDetectorRecord { LSidecarDetectorKind = 99, LSidecarDetectorEnabled = true },
                new LSidecarDetectorRecord
                {
                    LSidecarDetectorKind = (int)LDetectorKind.LDetectorKindSilence, LSidecarDetectorThreshold = -20
                }
            ]
        };

        LDetectorSet lParsed = TInterface.TDetectorSidecarParse(lRecord);

        Assert.Single(lParsed.LDetectorSetSteps);
        Assert.Equal(LDetectorKind.LDetectorKindSilence, lParsed.LDetectorSetSteps[0].LDetectorStepKind);
        Assert.Null(lParsed.LDetectorSetBlank);
        Assert.Null(lParsed.LDetectorSetStill);
        Assert.Null(lParsed.LDetectorSetMetric);
    }

    [Fact]
    public void LDetectorSidecarParse_UndefinedTypeFallsBack()
    {
        var lRecord = new LSidecarSplitRecord
        {
            LSidecarSplitDetectors =
            [
                new LSidecarDetectorRecord
                {
                    LSidecarDetectorKind = (int)LDetectorKind.LDetectorKindBlank, LSidecarDetectorType = 7
                },
                new LSidecarDetectorRecord
                {
                    LSidecarDetectorKind = (int)LDetectorKind.LDetectorKindStill, LSidecarDetectorType = 7
                },
                new LSidecarDetectorRecord
                {
                    LSidecarDetectorKind = (int)LDetectorKind.LDetectorKindLuminance, LSidecarDetectorType = 7
                },
                new LSidecarDetectorRecord
                {
                    LSidecarDetectorKind = (int)LDetectorKind.LDetectorKindVolume, LSidecarDetectorType = 7
                }
            ]
        };

        LDetectorSet lParsed = TInterface.TDetectorSidecarParse(lRecord);

        Assert.Equal(LDetectorType.LDetectorTypeBlack, lParsed.LDetectorSetBlank!.Value.LDetectorBlankType);
        Assert.Equal(LDetectorStillMode.LDetectorStillDiscard, lParsed.LDetectorSetStill);
        Assert.Equal(LDetectorLuminanceMode.LDetectorLuminanceNormal, lParsed.LDetectorSetLuminance);
        Assert.Equal(LDetectorMetricMode.LDetectorMetricLufs, lParsed.LDetectorSetMetric);
    }
}
