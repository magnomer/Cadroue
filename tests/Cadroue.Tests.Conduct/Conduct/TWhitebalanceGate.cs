using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Xunit;

namespace Cadroue.Tests;

public sealed class TWhitebalanceGate
{
    private readonly TWhitebalancePort tWhitebalancePort = new();
    private readonly CInspector tWhitebalanceInspector;
    private readonly CWhitebalance tWhitebalanceGate;
    private readonly List<string> tWhitebalanceEstimates = [];
    private readonly List<(bool, bool)> tWhitebalanceTools = [];
    private int tWhitebalanceNotices;

    public TWhitebalanceGate()
    {
        tWhitebalanceInspector = TAtelier.TAtelierCreate(port => port with { LAtelierWhitebalance = tWhitebalancePort })
            .CAtelierInspectorCreate();
        tWhitebalanceGate = tWhitebalanceInspector.CInspectorWhitebalance;
        tWhitebalanceGate.CWhitebalanceChanged += () => tWhitebalanceNotices++;
        tWhitebalanceGate.CWhitebalanceEstimateChanged += tWhitebalanceEstimates.Add;
        tWhitebalanceGate.CWhitebalanceToolChanged += (armed, white) => tWhitebalanceTools.Add((armed, white));
    }

    private void TWhitebalanceArmedSet(bool white)
    {
        tWhitebalanceGate.CWhitebalanceCapableSet(true, true);
        tWhitebalanceGate.CWhitebalanceToolSet(true, white);
    }

    [Fact]
    public void WhitebalanceCreate_Fresh_HoldsCoreDefault()
    {
        Assert.False(tWhitebalanceGate.CWhitebalanceActive);
        Assert.Equal("Median", tWhitebalanceGate.CWhitebalanceToken);
        Assert.Equal(2, tWhitebalanceGate.CWhitebalanceMethodIndex);
        Assert.Equal(["Average", "Minmax", "Median", "Manual"], tWhitebalanceGate.CWhitebalanceTokens);
        Assert.Equal(100, tWhitebalanceGate.CWhitebalanceSaturation);
        Assert.Equal(0, tWhitebalanceGate.CWhitebalanceSaturationLeast);
        Assert.Equal(300, tWhitebalanceGate.CWhitebalanceSaturationMost);
        Assert.False(tWhitebalanceGate.CWhitebalanceManual);
        Assert.False(tWhitebalanceGate.CWhitebalanceCapable);
        Assert.Equal(string.Empty, tWhitebalanceGate.CWhitebalanceGuideKey);
        Assert.False(tWhitebalanceGate.CWhitebalanceGuideShown);
        Assert.Equal(["create"], tWhitebalancePort.TWhitebalanceCalls);
    }

    [Fact]
    public void WhitebalanceMethodSelect_OutsideRows_IsIgnored()
    {
        tWhitebalanceGate.CWhitebalanceMethodSelect(-1);
        tWhitebalanceGate.CWhitebalanceMethodSelect(4);

        Assert.Equal("Median", tWhitebalanceGate.CWhitebalanceToken);
        Assert.Equal(0, tWhitebalanceNotices);
    }

    [Fact]
    public void WhitebalanceMethodSelect_Automatic_ClearsEstimateAndAsksForOne()
    {
        tWhitebalanceGate.CWhitebalanceEstimateSet(0.25, 0.5, true);
        tWhitebalanceGate.CWhitebalanceMethodSelect(0);

        Assert.Equal("Average", tWhitebalanceGate.CWhitebalanceToken);
        Assert.Equal(["Average"], tWhitebalanceEstimates);
        Assert.Equal((0d, 0d, false), tWhitebalanceGate.CWhitebalanceWheelRead());
        Assert.Equal(2, tWhitebalanceNotices);
    }

