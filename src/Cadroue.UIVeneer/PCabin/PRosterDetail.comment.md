# PRosterDetail.cs
Hash: `82c40fc874529714`

## `private static readonly IReadOnlyDictionary<QRosterDetailKind, Action<StackPanel, QRoster>> PRosterFillers =`

One filler per detail kind, so the veneer picks the pane content by lookup instead of branching.

## `public static void PRosterDetailUpdate(StackPanel pPanel, QRoster lRoster)`

Rebuilds the detail pane from the current selection.
Shared rows come from PRosterOverview, so the look stays in one class.

## `private static void PRosterJobAdd(StackPanel pPanel, QRoster lRoster)`

Section order is failure, overview, record, then video and audio encoding side by side.
Empty lists in the QRosterDetail plan simply add nothing, so no section needs a shown check.

## `private static Grid PRosterRowBuild(QRosterDetailRow lRow)`

The row key picks both brush and weight, so a failure value stands out without a branch here.
