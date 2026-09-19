# PSEncoderVideoFps.cs

## `private static readonly PSVideoScale[] psVideoFpsScale = PSVideoScaleCreate()`

Index 0 is the Source sentinel, and the remaining entries are the ordered rate scale.

## `private void PSVideoFpsBuild(Panel pHost)`

The editable field is authoritative and accepts any FFmpeg rate expression.
Moving the slider overwrites it with the picked scale value.
Slider index 0 means source, which reveals the explanatory notice below the row.
