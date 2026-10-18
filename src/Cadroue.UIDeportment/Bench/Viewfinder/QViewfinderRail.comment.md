# QViewfinderRail.cs
Hash: `3dff2a35244f394c`

## `public static class QViewfinderRail`

Places the viewfinder rail between the ruler lane and the coverage strip.
The frame and the section popup both read it, so their bands line up.

## `internal static (double, double) LViewfinderRailResolve(double lHeight)`

The rail starts one gap below the lane and ends one gap above the coverage strip.
A strip too short for a rail returns a bottom equal to the top.
