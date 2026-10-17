using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TRelayGate
{
    private const string TRelayClip = @"C:\in\clip.mp4";

    private readonly TRelayPort tRelayPort = new();
    private readonly CRelay tRelayGate;
    private readonly List<(Guid TRelayTab, CList? TRelayList)> tRelayTabs = [];
    private readonly Dictionary<Guid, string> tRelayTitles = [];
    private readonly List<(Guid, string, Guid)> tRelayArrivals = [];
    private readonly Guid tRelayFirst = Guid.NewGuid();
    private readonly Guid tRelaySecond = Guid.NewGuid();
    private readonly Guid tRelayBare = Guid.NewGuid();
    private readonly Guid tRelayBatch = Guid.NewGuid();

    // Two tabs with their own list, docket 0 and 1, and a third without one.
    public TRelayGate()
    {
        CAtelier atelier = TAtelier.TAtelierCreate(port => port with
        {
            LAtelierRelay = tRelayPort,
            LAtelierList = name => new TListPort().TListCreate(name),
        });
        tRelayGate = atelier.CAtelierRelay;
        CList first = atelier.CAtelierListCreate();
        CList second = atelier.CAtelierListCreate();
        tRelayPort.TRelayDockets.AddRange([first.CListDocket, second.CListDocket]);
        tRelayTabs.AddRange([(tRelayFirst, first), (tRelaySecond, second), (tRelayBare, null)]);
        tRelayTitles[tRelayFirst] = "One";
        tRelayTitles[tRelaySecond] = "Two";
        tRelayTitles[tRelayBare] = "Bare";
    }

    [Fact]
    public void RelayAttach_HandsTheSeamsToThePort()
    {
        Assert.False(tRelayPort.TRelayAttached);

        TRelayAttach();

        Assert.True(tRelayPort.TRelayAttached);
    }

    [Fact]
    public void DeliveredAdd_BeforeAttach_WarnsWithTheLeaf()
    {
        Assert.False(tRelayGate.CRelayDeliveredAdd(tRelayFirst, TRelayClip, tRelayBatch));

        Assert.Equal(["warning Relay skipped 'clip.mp4': the destination tab is gone"], tRelayPort.TRelayCalls);
    }

    [Fact]
    public void DeliveredAdd_UnknownTabOrNoList_WarnsGone()
    {
        TRelayAttach();

        Assert.False(tRelayGate.CRelayDeliveredAdd(Guid.NewGuid(), TRelayClip, tRelayBatch));
        Assert.False(tRelayGate.CRelayDeliveredAdd(tRelayBare, TRelayClip, tRelayBatch));

        Assert.Equal(
            [
                $"warning Relay skipped '{TRelayClip}.name': the destination tab is gone",
                $"warning Relay skipped '{TRelayClip}.name': the destination tab is gone"
            ],
            tRelayPort.TRelayCalls);
    }

    [Fact]
    public void DeliveredAdd_Added_LandsOpenInItsTab()
    {
        TRelayAttach();

        Assert.True(tRelayGate.CRelayDeliveredAdd(tRelayFirst, TRelayClip, tRelayBatch));

        Assert.Equal(
            [$"deliver 0 {TRelayClip} open", $"info Relay added '{TRelayClip}.name' to tab 'One'"],
            tRelayPort.TRelayCalls);
    }

    [Fact]
    public void DeliveredCommit_Listed_LandsLockedAndTakesOver()
    {
        TRelayAttach();
        tRelayPort.TRelayListed = true;

        Assert.True(tRelayGate.CRelayDeliveredCommit(tRelaySecond, TRelayClip, tRelayBatch));

        Assert.Equal(
            [
                $"deliver 1 {TRelayClip} locked",
                $"info Relay took over '{TRelayClip}.name' already listed in tab 'Two'"
            ],
            tRelayPort.TRelayCalls);
    }

    [Fact]
    public void DeliveredAdd_NothingScanned_Rejected()
    {
        TRelayAttach();
        tRelayPort.TRelayScanned = 0;

        Assert.False(tRelayGate.CRelayDeliveredAdd(tRelayFirst, TRelayClip, tRelayBatch));

        Assert.Equal(
            $"warning Relay skipped '{TRelayClip}.name': the destination tab rejected the output",
            tRelayPort.TRelayCalls[^1]);
    }

    [Fact]
    public void DeliveredAdd_NoneAdded_StillHeld()
    {
        TRelayAttach();
        tRelayPort.TRelayAdded = 0;

        Assert.False(tRelayGate.CRelayDeliveredAdd(tRelayFirst, TRelayClip, tRelayBatch));

        Assert.Equal(
            $"warning Relay skipped '{TRelayClip}.name': tab 'One' still holds it for other work",
            tRelayPort.TRelayCalls[^1]);
    }

    [Fact]
    public void DeliverRun_Added_TheTabTakesTheArrival()
    {
        TRelayAttach();

        Assert.True(tRelayGate.CRelayDeliverRun(tRelayFirst, TRelayClip, tRelayBatch));

        Assert.Equal([(tRelayFirst, TRelayClip, tRelayBatch)], tRelayArrivals);
    }

    [Fact]
    public void DeliverRun_Refused_NoArrival()
    {
        TRelayAttach();
        tRelayPort.TRelayScanned = 0;

        Assert.False(tRelayGate.CRelayDeliverRun(tRelayFirst, TRelayClip, tRelayBatch));

        Assert.Empty(tRelayArrivals);
    }

    [Fact]
    public void DeliveredRemove_RelayKeepsSources_LeavesTheTab()
    {
        TRelayAttach();
        tRelayPort.TRelayEmpty = false;

        tRelayGate.CRelayDeliveredRemove(tRelayFirst, ["a"], false);

        Assert.Equal(["empty"], tRelayPort.TRelayCalls);
    }

    [Fact]
    public void DeliveredRemove_Forced_DrainsTheSourceTab()
    {
        TRelayAttach();
        tRelayPort.TRelayEmpty = false;

        tRelayGate.CRelayDeliveredRemove(tRelayFirst, ["a", "b"], true);
        tRelayGate.CRelayDeliveredRemove(Guid.Empty, ["c"], true);

        Assert.Equal(
            ["remove 0 a,b", "info Relay removed 2 source file(s) from tab 'One' after delivery"],
            tRelayPort.TRelayCalls);
    }

    [Fact]
    public void BatchRemove_EveryListedTab_UnlocksOrphansAndRemovesDelivered()
    {
        TRelayAttach();
        tRelayPort.TRelayDeparted = ([("a", tRelayBatch)], ["d"]);
        tRelayPort.TRelayReleased = 1;

        tRelayGate.CRelayBatchRemove([tRelayBatch]);

        Assert.Equal(
            [
                "departed 0 1", "release 0 a",
                "info Relay unlocked 1 source file(s) in tab 'One' after their work left the worklist unfinished",
                "remove 0 d",
                "info Relay removed 1 delivered file(s) from tab 'One' after their batch left the worklist",
                "departed 1 1", "release 1 a",
                "info Relay unlocked 1 source file(s) in tab 'Two' after their work left the worklist unfinished",
                "remove 1 d",
                "info Relay removed 1 delivered file(s) from tab 'Two' after their batch left the worklist"
            ],
            tRelayPort.TRelayCalls);
    }

    [Fact]
    public void BatchRemove_NothingDeparted_TracesNothing()
    {
        TRelayAttach();

        tRelayGate.CRelayBatchRemove([tRelayBatch]);

        Assert.Equal(["departed 0 1", "departed 1 1"], tRelayPort.TRelayCalls);
    }

    [Fact]
    public void SourceRelease_OwnedInTheirTab_OrphansInEveryTab()
    {
        TRelayAttach();

        tRelayGate.CRelaySourceRelease(
            [("a", tRelayBatch, tRelayFirst), ("a", tRelayBatch, tRelayFirst), ("o", tRelayBatch, Guid.NewGuid())]);

        Assert.Equal(["release 0 a", "release 0 o", "release 1 o"], tRelayPort.TRelayCalls);
    }

    [Fact]
    public void SourceRelease_TabWithoutList_DropsItsReleases()
    {
        TRelayAttach();

        tRelayGate.CRelaySourceRelease([("a", tRelayBatch, tRelayBare)]);

        Assert.Empty(tRelayPort.TRelayCalls);
    }

    [Fact]
    public void SourceClaim_GroupsByTab_EveryClaimRuns_AllMustHold()
    {
        TRelayAttach();
        tRelayPort.TRelayClaimed = docket => docket != tRelayPort.TRelayDockets[0];

        bool claimed = tRelayGate.CRelaySourceClaim(
        [
            (tRelayFirst, [("a", tRelayBatch), ("m", tRelayBatch)]),
            (tRelayFirst, [("a", tRelayBatch)]),
            (tRelaySecond, [("c", tRelayBatch)]),
            (tRelayBare, [("x", tRelayBatch)]),
            (Guid.NewGuid(), [("y", tRelayBatch)])
        ]);

        Assert.False(claimed);
        Assert.Equal(["claim 0 a,m", "claim 1 c"], tRelayPort.TRelayCalls);
    }

    [Fact]
    public void SourceClaim_BeforeAttach_ClaimsNothing()
    {
        Assert.True(tRelayGate.CRelaySourceClaim([(tRelayFirst, [("a", tRelayBatch)])]));
        Assert.Empty(tRelayPort.TRelayCalls);
    }

    [Fact]
    public void AtelierCreate_NoRelayPort_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => TAtelier.TAtelierCreate(port => port with { LAtelierRelay = null! }));

    private void TRelayAttach() =>
        tRelayGate.CRelayAttach(
            () => tRelayTabs.ToArray(),
            tab => tRelayTitles[tab],
            (tab, path, batch) => tRelayArrivals.Add((tab, path, batch)));
}
