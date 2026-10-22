# Inspector

This folder wires the Conduct inspector gate to the passive Inspector surface.
Conduct decides what each tool shows and what a change means.
These classes pull the surface parts, relay each user act to the gate and paint the gate's answers.
The sections for Curve and Whitebalance live in their own subfolders, and the detector sections live in the Sensor folder.

## QInspector.cs

The class is the Deportment owner of one inspector.
It builds one wrapper per tool around the Conduct gate and exposes them as properties.
The persistent-change event is forwarded from the gate, which raises it once when the set of kept tools changes.
A kept layout's plan goes to the gate in one call.
The curve takes its step after, because the gate does not read it yet.

## QInspectorFrame.cs

The frame composes and drives one passive Inspector frame.
It builds every section once per surface.
It tags each section body with its step key and adds its persistent check box.
A rebuild with the same surface only repaints.
A rebuild with a new surface detaches the old one first.
Each section contributes an attach and detach pair.
The frame stores the pairs and one update action on the surface resources.
Loaded attaches every pair and Unloaded detaches them, so an unloaded frame leaves the gate's events.
Update shows only the section the gate selects, with its persistent check box.
It also sets the title, the vacancy text and the minimized or expanded view.

## QBlankLifecycle.cs

The class owns the request-handler lifetime of one Blank surface.
It gathers the handlers of the switches, sliders, number boxes and colour wheel as attach and detach pairs.
Every handler first reads a flag on the body resources and does nothing when it is false.
Detach clears the flag before it removes the handlers, so a late event cannot reach the gate.
Attach removes before it adds, so a repeat attach never doubles a handler.

## QBlank.cs

The blank section paints the gate's state and relays user acts.
Its repaints clear the attach flag and restore it afterward, so painting is not read as a user act.

## QInspectorNotice.cs

A semantic notice is projected into wording here.
A notice with arguments is formatted through the localization catalog.
A notice without arguments reads its key as text, and no notice reads as null.

## QInspectorField.cs

The class paints an inspector number field.
A row's slider takes the number within its range.
The box keeps typed text that already reads as the number.
The decisions themselves live in the Conduct field class.

## Section wiring

The Tone, Gamma, Exposure, Volume, Loudness, Noise, Filter, Equalizer and Skip classes follow one rule.
Each hands every input to its gate and shows the gate's state.
The Crop, Orientation, Ratio and Grid classes do the same for the crop gate.
A reset crop disarms the crop tool, and a persistent one is left as it is.
Test `CropRepaint_DoesNotRequestConduct` pins that a repaint requests nothing.
Test `ToolSet_RepeatedRequests_AreSilent` pins that repeated tool requests are silent.
Test `PlanApply_SetsEverySection_RaisesOneVideoNotice` pins the single notice for a plan.

## Stamp

Commit: `b84e5458de50`
- `QBlank.cs` `093065aafd711922`
- `QBlankLifecycle.cs` `0489f6bac386bd1f`
- `QEqualizer.cs` `4e5f442a8d9cb73f`
- `QEqualizerSection.cs` `8b3f4204f858771d`
- `QExposure.cs` `9a8b937b1c18599b`
- `QFilter.cs` `fb13a82580e22d8b`
- `QFilterSection.cs` `f78af9da8b6893b3`
- `QGamma.cs` `f433b3f2eb1455da`
- `QInspector.cs` `22db6d42b9feaae6`
- `QInspectorAudio.cs` `8f7b5c7cfb504c1b`
- `QInspectorCrop.cs` `85f2a1f82e618503`
- `QInspectorField.cs` `c73ea9f37a92579b`
- `QInspectorFrame.cs` `ba4c46c7a5e98618`
- `QInspectorGrid.cs` `fb36fdd55b9e73ff`
- `QInspectorNotice.cs` `758e4fb977984f06`
- `QInspectorOrientation.cs` `7c268c4dabcf16ee`
- `QInspectorPlan.cs` `8448ed3dbfc6929c`
- `QInspectorRatio.cs` `e5f9a2ae7d4a85dc`
- `QInspectorTip.cs` `5ae0c9b294abc91d`
- `QInspectorVideo.cs` `5aace1701736deb9`
- `QLoudness.cs` `b828c7562dd41db4`
- `QLoudnessSection.cs` `a173afd3504088c0`
- `QNoise.cs` `4b8c468cd7e1baea`
- `QNoiseSection.cs` `92faf2036abe2050`
- `QSkip.cs` `2ef248ac3494c470`
- `QSkipSection.cs` `4ca7712140b34d2e`
- `QTone.cs` `7c3d4304b8d6db80`
- `QVolume.cs` `499eff6561a7b2b7`
