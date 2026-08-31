using Cadroue.ShellEngine;

namespace Cadroue.Tests;

public sealed class TRelay : IDisposable
{
    private readonly HashSet<Guid> tRelayTabs = new();

    public int TRelayFinishSlot => LCartographer.LCartographerFinishSlot;

    public Guid TRelayFinishDestination => LCartographer.LCartographerFinishTarget;

    public void TRelayDestinationSet(Guid source, Guid destination)
    {
        tRelayTabs.Add(source);
        tRelayTabs.Add(destination);
        LCartographer.LCartographerTargetSet(source, destination);
    }

    public void TRelayFinishSet(Guid source)
    {
        tRelayTabs.Add(source);
        LCartographer.LCartographerTargetSet(source, LCartographer.LCartographerFinishTarget);
    }

    public IReadOnlyList<int> TRelaySlotResolve(IReadOnlyList<Guid> tabs) =>
        LCartographer.LCartographerSlotResolve(tabs);

    public IReadOnlyList<(Guid Source, Guid Destination)> TRelayAssignResolve(
        IReadOnlyList<Guid> tabs, IReadOnlyList<int> slots) =>
        LCartographer.LCartographerAssignmentResolve(tabs, slots);

    public bool TRelayFinishCheck(Guid destination) => destination == TRelayFinishDestination;

    public void Dispose()
    {
        foreach (Guid tRelayTab in tRelayTabs)
        {
            LCartographer.LCartographerTabRemove(tRelayTab);
        }
    }
}
