namespace Cadroue.Core;

/// <summary>Lifecycle of a scheduled work item.</summary>
public enum LWorkState
{
    LWorkStatePending,
    LWorkStateRunning,
    LWorkStateDone,
    LWorkStateFailed,
    LWorkStateCancelled
}
