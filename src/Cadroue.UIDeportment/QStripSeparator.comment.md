# QStripSeparator.cs
Hash: `ff88be9851f006c9`

## `public sealed class QStripSeparator`

Owns the strip's hover, orientation and separator state apart from the tab list.
It shares the owner's tab collection, so the separator math reads the live order.

## `public void LStripVerticalSet(bool lVertical)`

Switches the strip orientation and drops any hover, because hover positions no longer apply.

## `public void LStripHoverSet(LStripTab lTab)`

Marks a tab as hovered and recomputes which separators show.

## `public void LStripHoverClear(LStripTab? lTab)`

Clears the hover when it matches the tab, or any hover when the tab is null.

## `public void LStripHoverReset(LStripTab? lTab)`

Forgets the hover without redrawing separators, for a tab leaving or the list emptying.
The owner calls it before removal, so no separator update runs on a stale index.

## `public void LStripSeparatorUpdate()`

Hides a separator next to the selected or hovered tab and after the last tab.
Only tabs whose flag changes are raised, so unchanged tabs send no notice.
