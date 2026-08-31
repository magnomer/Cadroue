using Xunit;

namespace Cadroue.Tests;

public sealed class TRelayTarget
{
    [Fact]
    public void FinishDestination_EmitsFinishSlotAndRoundTrips()
    {
        using var relay = new TRelay();
        Guid a = Guid.NewGuid();
        relay.TRelayFinishSet(a);

        var tabs = new[] { a };
        IReadOnlyList<int> slots = relay.TRelaySlotResolve(tabs);
        Assert.Equal(new[] { relay.TRelayFinishSlot }, slots);

        IReadOnlyList<(Guid Source, Guid Destination)> assignments = relay.TRelayAssignResolve(tabs, slots);
        (Guid Source, Guid Destination) assignment = Assert.Single(assignments);
        Assert.Equal(a, assignment.Source);
        Assert.True(relay.TRelayFinishCheck(assignment.Destination));
    }

    [Fact]
    public void TwoTabPair_ResolvesToDestinationSlotAndBack()
    {
        using var relay = new TRelay();
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        relay.TRelayDestinationSet(a, b);

        var tabs = new[] { a, b };
        IReadOnlyList<int> slots = relay.TRelaySlotResolve(tabs);
        Assert.Equal(new[] { 1, -1 }, slots);

        IReadOnlyList<(Guid Source, Guid Destination)> assignments = relay.TRelayAssignResolve(tabs, slots);
        Assert.Equal(new[] { (a, b) }, assignments);
    }

    [Fact]
    public void SelfAndOutOfRangeSlots_AreSkipped()
    {
        using var relay = new TRelay();
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        var tabs = new[] { a, b };
        var slots = new[] { 0, 9 };

        IReadOnlyList<(Guid Source, Guid Destination)> assignments = relay.TRelayAssignResolve(tabs, slots);

        Assert.Empty(assignments);
    }

    [Fact]
    public void UnresolvedDestination_YieldsMinusOne()
    {
        using var relay = new TRelay();
        Guid a = Guid.NewGuid();
        var tabs = new[] { a };

        IReadOnlyList<int> slots = relay.TRelaySlotResolve(tabs);

        Assert.Equal(new[] { -1 }, slots);
    }
}
