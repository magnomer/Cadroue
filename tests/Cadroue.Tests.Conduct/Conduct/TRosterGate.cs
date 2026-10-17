using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Xunit;

namespace Cadroue.Tests;

public sealed class TRosterGate
{
    private readonly TRosterPort tRosterPort = new();
    private readonly CRoster tRosterGate;
    private int tRosterChanges;

    public TRosterGate()
    {
        CAtelier atelier = TAtelier.TAtelierCreate(port => port with { LAtelierRoster = tRosterPort });
        tRosterGate = atelier.CAtelierRosterCreate();
        tRosterGate.CRosterChanged += () => tRosterChanges++;
    }

    [Fact]
    public void AtelierRosterCreate_EachCall_GivesItsOwnRoster()
    {
        CAtelier atelier = TAtelier.TAtelierCreate(port => port with { LAtelierRoster = tRosterPort });

        Assert.NotSame(atelier.CAtelierRosterCreate(), atelier.CAtelierRosterCreate());
    }

    [Fact]
    public void Atelier_NoRosterPort_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TAtelier.TAtelierCreate(port => port with { LAtelierRoster = null! }));
    }

    [Fact]
    public void RosterShared_Read_AnswersThePortsPreference()
    {
        tRosterPort.TRosterShared = true;
        tRosterPort.TRosterDone = false;

        Assert.True(tRosterGate.CRosterShared);
        Assert.False(tRosterGate.CRosterDone);
    }

    [Fact]
    public void RosterSharedSet_Choices_SaveOnceEachAndAnswerTheSave()
    {
        Assert.True(tRosterGate.CRosterSharedSet(true));
        Assert.False(tRosterGate.CRosterSharedSet(true));
        Assert.True(tRosterGate.CRosterSharedSet(null));

        Assert.Equal(["shared:True", "shared:True", "shared:False"], tRosterPort.TRosterCalls);
        Assert.False(tRosterPort.TRosterShared);
        Assert.Equal(0, tRosterChanges);
    }

    [Fact]
    public void RosterDoneSet_TurnedOn_TellsAChangeOnce()
    {
        Assert.True(tRosterGate.CRosterDoneSet(true));
        Assert.False(tRosterGate.CRosterDoneSet(true));

        Assert.Equal(1, tRosterChanges);
        Assert.True(tRosterPort.TRosterDone);
    }

    [Fact]
    public void RosterDoneSet_TurnedOffOrUnset_SavesWithoutChange()
    {
        tRosterPort.TRosterDone = true;

        Assert.True(tRosterGate.CRosterDoneSet(null));
        Assert.False(tRosterGate.CRosterDoneSet(false));

        Assert.Equal(["done:False", "done:False"], tRosterPort.TRosterCalls);
        Assert.Equal(0, tRosterChanges);
    }

    // The station is a handle the gate hands on unread, so an empty instance stands in for one.
    private static LStation TRosterStationCreate() => (LStation)RuntimeHelpers.GetUninitializedObject(typeof(LStation));

    [Fact]
    public void RosterAttach_NoStation_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => tRosterGate.CRosterAttach(null!));
    }

    [Fact]
    public void RosterMenuRead_Attached_HandsItsStationToThePort()
    {
        LStation station = TRosterStationCreate();
        tRosterGate.CRosterAttach(station);

        tRosterGate.CRosterMenuRead(Guid.NewGuid());

        Assert.Same(station, tRosterPort.TRosterStation);
    }

    [Fact]
    public void RosterMenuRead_NotAttached_OffersNothingWithoutAsking()
    {
        tRosterGate.CRosterMenuRead(Guid.NewGuid());
        tRosterGate.CRosterMenuRun();

        Assert.False(tRosterGate.CRosterMenuRestartable);
        Assert.False(tRosterGate.CRosterMenuCancellable);
        Assert.False(tRosterGate.CRosterMenuRelay);
        Assert.Equal(0, tRosterGate.CRosterMenuCount);
        Assert.Empty(tRosterGate.CRosterMenuPaths);
        Assert.Empty(tRosterPort.TRosterCalls);
    }

    [Fact]
    public void RosterMenuRead_Restart_AnswersThePortAndRunsItsRun()
    {
        Guid clicked = Guid.NewGuid();
        int runs = 0;
        tRosterGate.CRosterAttach(TRosterStationCreate());
        tRosterPort.TRosterMenu = (true, false, 2, [], () => runs++);

        tRosterGate.CRosterMenuRead(clicked);
        tRosterGate.CRosterMenuRun();

        Assert.True(tRosterGate.CRosterMenuRestartable);
        Assert.False(tRosterGate.CRosterMenuCancellable);
        Assert.False(tRosterGate.CRosterMenuRelay);
        Assert.Equal(2, tRosterGate.CRosterMenuCount);
        Assert.Equal(1, runs);
        Assert.Equal([$"menu:{clicked}"], tRosterPort.TRosterCalls);
    }

    [Fact]
    public void RosterMenuRead_Relay_AnswersTheOutputs()
    {
        tRosterGate.CRosterAttach(TRosterStationCreate());
        tRosterPort.TRosterMenu = (false, false, 1, ["out.mp4"], () => { });

        tRosterGate.CRosterMenuRead(Guid.NewGuid());

        Assert.True(tRosterGate.CRosterMenuRelay);
        Assert.Equal(1, tRosterGate.CRosterMenuCount);
        Assert.Equal(["out.mp4"], tRosterGate.CRosterMenuPaths);
    }
}
