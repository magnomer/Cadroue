# LCurveCanvas.cs

## `public int LCurveHitFind(double lX, double lY, double lSize)`

Nearest control point within the hit radius, else -1.

## `private static LCurvePoint LCurvePixelResolve(double lInput, double lOutput, double lSize)`

Value → canvas pixel: input on X (0 left → 1 right), output on Y (0 bottom → 1 top).
Reused for hit-testing.

## `public static (double, double) LCurveValueResolve(double lX, double lY, double lSize)`

Canvas pixel → value, the inverse of `LCurvePixelResolve`.