    [Fact]
    public void WhitebalanceMethodSelect_Manual_AsksForNoEstimate_AndIgnoresOne()
    {
        tWhitebalanceGate.CWhitebalanceMethodSelect(3);
        tWhitebalanceGate.CWhitebalanceEstimateSet(0.25, 0.5, true);

        Assert.True(tWhitebalanceGate.CWhitebalanceManual);
        Assert.Empty(tWhitebalanceEstimates);
        Assert.Equal(1, tWhitebalanceNotices);
    }

    [Fact]
    public void WhitebalanceSaturationSet_HeldInsideRange_RepeatIsSilent()
    {
        tWhitebalanceGate.CWhitebalanceSaturationSet(500);
        tWhitebalanceGate.CWhitebalanceSaturationSet(300);

        Assert.Equal(300, tWhitebalanceGate.CWhitebalanceSaturation);
        Assert.Equal(1, tWhitebalanceNotices);
    }

    [Fact]
    public void WhitebalanceToolSet_Incapable_StaysDisarmed()
    {
        tWhitebalanceGate.CWhitebalanceToolSet(true, false);

        Assert.False(tWhitebalanceGate.CWhitebalanceArmed);
        Assert.Empty(tWhitebalanceTools);
    }

    [Fact]
    public void WhitebalanceToolSet_Armed_GuidesByTarget_AndTellsTheTool()
    {
        TWhitebalanceArmedSet(true);

        Assert.True(tWhitebalanceGate.CWhitebalanceWhiteArmed);
        Assert.False(tWhitebalanceGate.CWhitebalanceGreyArmed);
        Assert.Equal("Inspector.Video.WhitebalanceGuideWhite", tWhitebalanceGate.CWhitebalanceGuideKey);
        Assert.Equal([(true, true)], tWhitebalanceTools);

        tWhitebalanceGate.CWhitebalanceToolSet(true, false);

        Assert.True(tWhitebalanceGate.CWhitebalanceGreyArmed);
        Assert.Equal("Inspector.Video.WhitebalanceGuide", tWhitebalanceGate.CWhitebalanceGuideKey);
        Assert.Equal([(true, true), (true, false)], tWhitebalanceTools);
    }

    [Fact]
    public void WhitebalanceToolToggle_UncheckOfOtherTarget_IsIgnored()
    {
        TWhitebalanceArmedSet(false);

        tWhitebalanceGate.CWhitebalanceToolToggle(true, false);
        Assert.True(tWhitebalanceGate.CWhitebalanceGreyArmed);

        tWhitebalanceGate.CWhitebalanceToolToggle(false, false);
        Assert.False(tWhitebalanceGate.CWhitebalanceArmed);
    }

    [Fact]
    public void WhitebalanceStatusSet_ShownWhileDisarmed_ClearedByDisarm()
    {
        tWhitebalanceGate.CWhitebalanceStatusSet("Inspector.Video.WhitebalanceDecode");
        Assert.Equal("Inspector.Video.WhitebalanceDecode", tWhitebalanceGate.CWhitebalanceGuideKey);
        Assert.True(tWhitebalanceGate.CWhitebalanceGuideShown);

        TWhitebalanceArmedSet(false);
        tWhitebalanceGate.CWhitebalanceToolSet(false, false);

        Assert.Equal(string.Empty, tWhitebalanceGate.CWhitebalanceGuideKey);
    }

    [Fact]
    public void WhitebalanceWheelHandle_Pressed_SamplesUnderManualAndDisarms()
    {
        TWhitebalanceArmedSet(false);
        tWhitebalancePort.TWhitebalanceCalls.Clear();

        tWhitebalanceGate.CWhitebalanceWheelHandle(true, 10, 20, 120);

        LWhitebalanceAnswer answer = tWhitebalanceGate.CWhitebalanceAnswer;
        Assert.True(answer.LWhitebalanceActive);
        Assert.Equal("Manual", answer.LWhitebalanceToken);
        Assert.Equal((1.5, 1d, 0.5), (answer.LWhitebalanceRed, answer.LWhitebalanceGreen, answer.LWhitebalanceBlue));
        Assert.Equal(200, answer.LWhitebalanceSampleRed);
        Assert.False(tWhitebalanceGate.CWhitebalanceArmed);
        Assert.Equal(["canvas", "normalize"], tWhitebalancePort.TWhitebalanceCalls);
        Assert.Equal((true, 200, 100, 50), tWhitebalanceGate.CWhitebalanceReadoutRead());
    }

