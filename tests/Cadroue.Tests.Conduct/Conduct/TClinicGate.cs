using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TClinicGate
{
    private const string TClinicPath = @"C:\Media\Clip.mp4";
    private const string TClinicRecased = @"c:\media\clip.MP4";

    private static readonly IReadOnlyList<string> TClinicWording =
        ["untested", "scanning", "clean", "failed", "defect", "evidence", "repair"];

    private readonly CClinic tClinicGate = TClinicCreate();

    [Fact]
    public void FixedBasis_NeedsARepair_FallsBackToSource()
    {
        int plans = 0;
        tClinicGate.CClinicPlanChanged += () => plans++;

        tClinicGate.CSalvageModeSet(true);
        tClinicGate.CSalvageBasisSet(true);

        Assert.False(tClinicGate.CClinicRepairCheck());
        Assert.False(tClinicGate.CClinicSalvageRead().CClinicFixed);
        Assert.True(tClinicGate.CClinicSalvageRead().CClinicSeparate);
        Assert.Equal(1, plans);

        tClinicGate.CClinicStepSet("Container");
        tClinicGate.CClinicRepairSet(true);
        tClinicGate.CSalvageBasisSet(true);

        Assert.True(tClinicGate.CClinicSalvageRead().CClinicFixed);
        Assert.Equal(3, plans);

        tClinicGate.CClinicRepairSet(false);

        Assert.False(tClinicGate.CClinicSalvageRead().CClinicFixed);
    }

    [Fact]
    public void SalvageStep_ShowsSalvage_WithoutAKind()
    {
        tClinicGate.CClinicStepSet(CClinic.CClinicSalvageStep);

        Assert.True(tClinicGate.CClinicSalvageShown);
        Assert.Null(tClinicGate.CClinicKind);

        tClinicGate.CClinicRepairSet(true);

        Assert.False(tClinicGate.CClinicRepairCheck());
    }

    [Fact]
    public void PlanRoundTrip_KeepsStepsAndSalvage_NoPlanNotice()
    {
        tClinicGate.CClinicStepSet("Timing");
        tClinicGate.CClinicRepairSet(true);
        tClinicGate.CSalvageActiveSet(true);
        LClinicPort plan = TInterface.TClinicOutletCreate();
        tClinicGate.CClinicPlanApply(plan);

        CClinic restored = TClinicCreate();
        int plans = 0;
        restored.CClinicPlanChanged += () => plans++;
        restored.CClinicPlanAccept(plan);
        LClinicPort read = TInterface.TClinicOutletCreate();
        restored.CClinicPlanApply(read);

        Assert.True(restored.CClinicRepairCheck());
        Assert.Equal(plan.LClinicPlanRead().LClinicSteps, read.LClinicPlanRead().LClinicSteps);
        Assert.True(read.LClinicPlanRead().LClinicActive);
        Assert.Contains(read.LClinicPlanRead().LClinicSteps, step => step is ("Timing", true, false));
        Assert.Equal(11, read.LClinicPlanRead().LClinicSteps.Count);
        Assert.Equal(0, plans);
    }

    [Fact]
    public void FieldSetters_ChangeOneField_NormalizeBasis()
    {
        int plans = 0;
        tClinicGate.CClinicPlanChanged += () => plans++;

        tClinicGate.CSalvageActiveSet(true);
        tClinicGate.CSalvageModeSet(true);
        tClinicGate.CSalvagePersistentSet(true);
        tClinicGate.CSalvageBasisSet(true);

        var salvage = tClinicGate.CClinicSalvageRead();
        Assert.True(salvage.CClinicActive);
        Assert.True(salvage.CClinicSeparate);
        Assert.True(salvage.CClinicPersistent);
        Assert.False(salvage.CClinicFixed);
        Assert.False(salvage.CClinicFixable);
        Assert.Equal("Clinic.Salvage.Basis.None", salvage.CClinicDescription);
        Assert.Equal(3, plans);

        tClinicGate.CClinicStepSet("Container");
        tClinicGate.CClinicRepairSet(true);
        tClinicGate.CSalvageBasisSet(true);

        Assert.True(tClinicGate.CClinicSalvageRead().CClinicFixed);
        Assert.Equal("Clinic.Salvage.Basis.Fixed.Description", tClinicGate.CClinicSalvageRead().CClinicDescription);
        Assert.Equal(5, plans);
    }

    [Fact]
    public void FaceReads_FollowStepAndSalvage()
    {
        Assert.False(tClinicGate.CClinicKnown);
        Assert.False(tClinicGate.CClinicStepShown);
        Assert.False(tClinicGate.CClinicPersistentAllowed);
        Assert.False(tClinicGate.CClinicResultShown);
        Assert.Null(tClinicGate.CClinicSimpleKey);
        Assert.Equal("Clinic.Header.Title", tClinicGate.CClinicTitleKey);

        tClinicGate.CClinicStepSet(CClinic.CClinicSalvageStep);
        Assert.True(tClinicGate.CClinicKnown);
        Assert.False(tClinicGate.CClinicStepShown);
        Assert.False(tClinicGate.CClinicPersistentAllowed);
        Assert.False(tClinicGate.CClinicResultShown);

        tClinicGate.CClinicStepSet("Timing");
        tClinicGate.CClinicRepairSet(true);
        Assert.True(tClinicGate.CClinicStepShown);
        Assert.True(tClinicGate.CClinicPersistentAllowed);
        Assert.True(tClinicGate.CClinicRepairChecked);
        Assert.False(tClinicGate.CClinicPersistentChecked);
        Assert.True(tClinicGate.CClinicResultShown);
        Assert.False(tClinicGate.CClinicScanShown);
        Assert.Equal(0, tClinicGate.CClinicProgressValue);
        Assert.Equal("Processing.Step.Timing", tClinicGate.CClinicTitleKey);
        Assert.Equal("Clinic.Step.Timing.Technical", tClinicGate.CClinicTechnicalKey);
    }

    [Fact]
    public void ResultAndProgress_ReadBack_UnderRecasedPath()
    {
        int changes = 0;
        tClinicGate.CClinicChanged += () => changes++;
        tClinicGate.CClinicSourceSet(TClinicPath);
        tClinicGate.CClinicStepSet("Container");
        changes = 0;

        tClinicGate.CClinicResultAccept(TInterface.TClinicResultCreate(TClinicRecased, "Container", "Scanning"));
        tClinicGate.CClinicProgressSet(TClinicRecased, 0.5);

        Assert.Equal("scanning", tClinicGate.CClinicResultFormat(TClinicWording));
        Assert.True(tClinicGate.CClinicScanShown);
        Assert.Equal(0.5, tClinicGate.CClinicProgressValue);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void ResultsRemove_DropsResultAndProgress_NotifiesShownSource()
    {
        tClinicGate.CClinicSourceSet(TClinicPath);
        tClinicGate.CClinicStepSet("Container");
        tClinicGate.CClinicResultAccept(TInterface.TClinicResultCreate(TClinicPath, "Container", "Scanning"));
        tClinicGate.CClinicProgressSet(TClinicPath, 0.5);
        int changes = 0;
        tClinicGate.CClinicChanged += () => changes++;

        tClinicGate.CClinicResultsRemove([TClinicRecased]);

        Assert.Equal("untested", tClinicGate.CClinicResultFormat(TClinicWording));
        Assert.Equal(0, tClinicGate.CClinicProgressRead());
        Assert.Equal(1, changes);

        tClinicGate.CClinicResultsRemove([TClinicPath]);

        Assert.Equal(1, changes);
    }

    [Fact]
    public void ResultsRemove_OtherPath_KeepsShownSourceQuiet()
    {
        tClinicGate.CClinicSourceSet(TClinicPath);
        tClinicGate.CClinicStepSet("Container");
        tClinicGate.CClinicResultAccept(TInterface.TClinicResultCreate(TClinicPath, "Container", "Clean"));
        int changes = 0;
        tClinicGate.CClinicChanged += () => changes++;

        tClinicGate.CClinicResultsRemove([@"C:\Media\Other.mp4"]);

        Assert.Equal("clean", tClinicGate.CClinicResultFormat(TClinicWording));
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Accept_NoRecord_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => tClinicGate.CClinicPlanAccept(null!));
        Assert.Throws<ArgumentNullException>(() => tClinicGate.CClinicResultAccept(null!));
        Assert.Throws<ArgumentNullException>(() => tClinicGate.CClinicPlanApply(null!));
        Assert.Null(tClinicGate.CClinicSource);
    }

    private static CClinic TClinicCreate() => TAtelier.TAtelierCreate()
        .CAtelierInspectorCreate()
        .CInspectorClinic;
}
