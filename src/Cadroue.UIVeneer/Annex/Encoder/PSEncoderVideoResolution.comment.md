# PSEncoderVideoResolution.cs
Hash: `80d6bc0d348870be`

## `internal void PSVideoResolutionBuild(Panel pHost)`

Builds the size tier row, the width and height rows, the source notice and the reactive row into the given host.
The tier, width and height live in the Conduct size gate `CSEncoderSize`, which QSEncoderSize relays.
Every control redraws when `QSEncoderSizeChanged` fires, which PSEncoder wires to PSVideoSizeApply.
The tier slider selects a tier through QSEncoderSize directly.
A dimension slider only writes its box, and box edits alone carry dimensions to QSEncoderSize.

## `private static void PSVideoDimensionChange(Slider? pSlider, TextBox? pBox)`

A slider writes its box only when the text differs, so an echo from PSVideoSizeApply stops there.
Zero shows as an empty box, which the size gate reads as no value on that axis.

## `private void PSVideoDimensionChange()`

Both boxes go to QSEncoderSize together.
The size gate picks the source, a matching tier or custom.
A box showing the localized source word reads as zero, so two such boxes keep the source tier.

## `internal void PSVideoSizeApply()`

Redraws the whole size block from QSEncoderSize after any tier or dimension change.
The source tier mutes both boxes and shows the notice that the source size applies.
A custom size leaves the tier slider where it was, since no tier matches.

## `private void PSVideoKnobApply()`

Hides a slider thumb whose position means nothing in the current tier.
The tier thumb hides for a custom size, and the dimension thumbs hide for the source size.
It reruns on each slider load, because the thumb exists only after the template applies.

## `private void PSVideoReactiveApply()`

The reactive switch relabels width and height as the X and Y axes.
