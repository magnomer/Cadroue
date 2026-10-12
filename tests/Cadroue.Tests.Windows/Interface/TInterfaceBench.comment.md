# TInterfaceBench.cs
Hash: `bdaa9e4179124322`

## `internal static partial class TInterface`

The bench part of the test interface.
Tests reach the flow and its owners, the divider, cursor, map, viewfinder and trace only through these forwards.
Most forwards add no logic.

## `internal static IReadOnlyList<QMapBadge> TMapBadgeResolve(QMap map, QMapBand band, double width, double height) =>`

The map argument is unused, because the badge fit is static on `QMapText`.

## `internal static void TFlowTimerAttach(QFlow flow, Action defer, Action<TimeSpan> resume, Action reset)`

Subscribes the three timer notices of the keyframe owner.
The timers themselves stay in the veneer, so a test observes only the decisions.
