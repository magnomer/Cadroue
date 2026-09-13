# PSEncoderVideo.cs

## `private void PSVideoSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)`

Speed values are registered faster -> slower.
The slider reads slowest on the left and fastest on the right, so slider position mirrors the choice index.

## `private static readonly PSVideoScale[] psVideoFpsScale = PSVideoScaleCreate()`

Index 0 is the Source sentinel, and the remaining entries are the ordered rate scale.

## `private void PSVideoFpsBuild(Panel pHost)`

The editable field is authoritative and accepts any FFmpeg rate expression.
Moving the slider overwrites it with the picked scale value.
Slider index 0 means source, which reveals the explanatory notice below the row.
