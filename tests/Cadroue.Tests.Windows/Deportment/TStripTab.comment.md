# TStripTab.cs
Hash: `e12ca09a7cad7fe8`

## `public sealed class TStripTab`

Checks the strip deportment and its tab records without any control.
Each test builds a strip with an identity key resolve and a numbered title format.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void Add_NumbersSameKind_AndSelectsFirst()`

Tabs of one kind are numbered while a lone kind keeps its plain name.
The first added tab becomes the selection and raises one select notice.

## `public void Remove_SelectedTab_SelectsNextAndRenumbers()`

Closing the selected tab moves the selection to a neighbour.
Remaining tabs of that kind renumber, and the last one drops its number.

## `public void NameSet_ResolvesDuplicates_AndResetsToStandard()`

A custom name is trimmed and made unique against other tabs.
Setting the standard name again clears the custom name and reports false.

## `public void Separator_HidesAroundSelectedAndHovered()`

The divider beside a selected or hovered tab is hidden so the highlight stays clean.
Clearing hover for a different tab must not restore the separator.

## `public void Move_ReordersAndClamps()`

An out-of-range target index is clamped to the end.
Moving a tab to the place it already holds reports false.

## `public void Suspend_DefersTitlesAndSelection()`

While updates are suspended nothing is selected and titles stay plain.
Resuming and updating titles applies the deferred numbering.

## `public void KeyResolve_FallsBackToSplit()`

An unknown tab key resolves to Split, both for the resolve and for tab creation.

## `public void Close_RaisesCloseNotice_ThenRemovesTab()`

The close notice fires once per tab, and closing an absent tab is ignored.
Closing all raises a notice for every remaining tab and leaves no selection.

## `public void RelayRead_SkipsSourceAndTabsWithoutList()`

A relay target must have an input list and must not be the source tab.

## `public void PendingSet_RaisesTabChangeOnce()`

Setting the pending flag to its current value raises nothing.