    [Fact]
    public void WhitebalanceWheelHandle_ReleasedOrIncapable_IsIgnored()
    {
        tWhitebalanceGate.CWhitebalanceWheelHandle(true, 10, 20, 120);
        tWhitebalanceGate.CWhitebalanceCapableSet(true, false);
        tWhitebalanceGate.CWhitebalanceWheelHandle(false, 10, 20, 120);

        Assert.False(tWhitebalanceGate.CWhitebalanceManual);
        Assert.DoesNotContain("canvas", tWhitebalancePort.TWhitebalanceCalls);
    }

    [Fact]
    public void WhitebalanceActiveSet_Off_DisarmsPicker()
    {
        TWhitebalanceArmedSet(false);
        tWhitebalanceGate.CWhitebalanceActiveSet(true);
        tWhitebalanceGate.CWhitebalanceActiveSet(false);

        Assert.False(tWhitebalanceGate.CWhitebalanceActive);
        Assert.False(tWhitebalanceGate.CWhitebalanceArmed);
        Assert.Equal((false, false), tWhitebalanceTools[^1]);
    }

    [Fact]
    public void WhitebalanceCapableSet_PreviewOnlyWhileCapable_LossDisarms()
    {
        TWhitebalanceArmedSet(true);
        Assert.True(tWhitebalanceGate.CWhitebalancePreview);

        tWhitebalanceGate.CWhitebalanceCapableSet(false, true);

        Assert.False(tWhitebalanceGate.CWhitebalancePreview);
        Assert.False(tWhitebalanceGate.CWhitebalanceArmed);
    }

    [Fact]
    public void WhitebalanceReset_KeepsSwitch_ReturnsToDefault()
    {
        tWhitebalanceGate.CWhitebalanceCapableSet(true, true);
        tWhitebalanceGate.CWhitebalanceWheelHandle(true, 10, 20, 120);
        tWhitebalanceGate.CWhitebalanceReset();

        Assert.True(tWhitebalanceGate.CWhitebalanceActive);
        Assert.Equal("Median", tWhitebalanceGate.CWhitebalanceToken);
        Assert.Equal(["Median"], tWhitebalanceEstimates);
    }

    [Fact]
    public void WhitebalancePersistentSet_RepeatIsSilent()
    {
        tWhitebalanceGate.CWhitebalancePersistentSet(true);
        tWhitebalanceGate.CWhitebalancePersistentSet(true);

        Assert.True(tWhitebalanceGate.CWhitebalancePersistent);
        Assert.Equal(1, tWhitebalanceNotices);
    }

    [Fact]
    public void WhitebalanceDotRead_PlacesEstimateOnTheWheel()
    {
        tWhitebalanceGate.CWhitebalanceEstimateSet(0.5, 0.25, true);

        Assert.Equal((55d, 25d, true), tWhitebalanceGate.CWhitebalanceDotRead(120, 5));
    }

    [Fact]
    public void WhitebalanceBitmapRead_AnswersThePortsPixels()
    {
        (int size, int stride, byte[] pixels) = tWhitebalanceGate.CWhitebalanceBitmapRead(8, 0.9);

        Assert.Equal((8, 32, 256), (size, stride, pixels.Length));
    }

    [Fact]
    public void InspectorVideoAccept_PlanStep_IsNormalizedAndHeld()
    {
        var plan = new TInspectorPort
        {
            TInspectorWhitebalance =
                Conduct.TInterface.TWhitebalanceAnswerCreate(true, "Average", 400, 2, 2, 2, 9, 9, 9)
        };

        tWhitebalanceInspector.CInspectorVideo.CInspectorVideoAccept(plan);

        LWhitebalanceAnswer answer = tWhitebalanceGate.CWhitebalanceAnswer;
        Assert.Equal(Conduct.TInterface.TWhitebalanceAnswerCreate(true, "Average", 300, 1, 1, 1, 0, 0, 0), answer);
        Assert.Equal(["Average"], tWhitebalanceEstimates);
    }

