# LSOptions.cs
Hash: `c6c5ac635dd62653`

## `private static readonly IReadOnlyDictionary<bool, string> lsOptionsRecordKeys = new Dictionary<bool, string>`

Names the record location notice for each draft setting, keyed by whether records go to the workspace.

## `public LSOptions(LPreferenceState lDraft, bool lMpvEnabled)`

Takes the draft and the mpv availability from the caller, so a test needs no live preference or library probe.
The window edits this draft, and nothing reaches the live preference before apply.

## `public string LSOptionsEngineRead() =>`

Shows Flyleaf whenever mpv is unusable, even if the draft still asks for mpv.

## `public async Task LSOptionsMpvUpdate()`

Rechecks whether mpv can run and raises the result.
A failed check is traced and counts as unusable, so the window never sees an exception.

## `public static bool LSOptionsAppliedCheck(string lFolder) =>`

Answers whether a folder resolves to the workspace root already in use.

## `public static string LSOptionsWorkspaceResolve(string lFolder, out bool lOccupied)`

Refuses a new workspace folder that already holds another workspace.
The refusal returns the current folder, so the field falls back to it.
Choosing the current root again is never an occupied refusal.

## `public string LSOptionsRecordResolve() => lsOptionsRecordKeys[LSOptionsRecordCheck()];`

Returns the localization key of the record location notice for the current draft.
The Veneer formats it, as it does for the mpv, Flyleaf and FFmpeg keys.

## `public static string LSOptionsSidecarRead() => LSidecarStore.LSidecarFolderResolve();`

Returns the workspace record folder `LSidecarStore` resolves, even before the setting applies.
The beside notice has no placeholder, so passing the folder leaves its text unchanged.

## `public bool LSOptionsApply()`

Saves a copy of the draft as the live preference and switches the preview engine.
The result reports whether the save succeeded.
The engine switch happens either way.

## `public bool LSOptionsRestartCheck() =>`

Answers whether the saved language differs from the one loaded at startup.
The catalog loads once, so only a restart shows the new language.
