using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TSectionSweepGate
{
    private readonly TFlowPort tSectionSweepPort = new();
    private readonly CFlow tSectionSweepFlow;
    private readonly CSection tSectionSweepGate;
    private readonly List<bool> tSectionSweepTurns = [];

    public TSectionSweepGate()
    {
        tSectionSweepFlow = TAtelier.TAtelierCreate(port => port with { LAtelierFlow = tSectionSweepPort })
            .CAtelierWorkspaceCreate()
            .CWorkspaceFlow;
        tSectionSweepGate = tSectionSweepFlow.CFlowSection;
        tSectionSweepGate.CSectionEditableChanged += tSectionSweepTurns.Add;
        tSectionSweepFlow.CFlowCommandSet(true);
        tSectionSweepFlow.CFlowAttach(null!, "full:a", TimeSpan.FromSeconds(5));
        tSectionSweepFlow.CFlowPaletteApply(4);
        tSectionSweepPort.TFlowSection.TSectionCalls.Clear();
    }

    [Fact]
    public async Task SweepRun_ScanRunsLocked_ResultApplies_SwitchTurnsBack()
    {
        bool? during = null;

        bool applied = await tSectionSweepGate.CSectionSweepRun(
            () =>
            {
                during = tSectionSweepGate.CSectionEditable;
                return Task.CompletedTask;
            },
            [],
            [],
            [],
            () => false);

        Assert.True(applied);
        Assert.False(during);
        Assert.True(tSectionSweepGate.CSectionEditable);
        Assert.Equal([false, true], tSectionSweepTurns);
        Assert.Equal(["combine:60:4"], tSectionSweepPort.TFlowSection.TSectionCalls);
    }

    [Fact]
    public async Task SweepRun_ItemLockedByTheEnd_SwitchStaysOff()
    {
        bool applied = await tSectionSweepGate.CSectionSweepRun(() => Task.CompletedTask, [], [], [], () => true);

        Assert.True(applied);
        Assert.False(tSectionSweepGate.CSectionEditable);
        Assert.Equal([false], tSectionSweepTurns);
    }

    [Fact]
    public async Task SweepRun_ScanThrows_NothingApplies_SwitchTurnsBack()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => tSectionSweepGate.CSectionSweepRun(
            () => Task.FromException(new OperationCanceledException()), [], [], [], () => false));

        Assert.True(tSectionSweepGate.CSectionEditable);
        Assert.Equal([false, true], tSectionSweepTurns);
        Assert.Empty(tSectionSweepPort.TFlowSection.TSectionCalls);
    }

    [Fact]
    public async Task SweepRun_FlowUnloaded_Refuses_SwitchTurnsBack()
    {
        tSectionSweepFlow.CFlowUnloadSet();

        bool applied = await tSectionSweepGate.CSectionSweepRun(() => Task.CompletedTask, [], [], [], () => false);

        Assert.False(applied);
        Assert.True(tSectionSweepGate.CSectionEditable);
        Assert.Empty(tSectionSweepPort.TFlowSection.TSectionCalls);
    }
}
