# Encoder

This folder owns the encoder window's WPF controls, paint state, and raw input relay.
Conduct owns the draft, available choices, normalization, validation, and codec decisions.
UI helpers retain only the control state needed to rebuild or commit a surface.

## QSEncoder.cs

`QSEncoder` creates one Conduct draft and gives each row group its gate.
Output reads and writes, apply, save, close, and readiness are relayed to that draft.
One window therefore shares one draft across video, size, audio, suffix, and location controls.

The video driver also keeps the whole draft, to read and set the video detail.

## QSEncoderAudio.cs

The audio surface renders stream and mode choices and rebuilds codec-dependent controls.
It relays selections to `CSEncoderAudio`.
A Conduct change repaints the surface only after the surface has been built.
This protects the constructor-time subscription from accessing absent controls.

The verification progress bar is visible only while the awaited scan runs.
Results remain unavailable until at least one verdict row has been retained.
A cancelled scan keeps no new UI answer and is caught at the surface boundary.

The nested contract resolves named controls and sheet brushes from the supplied `UserControl`.
Missing or wrongly typed template parts fail through `QContract`.
Accessing the audio surface before build fails with `InvalidOperationException`.

## QSEncoderChip.cs

Codec entries use a shared frozen-brush template.
The last `" / "` separates a display head from a token chip.
Entries without that separator show the whole value and collapse the chip.
Conversion is display-only because reverse conversion returns `Binding.DoNothing`.

## QSEncoderFramerate.cs

The text box and slider mirror one Conduct-defined frame-rate value.
A `StrongBox<bool>` suppresses their reciprocal change handlers while one control writes the other.
The localized source value is painted muted with its hint.
Other values are painted as user input.

## QSEncoderLocation.cs

The surface defines the location modes.
Per-mode folder memory and visible fields come from the Conduct location gate.
The browse dialog receives the current path only when `CUsher` confirms that folder exists.
Optional controls allow the same presentation update to serve partial surfaces.

## QSEncoderName.cs

The output-name editor offers ten fixed token chips plus counted Backspace and Delete tokens.
Count input accepts digits.
It normalizes missing, non-positive, or invalid text to `1` when focus leaves.

A pointer movement of at least four pixels along either axis starts a copy drag.
A release without such a drag inserts the token directly.
Pointer activity over a count box does neither operation.
Editing the count therefore cannot also insert or drag its token.

## QSEncoderNameDrag.cs

Name-chip drags publish the same token under the token-control format and plain text.
They then request a copy operation.
The rich token editor and ordinary text drop targets can consume one drag payload.

## QSEncoderOutput.cs

The output surface owns only control population and raw values.
Container changes rebuild the extension offer and notify video and audio surfaces.
If Conduct offers no extension, the combo contains one empty value.
Otherwise it keeps the prior offered extension and falls back to the first offer.

Collision and location changes first pass the displayed suffix or folder to their Conduct gates.
This preserves a separate value for each policy or mode while another is visible.
Losing suffix focus normalizes it.
Browsing changes the text only when the folder dialog succeeds.

## QSEncoderPicture.cs

The picture surface coordinates codec, rate-control, size, frame-rate, pixel layout, and mode controls.
Codec or rate changes are relayed to Conduct.
A kept Conduct change resynchronizes rate choices, rebuilds codec-specific rows, and updates availability paint.

Applying the window reads raw presentation values and sends one draft update.
A video trial exposes progress while awaited and catches cancellation at the surface boundary.
Its result button is enabled only when Conduct has retained rows.

## QSEncoderRate.cs

Rate controls are rebuilt from the current Conduct codec.
Rebuild clears old faders, dynamic rows, slider references, and option references.
It then adds the codec's optional quality, notice, speed, and extras.
Missing optional parts add no control.
The codec resolves selected extra indexes back to named values.

## QSEncoderResolution.cs

The resolution surface always creates two dimension rows.
Conduct owns native, custom, tier, notch, knob, and shown-value interpretation.
A write guard prevents repainting the text boxes from recursively submitting their intermediate values.

Tier and dimension thumbs are hidden when their values cannot be changed through that scale.
The source-sized state uses localized source text, muted paint, and a visible hint.
Orientation changes only the localized axis labels.
It is committed later with the picture draft.

## QSEncoderSize.cs

`QSEncoderSize` is a thin relay over the Conduct size gate.
It forwards tier and typed-dimension input and republishes only kept size changes.
The resolution surface then repaints from the gate's canonical dimension.

## QSEncoderSound.cs

Audio codec details are rebuilt from a `CSEncoderSound` answer.
The current sample-rate and channel choices are retained as input to that answer.
Rebuild clears codec-dependent quality, speed, notice, and extra-option controls before adding the new set.
Absent quality or speed contributes no row.
Blank caution text contributes no notice.

Saving reads selected indexes and fader values.
It relays them with stream and mode to `QSEncoderAudio` without making a codec decision locally.

## QSEncoderSuffix.cs

Suffix visibility, per-policy memory, policy switching, and normalization are delegated to `CSEncoderSuffix`.
The WPF output surface does not duplicate collision-policy rules.

## QSEncoderVideo.cs

`QSEncoderVideo` exposes Conduct video, trial, and draft state to WPF without owning their decisions.
It republishes kept video changes and rejects empty encoder or rate input before forwarding a selection.
It also maps retained trial rows into verdict rows.
Cancellation propagates from the trial so the picture surface can end the UI operation.

`QSEncoderChoice` keeps stable tokens separate from localized labels.
Reading accepts this typed choice or the application's older localization choice representation.

## QSEncoderWindow.cs

The window constructs output, picture, and audio surfaces around one `QSEncoder`.
Container changes refresh both media surfaces.
Apply saves all control state before asking Conduct to apply the draft.
It then refreshes the summary.
OK performs the same apply before setting a successful dialog result.
Cancel closes without applying.

The close event closes the draft and saves placement.
Stored placement is restored under the fixed `Encoder` key.
Conduct readiness is marshalled through the window dispatcher before repainting audio.
This preserves WPF thread affinity.

## Stamp

Commit: `b84e5458de50`
- `QSEncoder.cs` `b5298d2bdd2971d7`
- `QSEncoderAudio.cs` `e631db73ed66511e`
- `QSEncoderChip.cs` `5d431c5c79e9d543`
- `QSEncoderFramerate.cs` `867080c70d64eb97`
- `QSEncoderLocation.cs` `860efc1cc323f0fb`
- `QSEncoderName.cs` `5442babfb0fbf851`
- `QSEncoderNameDrag.cs` `8191fd0efb3eaf59`
- `QSEncoderOutput.cs` `a8e60b66b815bf61`
- `QSEncoderPicture.cs` `d5a8d3eeeadef28c`
- `QSEncoderRate.cs` `7153a4b96b99a134`
- `QSEncoderResolution.cs` `34d73bd5e46223e2`
- `QSEncoderSize.cs` `cad0bef1534dbc72`
- `QSEncoderSound.cs` `1b67ffb38ca7faf9`
- `QSEncoderSuffix.cs` `1f9aabc540d3cddb`
- `QSEncoderVideo.cs` `0c78be7e320de301`
- `QSEncoderWindow.cs` `7785e166054e4991`
