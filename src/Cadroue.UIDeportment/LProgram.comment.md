# LProgram.cs
Hash: `0720b171a4ff65e5`


## `public sealed class LProgram`

Owns the startup and shutdown sequence of the program, free of any window.
The host supplies only the dispatcher hops, resource readers, placement keys, debounce restart and Flyleaf hooks.

## `public static LScheduleContract LScheduleCurrent { get; } = new LSchedule();`

The one schedule every station, messenger, and cartographer of this process shares.

## `public void LProgramStart(`

The order matters, because later steps read what earlier ones loaded.
The host fills the store and engine seams beforehand, so the first preference load already reaches its store.
Localization seams are set before the catalog loads, and preferences load before language and depot use them.
The relay startup arguments are read first, before any state is touched.
Stale claims are recovered after the depot is applied, since they live in its index.
The relay channel starts last, so no transfer arrives before the program is ready.

## `public void LProgramClose(int lExitCode)`

Pending preferences are committed before the relay stops.
The trace writer is persisted last so it records the earlier shutdown steps.

## `public void LProgramDebounceTick() => LPreference.LPreferenceSaveCommit();`

The veneer's debounce timer ends here, committing the preference save it was waiting on.

## `public static void LProgramDefer(Action lAction)`

Runs the action at once when no defer seam is attached yet.
This keeps early callers working before startup completes.

## `private static void LProgramStationAttach(Action<Action> lDispatch)`

Fills the seams of the station, messenger, and cartographer with the shared schedule.
The sources are delegates, so each read sees the current renderer program and preference.

## `public static bool LProgramLanguageNormalize()`

Corrects a stored language the catalog could not honor to the one actually loaded.
Returns true only when the preference was changed.

## `private bool LProgramDepotHandle()`

Called when the workspace folder preference changes.
The sidecar folder is reapplied even when the depot move failed.

## `public static void LProgramSidecarApply() =>`

The sidecar folder follows the record-workspace preference.

## `public string? LProgramDepotRoot => lDepotRootApplied;`

Null until a depot root has been applied successfully.

## `public bool LProgramDepotApply()`

Returns true when the preferred root is already applied or was applied now.
A move failure keeps the previous root.
The first application sets the root directly, and later ones move the folder.
Any exception clears the applied root and is recorded, never thrown.

## `private static void LProgramStaleClaim()`

Recovers jobs a previous run left claimed, and reports only a non-zero count.
A failure is recorded and never blocks startup.
