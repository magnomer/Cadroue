using Cadroue.ShellEngine;

namespace Cadroue.Tests.Interface;

public sealed class TRelay : IDisposable
{
    private readonly HashSet<Guid> tRelayTabs = new();

    public int TRelayFinishSlot => LCartographerSlot.LCartographerFinishSlot;

    public Guid TRelayFinishDestination => LCartographer.LCartographerFinishTarget;

    public void TRelayDestinationSet(Guid source, Guid destination)
    {
        tRelayTabs.Add(source);
        tRelayTabs.Add(destination);
        LCartographerRoute.LCartographerTargetSet(source, destination);
    }

    public void TRelayFinishSet(Guid source)
    {
        tRelayTabs.Add(source);
        LCartographerRoute.LCartographerTargetSet(source, LCartographer.LCartographerFinishTarget);
    }

    public IReadOnlyList<int> TRelaySlotResolve(IReadOnlyList<Guid> tabs) =>
        LCartographerSlot.LCartographerSlotResolve(tabs);

    public IReadOnlyList<LCartographerAssignment> TRelayAssignResolve(
        IReadOnlyList<Guid> tabs, IReadOnlyList<int> slots) =>
        LCartographerSlot.LCartographerAssignmentResolve(tabs, slots);

    public bool TRelayFinishCheck(Guid destination) => destination == TRelayFinishDestination;

    public void Dispose()
    {
        foreach (Guid tRelayTab in tRelayTabs)
        {
            LCartographerRoute.LCartographerTabRemove(tRelayTab);
        }
    }
}
