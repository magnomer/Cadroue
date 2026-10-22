# Inspector

This folder holds the inspector's decisions, with no window and no medium.
One gate object per tool answers what its section shows, what a change means and when a change counts.
The UIDeportment folder of the same name wires these answers to the passive surface.

## CInspector.cs

The class is the inspector gate, one per inspector.
Every decision that spans its tools is one member here, so an owner asks once whatever the number of tools.
The clinic is the fix tab's inspector, so it counts as one more tool.
The audio and video plans are each one object.
The owner reports capability once, and each tool takes its own fact.
An edit plan sets the crop and its ratio, then every video tool, then the skip switch.
A plan that keeps the skip switch keeps it as told, last.

## CInspectorFrame.cs

The frame decides the selected section, the minimized state and the crop-tool coordination.
It also answers the display questions: title key, empty state and which sections show.

## CInspectorField.cs

The field owns the medium-free rules of a number field.
Typed characters are accepted or refused, a commit is parsed and clamped, and equivalent typed text is kept as typed.
An empty field stays empty at zero.

## CInspectorTip.cs

A notice is a localization key plus arguments, before any wording exists.
A tip pairs a section's enabled answer with the notice that takes priority over its ordinary tips.
The disabled notice outranks the preview notice.
The preview notice shows only when the section is capable, the preview is unavailable and a preview key is given.
A section is enabled only when it is capable and active.
Test `TipResolve_DisabledThenPreview_ThenApplyKeys` pins the order.

## CDetector.cs

The detector holds the non-blank detectors' state, normalization, selection and effective-change decisions.
Blank has its own gate, so it is excluded.
A change event fires only when a normalized step differs from the held one.
Test `StepSet_Normalizes_AndEffectiveChangeNoticesOnce` pins this.
A mode or speed index outside its list is ignored.
Test `ModeSelections_IgnoreOutOfRange_AndSameValue` pins this.
Switching the metric moves the volume threshold only when the values still match a preset.
Test `Metric_FollowsMatchingPreset_ButNotDriftedValue` pins this.

## CDetectorPlan.cs

A plan is one detector section's shape before a UI localizes it.
It gives localization keys, bounds, units and optional choice groups.
The window row exists only for luminance and volume.
The plan list excludes blank, and the fixed names are kept.
Test `Plans_ExcludeBlank_AndKeepFixedNames` pins this.
Test `Plans_CastMutationCannotChangeSnapshot` pins that the list is read-only.
A choice group is a label, its row names and whether it shows, before the UI gives it wording.

## CDetectorPreset.cs

The preset keeps the token last chosen for each kind and the policy that matches values to tokens.
A kind without presets has no token.
A value edit keeps the token while the match is lost, so the custom row shows its basis.
Selecting a token outside the kind's catalog changes nothing.
Test `Choice_DriftKeepsToken_AndSelectionRestoresPreset` pins the drift.
Test `Choice_InvalidAndCustomIndices_AreSilent` pins that a bad or custom index does nothing.

## Stamp

Commit: `b84e5458de50`
- `CBlank.cs` `c0b3003299fe92d6`
- `CDetector.cs` `7f3dae0c096e8385`
- `CDetectorPlan.cs` `a2dd90f10df5b695`
- `CDetectorPreset.cs` `974acf410c78ca91`
- `CEqualizer.cs` `f368cefe169c35b2`
- `CExposure.cs` `df3cd88dea13e93c`
- `CFilter.cs` `ed225637d5f59e5a`
- `CGamma.cs` `64a167b21371932a`
- `CInspector.cs` `4b52560bb7f19f34`
- `CInspectorAudio.cs` `8074781d05ab78cd`
- `CInspectorField.cs` `20020ca643db3334`
- `CInspectorFrame.cs` `90a351b1877c4c5f`
- `CInspectorTip.cs` `0f1eb8c41f8953be`
- `CInspectorVideo.cs` `85c0cb5dfad25064`
- `CLoudness.cs` `09d55d4242ad3565`
- `CNoise.cs` `3d714dc896ffd995`
- `CSkip.cs` `cc40a40206853296`
- `CSweep.cs` `87cd160c59ffdd51`
- `CTone.cs` `6aec4b443b10e894`
- `CVolume.cs` `27de00d10c0558f1`
