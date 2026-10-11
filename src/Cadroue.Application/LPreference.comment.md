# LPreference.cs
Hash: `516468feeaebee2a`

## `public static LPreferenceState LPreferenceStateCurrent`

Every replacement of the live state reapplies the child-process standing levels, so custody never lags the preference.

## `private static LPreferenceState? lPreferenceBaseline;`

The first deferred change keeps the state from before it.
The deferred save then traces the whole difference once, not each debounced step.

## `public static void LPreferenceLoad()`

The load seam returns the stored state raw, or null when the file is missing or unreadable.
The owner applies the defaults and normalize here, so the store does input and output only.

## `public static bool LPreferenceStateSet(LPreferenceState lPreferenceState)`

A refused workspace move or a failed save puts the previous state back, so memory never outruns storage.
The state is normalized again just before the save seam, since that seam only writes.

## `private static void LPreferenceStateRestore(LPreferenceState lPreferencePrevious)`

The depot callback runs again, so the workspace follows the restored folder back.

## `public static void LPreferenceSaveCommit()`

A deferred save normalizes the current state first, since the save seam only writes.
A failed deferred save keeps the change in memory and leaves the baseline for the next attempt.
