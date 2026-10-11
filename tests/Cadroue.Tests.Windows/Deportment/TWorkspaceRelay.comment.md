# TWorkspaceRelay.cs
Hash: `cefe61b484bae48d`

## `public sealed class TWorkspaceRelay`

Checks how the workspace deportment builds and applies a relay payload without any control.
A relay moves a tab between instances, so its create and restore sides must agree.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void Create_CarriesKeyPresetAndDocketPaths()`

The payload names the layout key, the custom preset and the drop point.
It lists the docket paths and starts with no source and no sections.

## `public void Apply_WithPaths_DefersToPathsAdd_ThenSelectsListedSource()`

The source cannot be selected before the list holds it.
The apply adds the paths first and marks the relay pending.
The source is selected on the next source run, not opened directly.

## `public void Apply_WithoutList_OpensSourceDirectly()`

A tab without a list has nothing to select from, so the source is opened.

## `public void Restore_RaisesVolumeSeekAndRange_OnlyWhenCarried()`

A payload that carries nothing raises nothing.
Without a flow the range is not applied even when carried.

## `public void Restore_WithFlow_AppliesRange()`

With a flow attached the carried working range is raised as an origin and limit pair.
