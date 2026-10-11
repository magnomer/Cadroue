# TFunnelRule.cs
Hash: `c9025628897e7c8e`

## `public sealed class TFunnelRule`

Checks the funnel deportment's rules without any control.
It covers rule edits, restore from scene records, target resolution against the tab strip and the row drag.

## `public void JoinToggle_FlipsOneCondition_NotifiesOnce()`

Setting a join to the value it already holds is not a change.
Only a real flip raises the rule notice.

## `public void RestoredTarget_StaysPending_UntilTabsResolve()`

A stored target index cannot name a tab before the strip exists.
The rule keeps the index as pending and holds an empty target until tabs resolve.
A catch-all rule never takes a target from the resolve.

## `public void RestoreTwice_ReplacesRules_EmptyRestoreClears()`

Restore replaces the rule list and never appends to it.
Restoring nothing leaves no rule and no selection.

## `public void TargetChoice_ClearsPending_NotifiesRuleOnly()`

Choosing a target ends the pending state.
Only the rule notice fires, so the whole list is not rebuilt for a target pick.

## `public void RemoveSelected_SelectsNeighbour_MoveReorders()`

Removing the selected rule hands the selection to a neighbour.
A move that would leave the rule in place is refused.

## `public void StripAttach_TargetsExcludeSelf_OptionsStartWithNone_IndexFollowsStrip()`

A funnel never offers its own tab as a target.
The picker options start with an empty entry so a rule can have no target.
An unknown or empty target id has no index.

## `public void TargetSelect_ByOptionIndex_StripChangeClearsGoneTarget()`

Option indexes outside the picker are ignored.
Closing the targeted tab clears the target instead of leaving a dangling id.

## `public void LayoutRoundTrip_TargetIndexAndPending_ThroughTab()`

The saved layout stores the target as a strip index.
A restored tab keeps it pending until the strip is attached and resolved.

## `public void Slots_FollowOrderAndSelection_EmptyFlag()`

Slots carry the display position, the evaluation order and the selection of each rule.
The empty flag drops as soon as one rule exists.

## `public void CreateAndDelete_NotifyPerRule_SelectedRemoveUsesSelection()`

The create and delete notices fire once per rule.
Removing with nothing selected does nothing.
A catch-all rule is created without match fields.

## `public void TextResolve_KeepsShownWhenTrimEqual_ReplacesOtherwise()`

The box text is kept as typed when it only differs from the stored value by surrounding blanks.
Any other difference is replaced by the stored value.

## `public void Toggles_FlipCaseAndCollapsed()`

A case toggle touches only the named condition.
The condition table lists four conditions and the first carries no join.

## `public void Drag_ThresholdThenMoveByCenters_ReleaseClears()`

A press alone does not start a drag.
Moving past the threshold starts it and later moves reorder the rules by row centers.
Clearing the drag ends it, so a later release is not honoured.
