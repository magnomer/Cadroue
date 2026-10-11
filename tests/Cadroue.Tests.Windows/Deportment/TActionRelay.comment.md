# TActionRelay.cs
Hash: `751db2958b9ef437`

## `public sealed class TActionRelay`

Checks the action deportment's relay side, covering delivery acceptance, cohort runs and target naming.
Tabs are added to a real strip, each with its own action attached.
The Cartographer collection keeps it from running beside tests that share the delivery seams.

## `public void Accept_DeliversToAutoTab_OnlyWhenArmed()`

A delivered file reaches a tab only when that tab's auto flag is on.
An unknown tab id receives nothing.

## `public void Accept_SkipsMergeTab_EvenWhenArmed()`

The Merge tab does not take delivered files through this seam, even with auto on.

## `public void CohortRun_NeedsAutoAndAnAddedItem()`

A cohort run does nothing unless auto is on.
It reports true only when the cohort callback added at least one item.
The strip's cohort run gives the same answer.

## `public void StripTitleChange_RefreshesTargetFromCartographer()`

Renaming the relay target updates the action's face text.
The chosen target and the source tab stay unchanged.
