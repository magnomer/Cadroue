using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Conduct;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests.Conduct;

public sealed class TDetectorGate
{
    [Fact]
    public void Plans_ExcludeBlank_AndKeepFixedNames()
    {
        var detector = new CDetector();
        var other = new CDetector();

        Assert.Equal(5, detector.CDetectorPlans.Count);
        Assert.DoesNotContain(
            detector.CDetectorPlans,
            plan => plan.CDetectorPlanKind == LDetectorKind.LDetectorKindBlank);
        Assert.Equal("Still Image", CDetectorPlan.CDetectorNameRead(LDetectorKind.LDetectorKindStill));
        Assert.Equal(
            LDetectorKind.LDetectorKindStill,
            CDetectorPlan.CDetectorKindRead(CDetectorPlan.CDetectorNameRead(LDetectorKind.LDetectorKindStill)));
        Assert.Null(CDetectorPlan.CDetectorKindRead("Still"));

        IReadOnlyList<string> names = detector.CDetectorPlans
            .Single(plan => plan.CDetectorPlanKind == LDetectorKind.LDetectorKindStill)
            .CDetectorPlanMode.CDetectorGroupNames;
        IReadOnlyList<string> otherNames = other.CDetectorPlans
            .Single(plan => plan.CDetectorPlanKind == LDetectorKind.LDetectorKindStill)
            .CDetectorPlanMode.CDetectorGroupNames;
        Assert.NotSame(names, otherNames);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)names)[0] = "ExaminerScratchMutation");
        Assert.Equal("Inspector.Detector.StillMode.Discard", otherNames[0]);
    }

    [Fact]
    public void Plans_CastMutationCannotChangeSnapshot()
    {
        var detector = new CDetector();
        CDetectorPlan[] snapshot = detector.CDetectorPlans.ToArray();
        var mutable = (IList<CDetectorPlan>)detector.CDetectorPlans;

        Assert.Throws<NotSupportedException>(() => mutable.Clear());
        Assert.Throws<NotSupportedException>(() => mutable[0] = snapshot[^1]);
        Assert.Equal(snapshot.Length, detector.CDetectorPlans.Count);
        for (int index = 0; index < snapshot.Length; index++)
        {
            Assert.Same(snapshot[index], detector.CDetectorPlans[index]);
        }
    }

    [Fact]
    public void StepSet_Normalizes_AndEffectiveChangeNoticesOnce()
    {
        var detector = new CDetector();
        const LDetectorKind kind = LDetectorKind.LDetectorKindScene;
        int notices = 0;
        detector.CDetectorChanged += () => notices++;
        LDetectorStep source = detector.CDetectorStepRead(kind);
        LDetectorStep expected = source with { LDetectorStepThreshold = 400.0 / 3 };

        detector.CDetectorStepSet(source with { LDetectorStepThreshold = double.MaxValue });
        detector.CDetectorStepSet(detector.CDetectorStepRead(kind));

        Assert.Equal(expected, detector.CDetectorStepRead(kind));
        Assert.Equal(1, notices);

        detector.CDetectorThresholdSet(kind, 0.1);
        LDetectorStep scene = detector.CDetectorStepRead(kind);
        detector.CDetectorMinimumSet(kind, scene.LDetectorStepMinimum);
        LDetectorStep minimum = detector.CDetectorStepRead(kind);
        int minimumNotices = notices;
        detector.CDetectorThresholdSet(kind, 0.1);
        detector.CDetectorWindowSet(kind, scene.LDetectorStepWindow);
        LDetectorStep window = detector.CDetectorStepRead(kind);
        int windowNotices = notices;
        Exception? blankException = Record.Exception(
            () => detector.CDetectorThresholdSet(LDetectorKind.LDetectorKindBlank, 1));

        Assert.Equal(
            (scene, 2, scene, 2, typeof(KeyNotFoundException)),
            (minimum, minimumNotices, window, windowNotices, blankException?.GetType()));

        detector.CDetectorStepSet(source with { LDetectorStepThreshold = 120 });
        Assert.Equal(120, detector.CDetectorStepRead(kind).LDetectorStepThreshold);
        detector.CDetectorThresholdSet(kind, 0.1);
        detector.CDetectorThresholdSet(kind, 120);
        Assert.Equal(120, detector.CDetectorStepRead(kind).LDetectorStepThreshold);
    }

    [Fact]
    public void Choice_DriftKeepsToken_AndSelectionRestoresPreset()
    {
        var detector = new CDetector();
        const LDetectorKind kind = LDetectorKind.LDetectorKindStill;
        CDetectorPreset preset = detector.CDetectorPreset;

        preset.CDetectorPresetSelect(kind, "Sensitive");
        detector.CDetectorThresholdSet(kind, 0.3);
        IReadOnlyList<CChoice> drifted = preset.CDetectorChoiceRead(kind);

        Assert.Equal("Sensitive", preset.CDetectorTokenRead(kind));
        Assert.Null(preset.CDetectorMatchRead(kind));
        Assert.True(drifted[^1].CChoiceChosen);
        Assert.Equal("Inspector.Common.PresetCustom", drifted[^1].CChoiceKey);
        Assert.Equal("Inspector.Detector.Sensitive", drifted[^1].CChoiceBasis);

        int sensitive = drifted.ToList().FindIndex(choice => choice.CChoiceToken == "Sensitive");
        preset.CDetectorChoiceSelect(kind, sensitive);

        Assert.Equal("Sensitive", preset.CDetectorMatchRead(kind));
    }

    [Fact]
    public void Choice_InvalidAndCustomIndices_AreSilent()
    {
        var detector = new CDetector();
        const LDetectorKind kind = LDetectorKind.LDetectorKindScene;
        CDetectorPreset preset = detector.CDetectorPreset;
        int notices = 0;
        detector.CDetectorChanged += () => notices++;
        int custom = preset.CDetectorChoiceRead(kind).Count - 1;

        preset.CDetectorChoiceSelect(kind, -1);
        preset.CDetectorChoiceSelect(kind, custom);
        preset.CDetectorChoiceSelect(kind, custom + 1);

        Assert.Equal(0, notices);
        Assert.Equal(LDetectorCatalog.LDetectorTokenDefault, preset.CDetectorTokenRead(kind));
    }

    [Fact]
    public void Metric_FollowsMatchingPreset_ButNotDriftedValue()
    {
        var detector = new CDetector();

        detector.CDetectorMetricSet(LDetectorMetricMode.LDetectorMetricRms);
        Assert.Equal(
            19,
            detector.CDetectorStepRead(LDetectorKind.LDetectorKindVolume).LDetectorStepThreshold);

        detector.CDetectorThresholdSet(LDetectorKind.LDetectorKindVolume, 15);
        detector.CDetectorMetricSet(LDetectorMetricMode.LDetectorMetricLufs);

        Assert.Equal(
            15,
            detector.CDetectorStepRead(LDetectorKind.LDetectorKindVolume).LDetectorStepThreshold);
        Assert.Equal(
            "Normal",
            detector.CDetectorPreset.CDetectorTokenRead(LDetectorKind.LDetectorKindVolume));
    }

    [Fact]
    public void ModeSelections_IgnoreOutOfRange_AndSameValue()
    {
        var detector = new CDetector();
        int notices = 0;
        detector.CDetectorChanged += () => notices++;

        detector.CDetectorModeSelect(1);
        detector.CDetectorModeSelect(1);
        detector.CDetectorModeSelect(7);
        detector.CDetectorSpeedSelect(-1);
        detector.CDetectorMetricSet(detector.CDetectorMetric);

        Assert.Equal(LDetectorStillMode.LDetectorStillTreat, detector.CDetectorMode);
        Assert.Equal(1, notices);
    }
}
