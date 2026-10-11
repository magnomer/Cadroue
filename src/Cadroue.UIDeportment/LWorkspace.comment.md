# LWorkspace.cs
Hash: `0d7088801b1576d8`

## `public sealed class LWorkspace`

Owns one tab's key facts, preset, history, media notices, relay and clear decision.
It constructs its key facts sibling and hands the flow and viewer pair to its wiring sibling.

## `public QWorkspaceKey QWorkspaceKey { get; }`

The key facts sibling: source present, audio only and section visible.

## `public void LWorkspaceAttach(LDocket? lDocket, LSegment? lSegment, LFlow? lFlow, LViewer? lViewer, LStation? lStation, Func<LSceneTabEntry> lLayoutSource)`

Takes the tab's parts, subscribes their notices and seeds the history with the first snapshot.

## `public void LWorkspaceClose()`

Drops every subscription the attach made, forgets a pending relay and closes the preset owner.

## `public void LWorkspaceHistoryReset()`

Starts a fresh history when the flow loads other media, so undo never crosses sources.

## `public bool LWorkspaceUndo()`

Refuses while the flow's sections are not editable.
The history is suspended during the apply, so the restored state records no new entry.

## `public void LWorkspaceRemovedHandle(IReadOnlyList<string> lRemovedPaths)`

Cancels a pending load and closes the loaded media when the list drops their path.

## `public bool LWorkspaceMediaClear(IReadOnlySet<Guid> lCohorts)`

Closes or cancels media that no protected cohort needs, then removes stale list paths.

## `public LRelay LWorkspaceRelayCreate(string lCustomName, double lDropLeft, double lDropTop)`

Packs the tab's preset, layout, paths, playback and sections into a relay payload.

## `public void LWorkspaceRelayApply(LRelay lRelay)`

Starts restoring a relayed tab, adding its paths first when the tab has a list.

## `public void LWorkspaceSourceRun()`

Opens the relayed source once, selecting it in the list when the list already holds it.

## `public void LWorkspaceRelayRestore(LRelay lRelay, TimeSpan lDuration)`

Restores sections, range, volume and position once the relayed source has loaded.
The media notice defers it until the load, because sections are bound to the media duration.
