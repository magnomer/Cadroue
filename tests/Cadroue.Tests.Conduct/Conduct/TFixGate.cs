using System;
using System.Linq;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TFixGate
{
    private static readonly Guid TFixRelay = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TFixTab = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TFixBatchA = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TFixBatchB = new("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid TFixBatchC = new("cccccccc-0000-0000-0000-000000000003");

    private static readonly (string CFixPath, Guid CFixBatch, bool CFixLocked)[] TFixItems =
    [
        (@"C:\a.mp4", TFixBatchA, false),
        (@"C:\b.mp4", TFixBatchB, true),
        (@"C:\c.mp4", TFixBatchC, false),
    ];

    private readonly TFixPort tFixPort = new();
    private readonly CFix tFixGate;
    private readonly CFixPlan tFixPlanGate;
    private readonly CClinic tFixClinic;
    private readonly CWorkspace tFixWorkspace;

    public TFixGate()
    {
        CAtelier atelier =
            TAtelier.TAtelierCreate(port => port with { LAtelierFix = tFixPort, LAtelierFixPlan = tFixPort });
        tFixGate = atelier.CAtelierFix;
        tFixPlanGate = atelier.CAtelierFix.CFixPlan;
        tFixClinic = atelier.CAtelierInspectorCreate().CInspectorClinic;
        tFixWorkspace = atelier.CAtelierWorkspaceCreate();
    }

    [Fact]
    public void FixRun_InvalidPreset_AnswersFalseAndRunsNothing()
    {
        Assert.False(tFixGate.CFixRun(false, "Alpha", @"C:\a.mp4", TFixItems, true, TFixRelay, TFixTab));
        Assert.False(tFixGate.CFixAllRun(false, "Alpha", TFixItems, null, TFixRelay, TFixTab));
        Assert.Empty(tFixPort.TFixCalls);
    }

    [Fact]
    public void FixRun_CurrentPath_RunsThatItemIgnoringCaseAtTheGivenPriority()
    {
        Assert.True(tFixGate.CFixRun(true, "Alpha", @"c:\C.MP4", TFixItems, true, TFixRelay, TFixTab));
        Assert.True(tFixGate.CFixRun(true, "Alpha", @"C:\a.mp4", TFixItems, false, TFixRelay, TFixTab));

        Assert.Equal(
            [
                $@"Alpha|C:\c.mp4@{TFixBatchC:N}|urgent|{TFixRelay}|{TFixTab}",
                $@"Alpha|C:\a.mp4@{TFixBatchA:N}|normal|{TFixRelay}|{TFixTab}",
            ],
            tFixPort.TFixCalls);
    }

    [Fact]
    public void FixRun_LockedMissingOrNoPath_StillAsksThePortWithNothing()
    {
        Assert.True(tFixGate.CFixRun(true, "Alpha", @"C:\b.mp4", TFixItems, true, TFixRelay, TFixTab));
        Assert.True(tFixGate.CFixRun(true, "Alpha", @"C:\z.mp4", TFixItems, true, TFixRelay, TFixTab));
        Assert.True(tFixGate.CFixRun(true, "Alpha", null, TFixItems, true, TFixRelay, TFixTab));

        Assert.Equal(3, tFixPort.TFixCalls.Count);
        Assert.All(tFixPort.TFixCalls, call => Assert.StartsWith("Alpha||urgent|", call));
    }

    [Fact]
    public void FixAllRun_Paths_RunsUnlockedItemsAmongThemInDocketOrder()
    {
        Assert.True(tFixGate.CFixAllRun(true, "Alpha", TFixItems, null, TFixRelay, TFixTab));
        Assert.True(tFixGate.CFixAllRun(
            true, "Alpha", TFixItems, [@"C:\C.MP4", @"C:\b.mp4"], TFixRelay, TFixTab));

        Assert.Equal(
            [
                $@"Alpha|C:\a.mp4@{TFixBatchA:N},C:\c.mp4@{TFixBatchC:N}|normal|{TFixRelay}|{TFixTab}",
                $@"Alpha|C:\c.mp4@{TFixBatchC:N}|normal|{TFixRelay}|{TFixTab}",
            ],
            tFixPort.TFixCalls);
    }

    [Fact]
    public void StateSave_OpenUnlockedSource_SavesToItAndAnswersEachStep()
    {
        tFixClinic.CClinicStepSet("Timing");
        tFixClinic.CClinicRepairSet(true);
        tFixWorkspace.CWorkspaceMediaSet(@"c:\A.MP4", true);

        var steps = tFixPlanGate.CFixStateSave(tFixClinic, tFixWorkspace, TFixItems);

        Assert.Equal([@"save|c:\A.MP4|C:\a.mp4,C:\c.mp4"], tFixPort.TFixCalls);
        Assert.Equal(12, steps.Count);
        Assert.Contains(("Timing", true), steps);
        Assert.Equal((CClinic.CClinicSalvageStep, false), steps[^1]);
        Assert.Single(steps, step => step.CFixActive);
    }

    [Fact]
    public void StateSave_NoSourceLockedOrHeld_OnlyReadsThePlan()
    {
        tFixPlanGate.CFixStateSave(tFixClinic, tFixWorkspace, TFixItems);
        tFixWorkspace.CWorkspaceMediaSet(@"C:\B.mp4", true);
        tFixPlanGate.CFixStateSave(tFixClinic, tFixWorkspace, TFixItems);
        tFixWorkspace.CWorkspaceMediaSet(@"C:\a.mp4", true);
        tFixClinic.CClinicSaveSuspend();
        var steps = tFixPlanGate.CFixStateSave(tFixClinic, tFixWorkspace, TFixItems);

        Assert.Equal(["create", "create", "create"], tFixPort.TFixCalls);
        Assert.Equal(12, steps.Count);
        Assert.DoesNotContain(steps, step => step.CFixActive);
    }

    [Fact]
    public void SourceOpen_SavesTheLeftSource_ThenTheClinicFollows()
    {
        tFixWorkspace.CWorkspaceMediaSet(@"C:\c.mp4", true);

        tFixPlanGate.CFixSourceOpen(tFixClinic, tFixWorkspace, TFixItems, @"C:\z.mp4");

        Assert.Equal([@"save|C:\c.mp4|C:\a.mp4,C:\c.mp4"], tFixPort.TFixCalls);
        Assert.Equal(@"C:\z.mp4", tFixClinic.CClinicSource);
    }

    [Fact]
    public void PersistentSave_HeldSaves_AskNothing()
    {
        tFixClinic.CClinicSaveSuspend();
        tFixPlanGate.CFixPersistentSave(tFixClinic, [@"C:\a.mp4"]);
        tFixClinic.CClinicSaveResume();
        tFixPlanGate.CFixPersistentSave(tFixClinic, [@"C:\a.mp4", @"C:\c.mp4"]);

        Assert.Equal([@"persistent|C:\a.mp4,C:\c.mp4"], tFixPort.TFixCalls);
    }

    [Fact]
    public void PlanRestore_ClinicTakesTheAnsweredPlan_StepsReadFromIt()
    {
        LClinicPort restore = TInterface.TClinicOutletCreate();
        Conduct.TInterface.TClinicPlanSet(restore, [("Index", true, false)], true, false, false, false);
        tFixPort.TFixRestored = restore;

        var steps = tFixPlanGate.CFixPlanRestore(tFixClinic, @"C:\a.mp4");

        Assert.Equal([@"restore|C:\a.mp4"], tFixPort.TFixCalls);
        Assert.True(tFixClinic.CClinicSalvageRead().CClinicFixable);
        Assert.True(tFixClinic.CClinicSalvageRead().CClinicActive);
        Assert.Equal([("Index", true), (CClinic.CClinicSalvageStep, true)], steps);
    }

    [Fact]
    public void LayoutApply_MissingMinimizes_PlanIsTaken_StepsReadAfter()
    {
        LClinicPort plan = TInterface.TClinicOutletCreate();
        Conduct.TInterface.TClinicPlanSet(plan, [("Coded", true, true)], false, false, false, false);

        var none = tFixPlanGate.CFixLayoutApply(tFixClinic, false, null);
        var missing = tFixPlanGate.CFixLayoutApply(tFixClinic, true, plan);
        var taken = tFixPlanGate.CFixLayoutApply(tFixClinic, false, plan);

        Assert.Equal(["create", "create", "create"], tFixPort.TFixCalls);
        Assert.True(tFixClinic.CClinicMinimized);
        Assert.DoesNotContain(none, step => step.CFixActive);
        Assert.DoesNotContain(missing, step => step.CFixActive);
        Assert.Equal([("Coded", true)], taken.Where(step => step.CFixActive));
    }

    [Fact]
    public void FixRun_NoItems_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => tFixGate.CFixRun(true, "Alpha", null, null!, true, default, default));
        Assert.Throws<ArgumentNullException>(() => tFixGate.CFixAllRun(true, "Alpha", null!, null, default, default));
    }
}
