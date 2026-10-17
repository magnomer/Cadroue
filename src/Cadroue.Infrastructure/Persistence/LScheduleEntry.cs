using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed record LScheduleEntry(LDepotFolder LScheduleEntryFolder, LWorkDocument LScheduleEntryWork)
{
    internal static LWorkState LScheduleStateRead(LDepotFolder lDepotFolder, LWorkState lScheduleFileState) =>
        lDepotFolder switch
        {
            LDepotFolder.LDepotFolderRunning => LWorkState.LWorkStateRunning,
            LDepotFolder.LDepotFolderDone => LScheduleTerminalRead(lScheduleFileState, LWorkState.LWorkStateDone),
            LDepotFolder.LDepotFolderFailed => LScheduleTerminalRead(lScheduleFileState, LWorkState.LWorkStateFailed),
            LDepotFolder.LDepotFolderCancelled => LWorkState.LWorkStateCancelled,
            _ => lScheduleFileState == LWorkState.LWorkStateRunning ? LWorkState.LWorkStatePending : lScheduleFileState
        };

    private static LWorkState LScheduleTerminalRead(LWorkState lScheduleFileState, LWorkState lScheduleFolderState) =>
        lScheduleFileState is LWorkState.LWorkStatePartial
            or LWorkState.LWorkStateUnresolved
            or LWorkState.LWorkStateBlocked
            ? lScheduleFileState
            : lScheduleFolderState;
}
