# QIcon.cs
Hash: `905e41a0e34aed14`

## `public static class QIcon`

Resolves the bitmap stand-in and reader kind for a `/PAsset/` path.
The extension read goes through `LUsher`, the engine owner of path text.

## `public static bool QIconSvgCheck(string qIconPath)`

Answers whether an icon path names a vector file.
Only svg, png and ico have a reader, so any other extension throws.

## `public static string? QIconBitmapResolve(string qIconPath, double qIconWidth)`

Names the prerendered bitmap that may stand in for a vector icon at the requested width.
Null means the path keeps its own file, either because it is a bitmap or lies outside the asset root.
The Veneer still checks that the bitmap resource exists before using it.

## `public static bool QIconPrerenderCheck(string qIconPath)`

Answers whether a path lies in a prerendered bitmap tree, `P32` or `P256`.
Only those bitmaps get their transparent margin trimmed.

## `private static string? QIconSvgResolve(string qIconPath, double qIconWidth)`

Maps `/PAsset/{folder}/P{Name}.svg` to `/PAsset/{P32|P256}/{folder}/{Name}.png`.
The leading `P` is dropped because the bitmap tree names files without the prefix.
Widths under 60 take the `P32` tree, larger ones `P256`.
