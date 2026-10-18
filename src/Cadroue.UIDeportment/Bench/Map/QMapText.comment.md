# QMapText.cs
Hash: `076ec28fa97f4477`

## `public static class QMapText`

Fits a section number badge inside one map band from the measured text size.
It holds no state, so the veneer calls it directly per band.

## `public static IReadOnlyList<QMapBadge> QMapBadgeResolve(QMapBand lBand, double lTextWidth, double lTextHeight)`

Returns no badge when the measured text does not fit inside the band with its margin.
