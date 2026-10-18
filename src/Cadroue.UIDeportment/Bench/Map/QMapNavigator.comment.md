# QMapNavigator.cs
Hash: `10abb4774ba80e43`

## `public static class QMapNavigator`

Builds the navigator shapes on the map, the rect marking the working range.
It holds no state, so the map calls it as plain geometry.

## `internal static void QMapNavigatorAdd(`

Adds fill, shadow, rim, sides, edge grips, move grip with dots, shine and border in paint order.
Nothing is drawn when the body is no taller than its shadow drop.

## `internal static (double, double, double) LMapMoveResolve(`

Places the move grip centred between the two edge handles, sized to the navigator.
No grip exists when the handles leave no room between them.
The map hit test reads the same placement, so drawing and hitting agree.
