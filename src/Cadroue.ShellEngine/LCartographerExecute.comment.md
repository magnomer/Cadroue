# LCartographerExecute.cs
Hash: `6cf231376aa5fe11`

## `public static Task<IReadOnlyList<string>> LCartographerStageRun(LCartographerStagePlan lCartographerPlan)`

Queues one captured stage's inputs through the messenger of its layout, with no live tab involved.
A layout key it does not know runs as Split.
It returns only the inputs the stage acknowledged, for the caller to commit against the plan.

## `private static IReadOnlyList<string> LCartographerEditRun(`

The Edit plan comes from the stage's captured inspector record, or an empty plan when none was kept.
The eq question goes through LInventoryEngine, so the cartographer never names the Infrastructure inventory.

## `private static IReadOnlyList<string> LCartographerAcknowledgedRead(`

An input counts as acknowledged only when the schedule holds work for it under this batch and stage.
So an input the planner skipped stays unacknowledged, even when the messenger was called.

## Inline notes

### `if (!lCartographerLayout.LSceneGroupAuto)`

A captured Merge stage in Manual mode owns no groups.
Groups exist only when a person drags files into them on the live tab.
The stage schema stores none of them.
No group means nothing to merge and nothing to relay, as on a live tab with an empty Group panel.
Auto grouping must never be invented for a Manual stage.
