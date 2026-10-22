using Cadroue.Conduct;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TSweepGate
{
    [Fact]
    public void RunRequest_IdleRuns_RunningStops()
    {
        CInspector inspector = TAtelier.TAtelierCreate().CAtelierInspectorCreate();
        CSweep sweep = inspector.CInspectorSweep;
        int runs = 0;
        int stops = 0;
        sweep.CSweepRunApplied += () => runs++;
        sweep.CSweepStopApplied += () => stops++;

        sweep.CSweepRunHandle();
        sweep.CSweepRunningSet(true);
        sweep.CSweepRunHandle();

        Assert.Same(sweep, inspector.CInspectorSweep);
        Assert.Equal(1, runs);
        Assert.Equal(1, stops);
    }

    [Fact]
    public void RepeatedState_IsSilent()
    {
        CSweep sweep = new();
        int running = 0;
        int persistent = 0;
        sweep.CSweepRunningChanged += _ => running++;
        sweep.CSweepPersistentChanged += _ => persistent++;

        sweep.CSweepRunningSet(true);
        sweep.CSweepRunningSet(true);
        sweep.CSweepPersistentSet(true);
        sweep.CSweepPersistentSet(true);

        Assert.True(sweep.CSweepRunning);
        Assert.True(sweep.CSweepPersistent);
        Assert.Equal(1, running);
        Assert.Equal(1, persistent);
    }
}
