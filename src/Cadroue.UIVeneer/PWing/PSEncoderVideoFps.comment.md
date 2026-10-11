# PSEncoderVideoFps.cs
Hash: `00d0568db9482e8f`

## `private static readonly PSVideoScale[] psVideoFpsScale = PSVideoScaleCreate()`

Index 0 is the Source sentinel, and the remaining entries are the ordered rate scale.

## `private static bool PSVideoSourceCheck(string pFps)`

Blank text, the stored source token and the localized source word all mean keeping the source rate.

## `private static int PSVideoFpsResolve(string pText)`

Places typed text on the nearest scale step, so the slider follows free-form entries.
Source text or a rate that is not a positive number lands on the source step.

## `private void PSVideoFpsBuild(Panel pHost)`

The editable field is authoritative and accepts any FFmpeg rate expression.
Moving the slider overwrites it with the picked scale value.
Slider index 0 means source, which reveals the explanatory notice below the row.
A sync flag stops the slider and the field from echoing each other's change back.
