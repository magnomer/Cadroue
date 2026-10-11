# PSOptionsSystemWorkspace.cs
Hash: `054eaa48bc05d64a`

## `private UIElement PSSystemRecordBuild()`

Builds the file record plate with its location toggle, notices and clear button.
The location toggle writes `LPreferenceRecordWorkspace` on the draft at once.
The notice can then name the folder records would use.
`LSOptions` names the notice key and the folder, and this page formats them.
The notice starts empty because the first update fills it straight away.

## `private void PSSystemRecordClear()`

Deletes every file record in the record folder after the user confirms.
A missing folder is reported instead of asking, since nothing would be deleted.

## `private void PSSystemMaintenanceUpdate()`

The workspace clear buttons act on the applied depot root only.
While the box holds an unapplied folder they are disabled and a notice says why.

## `private void PSSystemDoneClear()`

Removes finished, failed, cancelled and scheduled work after the user confirms.
The depot index is rebuilt and compacted so it no longer lists the removed work.

## `private void PSWorkspaceClear()`

Refuses while a runner works in the workspace, because resetting would pull files from under it.
Otherwise it resets the workspace after confirmation and rebuilds the index.
