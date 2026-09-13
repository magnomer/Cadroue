# PInspectorCurveCanvas.cs

## `private static int PCurveHitFind(Point pPixel, IReadOnlyList<LWorkCurvePoint> pPoints)`

Nearest control point within the hit radius, else -1.

## `private static int PCurvePointAdd(Point pPixel, List<LWorkCurvePoint> pPoints)`

Insert a new interior control point at the clicked value, returning its sorted index.

## `private static Point PCurvePointResolve(double pInput, double pOutput)`

Value → canvas pixel: input on X (0 left → 1 right), output on Y (0 bottom → 1 top).
Reused next job for hit-testing.

## `private static LWorkCurvePoint PCurveValueResolve(Point pPixel)`

Canvas pixel → value, the inverse of `PCurvePointResolve`.

## `private static double[] PCurveTangentResolve(double[] pXs, double[] pYs)`

Monotone pchip tangents (Fritsch–Carlson), matching FFmpeg's interp=pchip so the on-screen track equals the rendered result.
