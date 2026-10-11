# PStrip.cs
Hash: `dd1974289fca97f3`

## `public sealed class PStrip`

Pairs each tab of the deportment strip with its workspace.
It keeps only that list, every other decision belongs to `LStrip`.

## `public PStrip()`

Builds the deportment strip and attaches the relay seams last.
So the relay engine sees a strip whose notices are already wired.

## `public PWorkspace? PStripWorkspaceRead(LStripTab? lStripTab)`

Matches by reference, not by title or key.
Returns null for a tab that has no workspace, such as a null or already closed one.

## `public LStripTab PStripAdd(`

Creates the tab and its workspace before the tab joins the strip.
So the strip's add notice finds a workspace for the tab.

## `private void PStripTabHandle(LStripTab lStripTab) =>`

A tab with a relay pending is disabled for the length of the relay.
This stops edits to a tab that is leaving.

## `private void PStripStaleRemove() =>`

Drops the workspaces whose tab is no longer in the strip.

## `private void PStripCloseHandle(LStripTab lStripTab) => PStripWorkspaceRead(lStripTab)?.PWorkspaceClose();`

Closes the workspace on the closed notice, while the tab is still in the strip.
