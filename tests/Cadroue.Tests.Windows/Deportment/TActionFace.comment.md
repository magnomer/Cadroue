# TActionFace.cs
Hash: `12dbfe07bb933185`

## `public sealed class TActionFace`

Checks the action cluster deportment without any control.
It covers the auto-relay flag, the relay target and its face, the menu and the three run entry points.
The Cartographer collection keeps it from running beside tests that share the relay plan.

## `public void AutoSet_ReadsCheckedOnly()`

The checkbox hands over a nullable state.
Only an explicit true turns auto-relay on.

## `public void RelayAttach_HoldsTabAndAnswersStripSeams()`

Attaching ties the action to its tab and to the strip.
The strip's relay-pending read then follows the auto flag.

## `public void RelaySelect_ShowsTabFace_RefusesSelf()`

Selecting a target shows that tab's icon and title.
Selecting the same target again raises no second face notice.
A tab never relays to itself.
The finish target is a valid choice and hides the face.

## `public void RelayApply_UnknownTab_FallsBackToNone()`

A restored target that is no longer a tab becomes no target.

## `public void MenuRead_WrapsOptionsWithNoneAndFinish()`

The menu lists no target first and the finish target last.
The other tabs sit between them.

## `public void Runs_GateOnEligible_RaiseEmptyOtherwise()`

No run starts while the eligible read is empty.
Each refused run raises the empty notice instead.
With eligible sources the all and high runs fire and the list run falls back to a normal run.

## `public void ListRun_WithSelection_SendsItems()`

With a list attached the list run sends the selected paths as items.
It starts no priority run.