    [Fact]
    public void WhitebalanceCreate_Fresh_AnswersMethodKeysAndOrigin()
    {
        Assert.Equal(
            ["Inspector.Video.WhitebalanceMethodAverage", "Inspector.Video.WhitebalanceMethodMinmax",
                "Inspector.Video.WhitebalanceMethodMedian", "Inspector.Video.WhitebalanceMethodCustom"],
            tWhitebalanceGate.CWhitebalanceMethodKeys);
        Assert.Equal(100, tWhitebalanceGate.CWhitebalanceSaturationOrigin);
    }

    [Fact]
    public void WhitebalanceSampleAccept_Valid_SamplesUnderManualAndDisarms()
    {
        TWhitebalanceArmedSet(true);
        tWhitebalancePort.TWhitebalanceCalls.Clear();

        tWhitebalanceGate.CWhitebalanceSampleAccept(tWhitebalancePort);

        LWhitebalanceAnswer answer = tWhitebalanceGate.CWhitebalanceAnswer;
        Assert.True(answer.LWhitebalanceActive);
        Assert.Equal("Manual", answer.LWhitebalanceToken);
        Assert.Equal(1.25, answer.LWhitebalanceRed);
        Assert.Equal(180, answer.LWhitebalanceSampleRed);
        Assert.False(tWhitebalanceGate.CWhitebalanceArmed);
        Assert.Equal(["sample", "normalize"], tWhitebalancePort.TWhitebalanceCalls);
    }

    [Theory]
    [InlineData("Decode", "Inspector.Video.WhitebalanceDecode")]
    [InlineData("Invalid", "Inspector.Video.WhitebalanceInvalid")]
    public void WhitebalanceSampleAccept_Failed_LeavesItsStatus(string status, string key)
    {
        tWhitebalancePort.TWhitebalancePick = (status, 0, 0, 0, 0, 0, 0);

        tWhitebalanceGate.CWhitebalanceSampleAccept(tWhitebalancePort);

        Assert.Equal(key, tWhitebalanceGate.CWhitebalanceGuideKey);
        Assert.Equal("Median", tWhitebalanceGate.CWhitebalanceToken);
        Assert.Equal(1, tWhitebalanceNotices);
    }

    [Fact]
    public void WhitebalanceSampleAccept_UnknownStatus_Throws()
    {
        tWhitebalancePort.TWhitebalancePick = ("Other", 0, 0, 0, 0, 0, 0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => tWhitebalanceGate.CWhitebalanceSampleAccept(tWhitebalancePort));
        Assert.Throws<ArgumentNullException>(() => tWhitebalanceGate.CWhitebalanceSampleAccept(null!));
    }

    [Fact]
    public void WhitebalanceEstimateStart_AsksUnlessManual()
    {
        tWhitebalanceGate.CWhitebalanceEstimateStart();
        tWhitebalanceGate.CWhitebalanceMethodSelect(3);
        tWhitebalanceGate.CWhitebalanceEstimateStart();

        Assert.Equal(["Median"], tWhitebalanceEstimates);
    }

    [Fact]
    public void WhitebalanceToolCancel_DisarmsKeepingTarget()
    {
        TWhitebalanceArmedSet(true);

        tWhitebalanceGate.CWhitebalanceToolCancel();
        tWhitebalanceGate.CWhitebalanceToolCancel();

        Assert.False(tWhitebalanceGate.CWhitebalanceArmed);
        Assert.True(tWhitebalanceGate.CWhitebalanceWhite);
        Assert.Equal([(true, true), (false, true)], tWhitebalanceTools);
    }

    [Fact]
    public void WhitebalanceCreate_NullPort_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => TAtelier.TAtelierCreate(port => port with { LAtelierWhitebalance = null! }));
}
