# LNeutralColor.cs

## `public static LNeutralSample LNeutralColorResolve(double lNeutralX, double lNeutralY)`

A colour-wheel pick: disc coordinates (each -1..1, centre neutral) name the source cast as a hue/saturation offset.
Reconstruct the gray that carries that cast at a fixed reference value.
Then resolve it exactly as a picked sample, so wheel and picker feed one correction pipeline.

## `public static LNeutralRgb LNeutralRgbResolve(`

Convert an HSV colour (hue degrees, saturation and value each 0..1) to an sRGB byte triple.
Shared by the wheel pick and the inspector's disc rendering.
So the dot the user clicks matches the hue drawn under it.

## `public static LNeutralWheel LNeutralWheelResolve(int lNeutralRed, int lNeutralGreen, int lNeutralBlue)`

Place the wheel dot for a gray sample: hue as angle, saturation as radius.
Value is discarded, since only the cast direction matters on a neutral disc.
