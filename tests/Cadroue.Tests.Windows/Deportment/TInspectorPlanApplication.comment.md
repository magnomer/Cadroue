# TInspectorPlanApplication.cs
Hash: `0cc23956de72bb0a`

## `public sealed class TInspectorPlanApplication`

Checks how the inspector deportment applies plans and raises its aggregated notices, without any control.
Each test builds a fresh inspector.
The notice tests count what the inspector raises.

## `public void PlanApply_SetsEverySection_RaisesOneVideoNotice()`

A whole plan touches crop, tone, gamma, exposure and skip.
The viewer must hear one video notice for the bulk apply, not one per section.

## `public void VideoApply_RaisesOnce_SingleSetRaisesEach()`

The single notice is a property of the bulk apply only.
Individual value edits afterwards each raise their own notice.

## `public void PersistentChange_RaisesOncePerFlip_NotOnValues()`

Only a real change of a persistent switch raises the aggregated notice.
Value edits and repeated sets of the same state raise nothing.

## `public void AudioApply_RaisesOneAudioNotice()`

Applying an audio plan raises one audio notice and sets the section values.

## `public void Section_Shown_Title_FollowStep()`

The selected step decides which section shows and which title the inspector carries.
An unknown step shows the empty state with the default title.

## `public void StepSet_AwayFromCrop_DisarmsTool_RaisesToolNotice()`

Leaving the crop step must disarm the crop tool.
Subscribers hear the armed and disarmed transitions in order.

## `public void TipResolve_DisabledThenPreview_ThenApplyKeys()`

An incapable section is disabled and its box tip carries the disabled reason.
A capable section without preview carries the preview notice.
With neither, the box tip falls back to the common apply text.
