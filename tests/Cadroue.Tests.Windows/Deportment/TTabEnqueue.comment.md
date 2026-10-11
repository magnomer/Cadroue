# TTabEnqueue.cs
Hash: `287af423e2615bff`

## `public sealed class TTabEnqueue`

Checks how the Convert, Funnel and action deportments decide what to enqueue or deliver, without any control.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void Convert_NoPreset_RaisesMissingOncePerGesture()`

A selected preset that does not exist blocks the enqueue.
Each of the three gestures raises the missing notice once.

## `public void Funnel_Dispatch_OneDeliveryPerMatchedItem_EmptyListDeliversNothing()`

Only files matching a rule are delivered, and only to the target tab of that rule.
The run over the selection delivers nothing when the selection does not match.
The messenger delivery seam is detached afterwards.

## `public void Action_EligibleCheck_NoSourceAllows_DocketUnlockedGates_OverrideWins()`

Without a docket every path is eligible.
With a docket only its unlocked paths are, compared without regard to case.
A tab supplied read replaces the docket decision.
