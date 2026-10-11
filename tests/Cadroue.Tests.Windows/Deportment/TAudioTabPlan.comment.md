# TAudioTabPlan.cs
Hash: `a1fddbfaf89107de`

## `public sealed class TPresetCollection`

Declares the Preset collection that serializes tests sharing the preset store.

## `public sealed class TAudioTabPlan`

Checks the Audio tab deportment without any control.
Each test builds the tab over a real inspector, viewer, docket and processing list.
Tests that touch presets seed the preset store and run inside the Preset collection.
Librarian seams are attached for a test and always cleared afterwards.

## `public void Rows_SixSteps_EveryKeyMapsToAKind()`

Every processing row the tab offers must map to an audio kind.
Keys that belong to other tabs map to nothing.

## `public void PlanRead_FollowsProcessingOrder_CarriesSkip()`

The plan order is the processing list order, not the inspector order.
The skip switch travels with the plan.

## `public void ActiveUpdate_MirrorsSectionActiveOntoProcessing()`

A section turned on in the inspector must show as active in the processing list.
Sections left off stay inactive there.

## `public void ViewerApply_SkipGivesEmptyGraph_ActiveGivesFilter()`

Skip must send the viewer an empty filter graph.
Otherwise the preview would keep playing the bypassed audio chain.

## `public void LayoutRoundTrip_CarriesPersistentKindsAndSkip()`

Only persistent kinds and the skip state survive a layout read and apply.
The applied tab must also refresh its processing list.

## `public void LayoutRead_NothingPersistent_LeavesInspectorNull()`

An unpersisted edit must not leave an inspector record in the saved layout.

## `public void StateSave_OwnerPath_WritesOnce_NoOwnerWritesNothing()`

Saving before the viewer owns a source must write nothing.
Once the source is open, a section change writes exactly one sidecar for the owner path.

## `public void MediaHandle_FirstOwner_AdoptsPendingPlan_SavesIt()`

A plan edited before any media opened is adopted by the first source.
The adopted plan is then saved to that source sidecar.

## `public void PlanRestore_SavedSidecar_AppliesSteps()`

A saved sidecar entry restores both the section values and the processing list state.

## `public void ItemsHandle_PersistentKind_FansOutToAddedPaths_NothingPersistentWritesNothing()`

Persistent kinds are written to the sidecar of every added path.
With no persistent kind, adding paths writes nothing.

## `public void Run_NoPreset_RaisesMissing()`

Both run entry points must report a missing preset instead of enqueueing work.
