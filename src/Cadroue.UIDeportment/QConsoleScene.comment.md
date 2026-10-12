# QConsoleScene.cs
Hash: `fdca863aab387132`

## `public sealed class QConsoleScene`

Owns the scene cluster of the console card: the selected name, the reload name and every scene command.
The veneer only draws what the notices say and forwards gestures.
No control reaches this class.

## `public string? QConsoleReloadRead()`

Reading the reload name also clears it.
So a pending reload is consumed exactly once, however many handlers ask.

## `public void QConsoleSceneStart()`

Adopts the scene that was active when the program last ran.
It does not load that scene, it only marks it as the selected one.

## `public void QConsoleSceneUpdate()`

The stored-name argument is null when the selected scene no longer exists on disk.
So the veneer can tell a deleted scene from an unsaved one.

## `public void QConsoleTickHandle(bool lFocusWithin)`

The dirty mark is refreshed only while focus is outside the cluster.
Typing in the name box never has the mark flicker underneath it.

## `public bool QConsoleDirtyCheck()`

A scene is dirty when it is stored and the live window differs from it.
An unnamed or unsaved scene is never dirty.

## `public void QConsoleSelectHandle(object? lSelected, bool lDropOpen)`

While the dropdown is open a pick is only remembered.
The load waits for the close, so browsing the list never replaces the tabs.

## `public void QConsoleCloseHandle()`

Loads the pick remembered while the dropdown was open, else redraws the current selection.

## `public void QConsoleSceneLoad(string lSceneName)`

Loading closes every tab, so it always asks first.
The scene read before the question is the one applied after the answer.

## `private void QConsoleSceneRun(bool lAnswer, string lSceneName, LSceneDocument lScene)`

A refused close of the old tabs leaves the selection untouched.
The selection moves only when the scene was actually applied.

## `public bool QConsoleDeleteHandle(string? lSceneName)`

Returns whether a name was given, not whether the delete succeeded.
A failed delete leaves the catalogue and the selection as they were.

## `public void QConsoleSceneSave(string? lText)`

An empty name raises a notice and saves nothing.
After a save the saved scene becomes the selected one, so it reads clean.

## `public void QConsolePressHandle(bool lDropOpen, bool lFocusWithin, bool lInsideCombo)`

A press outside the combo while the cluster holds focus clears that focus.
An open dropdown keeps focus, so its own click is never swallowed.

## `private LSceneDocument? QConsoleExportResolve(string? lText)`

A stored scene of the typed name wins.
Otherwise the live window is captured under that name, or under the default name when the box is empty.

## `public void QConsoleImportCommit(bool? lConfirmed, string lPath)`

An imported scene never overwrites a stored one.
Its name is made unique first.

## `public static string QConsoleNameCreate(string lBaseName, IReadOnlyList<string> lNames)`

The comparison ignores case, like the scene files on disk.
A taken name gets the first free number from 2.

## `private void QConsoleErrorRaise(string lMessageKey, string lDetail, string lTitleKey)`

An empty detail selects the plain message without a placeholder.
