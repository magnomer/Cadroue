# PSEncoderVideoResolution.cs
Hash: `80d6bc0d348870be`

## `private void PSVideoResolutionBuild(Panel pHost)`

Builds the size tier row, the width and height rows and the source notice into the given host.
The tier, width and height live in LSEncoder, and every control redraws from its size notice.
The tier slider selects a tier in LSEncoder directly.
A dimension slider only writes its box, and box edits alone carry dimensions to LSEncoder.

## `private static void PSVideoDimensionChange(Slider? pSlider, TextBox? pBox)`

A slider writes its box only when the text differs, so an echo from the size notice stops there.
Zero shows as an empty box, which LSEncoder reads as no value on that axis.

## `private void PSVideoDimensionChange()`

Both boxes go to LSEncoder together, which picks the source, a matching tier or custom.
A box showing the localized source word reads as zero, so the source tier holds.

## `private void PSVideoSizeApply()`

Redraws the whole size block from LSEncoder after any tier or dimension change.
The source tier mutes both boxes and shows the notice that the source size applies.
A custom size leaves the tier slider where it was, since no tier matches.

## `private void PSVideoKnobApply()`

Hides a slider thumb whose position means nothing in the current tier.
The tier thumb hides for a custom size, and the dimension thumbs hide for the source size.
It reruns on each slider load, because the thumb exists only after the template applies.

## `private void PSVideoReactiveApply()`

The reactive switch relabels width and height as the X and Y axes.
