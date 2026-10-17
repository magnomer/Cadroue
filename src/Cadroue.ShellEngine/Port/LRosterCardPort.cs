using Cadroue.Core;

namespace Cadroue.ShellEngine;

public interface LRosterCardPort
{
    // The schedule's work items with these ids, in id order; an id the schedule no longer holds is left out.
    // Ownership is told against the asking roster's runner.
    IReadOnlyList<LRosterRow> LRosterCardRead(IReadOnlyList<Guid> lIds, Guid lRunner);
}

// One work item as a roster row reads it. The token names its state; claimed, owned and local tell who runs it.
public sealed record LRosterRow(
    Guid LRosterRowId,
    bool LRosterRowHigh,
    TimeSpan LRosterRowLength,
    bool LRosterRowRunning,
    double LRosterRowProgress,
    string LRosterRowToken,
    bool LRosterRowClaimed,
    bool LRosterRowOwned,
    bool LRosterRowLocal)
{
    public static LRosterRow LRosterRowCreate(LWorkItem lWorkItem, bool lOwned) =>
        new(
            lWorkItem.LWorkId,
            lWorkItem.LWorkPriority == LWorkPriority.LWorkPriorityHigh,
            lWorkItem.LWorkLength,
            lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning,
            lWorkItem.LWorkProgress,
            lWorkItem.LWorkStateCurrent switch
            {
                LWorkState.LWorkStatePending => "Pending",
                LWorkState.LWorkStateRunning => "Running",
                LWorkState.LWorkStateDone => "Done",
                LWorkState.LWorkStateFailed => "Failed",
                LWorkState.LWorkStateUnresolved => "Unresolved",
                LWorkState.LWorkStatePartial => "Partial",
                LWorkState.LWorkStateBlocked => "Blocked",
                LWorkState.LWorkStateCancelled => "Cancelled",
                _ => "Pending"
            },
            lWorkItem.LWorkOwnerRunner != Guid.Empty,
            lOwned,
            lWorkItem.LWorkOwnerProcess == Environment.ProcessId);
}
