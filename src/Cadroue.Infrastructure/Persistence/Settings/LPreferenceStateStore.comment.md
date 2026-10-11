# LPreferenceStateStore.cs
Hash: `e4c08efac2154a2b`

## `public static class LPreferenceStateStore`

Owns the preference file load and save through the vault.
The host assigns its methods to the Application preference seams, since Application cannot name it.
The Flyleaf module start also loads the file directly, before any preference owner exists.

## `public static LPreferenceState? LPreferenceStateLoad()`

Returns the stored state as read, or null for a missing or unreadable file.
The outcome is kept for the next save.
Defaults and normalizing belong to the preference owner, which applies them after its seam returns.

## `public static bool LPreferenceStateSave(LPreferenceState lPreferenceState)`

A file the last load could not read is never overwritten, so its settings are not lost.
The state arrives already normalized by the preference owner, so the store only writes it.

## `public static string LPreferencePathRead() => LPreferencePathCreate();`

The startup placement import reads the preference file directly to carry subwindow placements stored there into their own file.
