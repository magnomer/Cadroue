using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorGate
{
    private readonly CInspector tInspectorGate = TAtelier.TAtelierCreate()
        .CAtelierInspectorCreate();

    [Fact]
    public void InspectorVideoAccept_Plan_SetsEveryVideoToolFromItsRead()
    {
        TInspectorPort plan = new()
        {
            TInspectorGamma = (true, [10, -20, 30, -40, 50]),
            TInspectorExposure = (true, 1.5),
            TInspectorVolume = (true, 6),
        };

        tInspectorGate.CInspectorVideo.CInspectorVideoAccept(plan);

        Assert.Equal(["tone", "whitebalance", "gamma", "exposure"], plan.TInspectorCalls);
        Assert.True(tInspectorGate.CInspectorGamma.CGammaActive);
        Assert.Equal([10d, -20d, 30d, -40d, 50d], tInspectorGate.CInspectorGamma.CGammaValues);
        Assert.True(tInspectorGate.CInspectorExposure.CExposureActive);
        Assert.Equal(1.5, tInspectorGate.CInspectorExposure.CExposureValue);
        Assert.False(tInspectorGate.CInspectorVolume.CVolumeActive);
        Assert.Equal([false, false, false], tInspectorGate.CInspectorTone.CToneActives);
    }

    [Fact]
    public void GammaValueSet_Requests_MapSlotsAndClamp()
    {
        CGamma gamma = tInspectorGate.CInspectorGamma;
        gamma.CGammaMidtoneSet(-200);
        gamma.CGammaRedSet(-20);
        gamma.CGammaGreenSet(30);
        gamma.CGammaBlueSet(120);
        gamma.CGammaHighlightSet(150);

        Assert.Equal([-100d, -20d, 30d, 100d, 100d], gamma.CGammaValues);
    }

    [Fact]
    public void InspectorAudioAccept_Plan_SetsEveryAudioToolFromItsRead()
    {
        TInspectorPort plan = new()
        {
            TInspectorExposure = (true, 2),
            TInspectorVolume = (true, -6),
            TInspectorEqualizer = (true, [(100, 3), (2000, -4)], null),
        };

        tInspectorGate.CInspectorAudio.CInspectorAudioAccept(plan);

        Assert.Equal(
            ["highpass", "lowpass", "noise", "equalizer", "volume", "leveling", "skip"], plan.TInspectorCalls);
        Assert.True(tInspectorGate.CInspectorVolume.CVolumeActive);
        Assert.Equal(-6, tInspectorGate.CInspectorVolume.CVolumeGain);
        Assert.True(tInspectorGate.CInspectorEqualizer.CEqualizerActive);
        Assert.Equal([(100d, 3d), (2000d, -4d)], tInspectorGate.CInspectorEqualizer.CEqualizerBands);
        Assert.Null(tInspectorGate.CInspectorEqualizer.CEqualizerToken);
        Assert.False(tInspectorGate.CInspectorExposure.CExposureActive);
    }

    [Fact]
    public void InspectorAccept_NoPlan_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => tInspectorGate.CInspectorVideo.CInspectorVideoAccept(null!));
        Assert.Throws<ArgumentNullException>(() => tInspectorGate.CInspectorAudio.CInspectorAudioAccept(null!));
        Assert.Throws<ArgumentNullException>(() => tInspectorGate.CInspectorEditAccept(null!));
    }

    [Fact]
    public void InspectorCapableSet_Facts_EachToolTakesItsOwn()
    {
        tInspectorGate.CInspectorCapableSet(eq: true, exposure: false, whitebalance: false, preview: true);

        Assert.True(tInspectorGate.CInspectorGamma.CGammaCapable);
        Assert.True(tInspectorGate.CInspectorGamma.CGammaPreview);
        Assert.False(tInspectorGate.CInspectorExposure.CExposureCapable);
        Assert.False(tInspectorGate.CInspectorExposure.CExposurePreview);
        Assert.True(tInspectorGate.CInspectorTone.CToneCapable);
        Assert.False(tInspectorGate.CInspectorWhitebalance.CWhitebalanceCapable);

        tInspectorGate.CInspectorCapableSet(eq: false, exposure: true, whitebalance: true, preview: true);

        Assert.True(tInspectorGate.CInspectorWhitebalance.CWhitebalanceCapable);
        Assert.True(tInspectorGate.CInspectorWhitebalance.CWhitebalancePreview);
        Assert.False(tInspectorGate.CInspectorGamma.CGammaCapable);
        Assert.False(tInspectorGate.CInspectorTone.CToneCapable);
        Assert.True(tInspectorGate.CInspectorExposure.CExposureCapable);
        Assert.True(tInspectorGate.CInspectorExposure.CExposurePreview);
    }

    [Fact]
    public void InspectorPersistentChange_OnlyChangedKeptStateIsTold()
    {
        int notices = 0;
        tInspectorGate.CInspectorPersistentChanged += () => notices++;

        tInspectorGate.CInspectorGamma.CGammaActiveSet(true);
        tInspectorGate.CInspectorGamma.CGammaCapableSet(true, true);
        tInspectorGate.CInspectorGamma.CGammaPersistentSet(true);
        tInspectorGate.CInspectorGamma.CGammaPersistentSet(true);
        tInspectorGate.CInspectorExposure.CExposurePersistentSet(true);
        tInspectorGate.CInspectorCurve.CCurvePersistentSet(true);
        tInspectorGate.CInspectorWhitebalance.CWhitebalancePersistentSet(true);
        tInspectorGate.CInspectorVolume.CVolumePersistentSet(true);
        tInspectorGate.CInspectorLoudness.CLoudnessPersistentSet(true);
        tInspectorGate.CInspectorNoise.CNoisePersistentSet(true);
        tInspectorGate.CInspectorHighpass.CFilterPersistentSet(true);
        tInspectorGate.CInspectorLowpass.CFilterPersistentSet(true);
        tInspectorGate.CInspectorEqualizer.CEqualizerPersistentSet(true);
        tInspectorGate.CInspectorTone.CTonePersistentSet(0, true);
        tInspectorGate.CInspectorTone.CTonePersistentSet(1, true);
        tInspectorGate.CInspectorTone.CTonePersistentSet(2, true);
        tInspectorGate.CInspectorSkip.CSkipPersistentSet(true);
        tInspectorGate.CInspectorGamma.CGammaPersistentSet(false);

        Assert.Equal(15, notices);
    }

    [Fact]
    public void InspectorFrame_RequestsCoordinateToolsAndStaySilent()
    {
        int changes = 0;
        int tools = 0;
        tInspectorGate.CInspectorFrame.CInspectorChanged += () => changes++;
        tInspectorGate.CInspectorFrame.CInspectorToolChanged += _ => tools++;

        tInspectorGate.CInspectorFrame.CInspectorStepSet("Crop");
        tInspectorGate.CInspectorFrame.CInspectorToolSet(true);
        tInspectorGate.CInspectorFrame.CInspectorStepSet("Crop");
        tInspectorGate.CInspectorWhitebalance.CWhitebalanceCapableSet(true, true);
        tInspectorGate.CInspectorWhitebalance.CWhitebalanceToolSet(true, true);
        tInspectorGate.CInspectorFrame.CInspectorToolSet(true);

        Assert.False(tInspectorGate.CInspectorWhitebalance.CWhitebalanceArmed);
        tInspectorGate.CInspectorFrame.CInspectorToolSet(false);
        tInspectorGate.CInspectorFrame.CInspectorMinimizedSet(true);
        tInspectorGate.CInspectorFrame.CInspectorMinimizedSet(true);

        Assert.Equal(6, changes);
        Assert.Equal(4, tools);
        Assert.True(tInspectorGate.CInspectorFrame.CInspectorMinimized);
        Assert.False(tInspectorGate.CInspectorFrame.CInspectorToolArmed);
    }

    [Fact]
    public void CurveChannel_SelectAndEdit_KeepsOtherChannelsIdentity()
    {
        Assert.Null(typeof(CCurve).GetMethod("CCurvePointRead"));
        Assert.Null(typeof(CCurve).GetMethod("CCurvePointSelect"));
        Assert.Null(typeof(CCurve).GetMethod("CCurvePointSet"));
        Assert.Null(typeof(CCurve).GetMethod("CCurvePointAdd"));

        CCurve curve = tInspectorGate.CInspectorCurve;

        curve.CCurveChannelSelect(-1);
        curve.CCurveChannelSelect(1);
        curve.CCurvePressHandle(75, 45, 150);

        Assert.Equal(1, curve.CCurveChannel);
        Assert.Equal(1, curve.CCurveSelected);
        Assert.Equal(3, curve.CCurvePoints.Count);

        curve.CCurveMove(0.9, 0.2, true);
        Assert.True(curve.CCurvePoints[1].Item1 < 1);
        Assert.Equal(2, curve.CCurveChannels[0].Count);
        Assert.Equal(3, curve.CCurveChannels[1].Count);

        curve.CCurvePointDelete();
        Assert.Equal(2, curve.CCurvePoints.Count);
        curve.CCurveChannelReset(1);
        Assert.Equal(1, curve.CCurveSelected);

        curve.CCurvePressHandle(75, 75, 150);
        curve.CCurveReset();
        Assert.All(curve.CCurveChannels, points => Assert.Equal(2, points.Count));
        curve.CCurveChannelSelect(9);
        curve.CCurvePressHandle(0, 150, 150);
        Assert.Equal(3, curve.CCurveChannel);
        Assert.Equal(0, curve.CCurveSelected);
        curve.CCurvePointDelete();
        Assert.Equal(2, curve.CCurvePoints.Count);
    }

    [Fact]
    public void CurveReset_ReusesIdentityReference()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;
        IReadOnlyList<(double, double)> identity = curve.CCurveChannels[0];

        Assert.Same(identity, curve.CCurveChannels[1]);
        curve.CCurvePressHandle(75, 75, 150);
        curve.CCurveChannelReset(0);
        Assert.Same(identity, curve.CCurveChannels[0]);

        curve.CCurveChannelSelect(1);
        curve.CCurvePressHandle(75, 75, 150);
        curve.CCurveReset();
        Assert.All(curve.CCurveChannels, points => Assert.Same(identity, points));
    }

    [Fact]
    public void CurveStepSet_ActiveOnly_KeepsSelection()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;
        curve.CCurvePressHandle(75, 60, 150);
        curve.CCurvePressHandle(0, 150, 150);
        int notices = 0;
        curve.CCurveChanged += () => notices++;

        curve.CCurveStepSet(true, curve.CCurveChannels);

        Assert.Equal(1, notices);
        Assert.Equal(0, curve.CCurveSelected);
        Assert.True(curve.CCurveStepRead().CCurveActive);
        Assert.Equal(3, curve.CCurvePoints.Count);
    }

    [Fact]
    public void CurveStepSet_PointsChanged_ClampsSelection()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;
        curve.CCurvePressHandle(45, 105, 150);
        curve.CCurvePressHandle(90, 60, 150);
        curve.CCurvePressHandle(150, 0, 150);
        IReadOnlyList<(double, double)> identity = new[] { (0d, 0d), (1d, 1d) };

        curve.CCurveStepSet(false, new[] { identity, identity, identity, identity });

        Assert.Equal(1, curve.CCurveSelected);
        Assert.Equal(2, curve.CCurvePoints.Count);
        Assert.False(curve.CCurveStepRead().CCurveActive);
    }

    [Fact]
    public void CurvePointSet_RepeatedNaN_IsSilent()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;
        curve.CCurvePressHandle(75, 75, 150);
        int notices = 0;
        curve.CCurveChanged += () => notices++;

        curve.CCurveMove(0.5, double.NaN, true);
        curve.CCurveMove(0.5, double.NaN, true);

        Assert.Equal(1, notices);
    }

    [Fact]
    public void CurveTrackRead_CubicInterpolationMatchesExistingCurve()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;
        curve.CCurvePressHandle(75, 0, 150);
        curve.CCurvePressHandle(150, 0, 150);
        curve.CCurveMove(1, 0, true);

        IReadOnlyList<(double, double)> track = curve.CCurveTrackRead(4);

        Assert.Equal(5, track.Count);
        Assert.Equal(0, track[0].Item2, 6);
        Assert.Equal(0.625, track[1].Item2, 6);
        Assert.Equal(1, track[2].Item2, 6);
        Assert.Equal(0.625, track[3].Item2, 6);
        Assert.Equal(0, track[4].Item2, 6);
    }

    [Fact]
    public void CurvePress_OnPointSelectsIt()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;

        curve.CCurvePressHandle(3, 147, 150);

        Assert.Equal(0, curve.CCurveSelected);
        Assert.Equal(2, curve.CCurvePoints.Count);
    }

    [Fact]
    public void CurvePress_OffPointAddsAndSelects()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;

        curve.CCurvePressHandle(75, 60, 150);

        Assert.Equal(1, curve.CCurveSelected);
        Assert.Equal((0.5, 0.6), curve.CCurvePoints[curve.CCurveSelected]);
    }

    [Fact]
    public void CurvePress_AtPixelThresholdSelectsPoint()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;

        curve.CCurvePressHandle(0, 150, 150);
        curve.CCurvePressHandle(141, 0, 150);

        Assert.Equal(1, curve.CCurveSelected);
        Assert.Equal(2, curve.CCurvePoints.Count);

        curve.CCurvePressHandle(140, 0, 150);
        Assert.Equal(3, curve.CCurvePoints.Count);
    }

    [Fact]
    public void CurvePress_OutsideCanvasMissesThenAddsClampedPoint()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;

        curve.CCurvePressHandle(-30, 150, 150);

        Assert.Equal(3, curve.CCurvePoints.Count);
        Assert.Equal((1.0 / 150, 0), curve.CCurvePoints[curve.CCurveSelected]);
    }

    [Fact]
    public void CurveMove_WhenInactiveDoesNotEdit()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;
        curve.CCurvePressHandle(75, 75, 150);

        bool moved = curve.CCurveMove(0.7, 0.8, false);

        Assert.False(moved);
        Assert.Equal((0.5, 0.5), curve.CCurvePoints[curve.CCurveSelected]);
    }

    [Fact]
    public void CurveMove_WhenActiveEditsSelectedPoint()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;
        curve.CCurvePressHandle(75, 75, 150);

        bool moved = curve.CCurveMove(0.7, 0.8, true);

        Assert.True(moved);
        Assert.Equal((0.7, 0.8), curve.CCurvePoints[curve.CCurveSelected]);
    }

    [Fact]
    public void PointCommit_ReadsPercentText_ClampsToUnit()
    {
        CCurve curve = tInspectorGate.CInspectorCurve;
        curve.CCurvePressHandle(75, 75, 150);

        curve.CCurvePointCommit("60", "250");

        Assert.Equal(60, curve.CCurveInputPercent, 6);
        Assert.Equal(1, curve.CCurvePoints[1].Item2, 6);
    }

    [Fact]
    public void ValueCommit_AboveFloor_KeepsTypedNumber() =>
        Assert.Equal(2000, CInspectorField.CInspectorValueCommit("2000", 100, 20, 20000));

    [Fact]
    public void ValueCommit_Clamps_FallsBackWhenUnparsable()
    {
        Assert.True(CInspectorField.CInspectorDigitCheck("123"));
        Assert.False(CInspectorField.CInspectorDigitCheck("1.2"));
        Assert.True(CInspectorField.CInspectorDecimalCheck("-1.2"));
        Assert.False(CInspectorField.CInspectorDecimalCheck("1,2"));
        Assert.Equal(20, CInspectorField.CInspectorValueCommit("2", 100, 20, 20000));
        Assert.Equal(20000, CInspectorField.CInspectorValueCommit("99999", 100, 20, 20000));
        Assert.Equal(100, CInspectorField.CInspectorValueCommit("abc", 100, 20, 20000));
        Assert.Equal(-7.5, CInspectorField.CInspectorValueCommit("-7.5", 100, null, null));
    }

    [Fact]
    public void ValueFormat_EchoesEquivalentText_ElseFormats()
    {
        Assert.Equal("2000", CInspectorField.CInspectorValueFormat("2000", 2000, "0"));
        Assert.Equal("2000.0", CInspectorField.CInspectorValueFormat("2000.0", 2000, "0"));
        Assert.Equal("1.5", CInspectorField.CInspectorValueFormat("x", 1.5, "0.###"));
        Assert.Equal("3", CInspectorField.CInspectorValueFormat(string.Empty, 3, "0"));
        Assert.Equal(string.Empty, CInspectorField.CInspectorWholeFormat(string.Empty, 0));
        Assert.Equal("12", CInspectorField.CInspectorWholeFormat("x", 12.4));
    }

    [Fact]
    public void SlideCommit_OnlyWhenSliderLeavesClampedCurrent()
    {
        var set = new List<double>();

        CInspectorField.CInspectorSlideCommit(1000, 2000, 0, 1000, set.Add);
        CInspectorField.CInspectorSlideCommit(500, 2000, 0, 1000, set.Add);

        Assert.Equal(new[] { 500.0 }, set);
    }
}
