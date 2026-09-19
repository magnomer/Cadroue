# PInspectorCurveCanvas.cs

## `private static int PCurveHitFind(Point pPixel, IReadOnlyList<LWorkCurvePoint> pPoints)`

Nearest control point within the hit radius, else -1.

## `private static Point PCurvePointResolve(double pInput, double pOutput)`

Value → canvas pixel: input on X (0 left → 1 right), output on Y (0 bottom → 1 top).
Reused for hit-testing.

## `private static LWorkCurvePoint PCurveValueResolve(Point pPixel)`

Canvas pixel → value, the inverse of `PCurvePointResolve`.
