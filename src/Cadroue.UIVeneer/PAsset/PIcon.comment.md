# PIcon.cs
Hash: `fb15d7b7d326ba3c`

## `private static readonly IReadOnlyDictionary<bool, Func<Uri, Brush?, ImageSource>> PIconReaders =`

Picks the reader by the vector answer `QIcon` gives, so the Veneer never reads an extension itself.

## `public static ImageSource PIconRead(string pIconPath, double pIconWidth, Brush? pTintBrush)`

Caches each image by its resolved resource and tint, so one icon is read once per look.
Callers share the frozen image, so none may change it.

## `private static string PIconPathResolve(string pIconPath, double pIconWidth)`

Swaps a vector icon for its prerendered bitmap when `QIcon` names one and the resource exists.
The resource probe stays here because only the WPF application can open pack resources.

## `private static BitmapSource PIconBitmapResolve(Uri pIconUri, BitmapSource pIconBitmap)`

Trims the transparent margin of a prerendered bitmap, so it aligns like its vector source.
A fully transparent or fully opaque bitmap is kept whole.

## `private static DrawingGroup PIconTintApply(DrawingGroup pIconDrawing, Brush? pTintBrush)`

Tints a clone, so the drawing just read is never changed in place.

## `private static bool PIconBrushCheck(Brush? pBrush)`

Answers whether a brush paints anything visible.
Only visible paint is tinted, so transparent fills keep the icon's holes.
