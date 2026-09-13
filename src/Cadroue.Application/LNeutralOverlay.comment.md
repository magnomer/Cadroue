# LNeutralOverlay.cs

## `public static LNeutralPoint LNeutralPointResolve(`

Map a viewer-overlay click back to the raw (untransformed, stored-orientation) source pixel.
The overlay shows the frame after the mpv display pipeline hflip -> vflip -> transpose(rotate) -> crop -> scale-to-fit(letterbox).
This resolver walks that chain in reverse.
The shown region is the crop rect when a crop is applied, otherwise the whole rotated frame.
Both are in the final (post-transpose) pixel space.
Source dimensions are the raw stored dimensions.
