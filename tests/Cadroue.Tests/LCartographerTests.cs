using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class LCartographerTests
{
    [Fact]
    public void SlotResolve_FinishTarget_EmitsFinishSlotAndRoundTrips()
    {
        Guid a = Guid.NewGuid();
        LCartographer.LCartographerTargetSet(a, LCartographer.LCartographerFinishTarget);
        try
        {
            var ids = new[] { a };
            IReadOnlyList<int> slots = LCartographer.LCartographerSlotResolve(ids);
            Assert.Equal(new[] { LCartographer.LCartographerFinishSlot }, slots);

            IReadOnlyList<(Guid Source, Guid Target)> assignments =
                LCartographer.LCartographerAssignmentResolve(ids, slots);
            Assert.Equal(new[] { (a, LCartographer.LCartographerFinishTarget) }, assignments);
        }
        finally
        {
            LCartographer.LCartographerTabRemove(a);
        }
    }

    [Fact]
    public void SlotResolve_TwoTabPair_ResolvesToTargetSlotAndBack()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        LCartographer.LCartographerTargetSet(a, b);
        try
        {
            var ids = new[] { a, b };
            IReadOnlyList<int> slots = LCartographer.LCartographerSlotResolve(ids);
            Assert.Equal(new[] { 1, -1 }, slots);

            IReadOnlyList<(Guid Source, Guid Target)> assignments =
                LCartographer.LCartographerAssignmentResolve(ids, slots);
            Assert.Equal(new[] { (a, b) }, assignments);
        }
        finally
        {
            LCartographer.LCartographerTabRemove(a);
            LCartographer.LCartographerTabRemove(b);
        }
    }

    [Fact]
    public void AssignmentResolve_SelfAndOutOfRangeSlots_AreSkipped()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        var ids = new[] { a, b };
        var slots = new[] { 0, 9 };

        IReadOnlyList<(Guid Source, Guid Target)> assignments =
            LCartographer.LCartographerAssignmentResolve(ids, slots);

        Assert.Empty(assignments);
    }

    [Fact]
    public void SlotResolve_UnresolvedTarget_YieldsMinusOne()
    {
        Guid a = Guid.NewGuid();
        var ids = new[] { a };

        IReadOnlyList<int> slots = LCartographer.LCartographerSlotResolve(ids);

        Assert.Equal(new[] { -1 }, slots);
    }
}
