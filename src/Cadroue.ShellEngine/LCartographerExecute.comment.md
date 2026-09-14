# LCartographerExecute.cs

## Inline notes

### `if (!lCartographerLayout.LSceneGroupAuto)`

A captured Merge stage in Manual mode owns no groups.
Groups exist only when a person drags files into them on the live tab.
The stage schema stores none of them.
No group means nothing to merge and nothing to relay, as on a live tab with an empty Group panel.
Auto grouping must never be invented for a Manual stage.
