using Cadroue.ShellEngine;

namespace Cadroue.Tests;

public sealed class TRelay : IDisposable
{
    private readonly HashSet<Guid> tRelayTabs = new();

    public int FinishSlot => LCartographer.LCartographerFinishSlot;

    public Guid FinishDestination => LCartographer.LCartographerFinishTarget;

    public void SetDestination(Guid source, Guid destination)
    {
        tRelayTabs.Add(source);
        tRelayTabs.Add(destination);
        LCartographer.LCartographerTargetSet(source, destination);
    }

    public void SetFinishDestination(Guid source)
    {
        tRelayTabs.Add(source);
        LCartographer.LCartographerTargetSet(source, LCartographer.LCartographerFinishTarget);
    }

    public IReadOnlyList<int> ResolveSlots(IReadOnlyList<Guid> tabs) =>
        LCartographer.LCartographerSlotResolve(tabs);

    public IReadOnlyList<(Guid Source, Guid Destination)> ResolveAssignments(
        IReadOnlyList<Guid> tabs, IReadOnlyList<int> slots) =>
        LCartographer.LCartographerAssignmentResolve(tabs, slots);

    public bool IsFinishDestination(Guid destination) => destination == FinishDestination;

    public void Dispose()
    {
        foreach (Guid tRelayTab in tRelayTabs)
        {
            LCartographer.LCartographerTabRemove(tRelayTab);
        }
    }
}
