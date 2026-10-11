# TWindowDrop.cs
Hash: `9e2823ab0509bcdf`

## `public sealed class TWindowDrop`

Checks the window deportment's drop decisions, tab notices and scene read, with no control.
Drag data is a plain record of paths, a group flag, the source type, copy permission and a data object.
Tabs are added to a real strip so the window sees a selection.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void Over_WithoutTab_RefusesButHandles()`

With no tab selected the drag shows no drop effect.
The event is still marked handled so nothing else reacts.

## `public void Over_OnGroup_LeavesTheEventAlone()`

A drag that started in the group panel is that panel's own reorder.
The window neither sets an effect nor marks the event handled.

## `public void Drop_OnListTab_HandsMediaPathsToTheList()`

A tab with a list receives every dropped path, and the list filters them later.
The viewer receives nothing.

## `public void Drop_OnListTab_WithoutMediaOrCopy_Refuses()`

A drop with no media among the paths is refused.
A drop that does not permit copying is refused too.
The list is not notified in either case.

## `public void Drop_OnViewerTab_OpensTheFirstExistingFile()`

A tab with a viewer and no list opens only the first dropped path that exists on disk.
When no dropped path exists the drop is refused and nothing opens.

## `public void Width_FloorsAtNineHundredPlusReserved()`

The minimum width is the larger of 900 and the required width, plus the reserved width.
A current width above that minimum is kept, and a narrower one is raised to it.

## `public void TabSet_RaisesDetachThenAttach()`

Switching tabs detaches the previous tab before attaching the new one.
Clearing the tab only detaches.

## `public void SceneRead_ListsTabsInOrderWithSelection()`

The scene record keeps the strip order and the selected tab index.
It carries one export and one layout per tab.
