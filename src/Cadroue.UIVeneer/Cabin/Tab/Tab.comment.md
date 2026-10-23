# Tab

This folder assembles prepared panels into workspace tabs and applies presentation layout around them.
Tab constructors receive a supplied `PExport`.
They coordinate its lock and layout with other panels without constructing it.

## PConvertTab.cs

The constructor receives `PExport` through its parameter and includes it in the panel sequence.
It coordinates Export locking with the list.
It subscribes the Convert mode's missing-preset event to the shared Export notice.

## PMergeTab.cs

The constructor receives `PExport` through its parameter and includes it in the panel sequence.
It coordinates Export locking with the list.
It subscribes the Merge mode's missing-preset event to the shared Export notice.
The tab also applies and reads merge-specific layout state and closes its merge driver before the base list close.

## PTabSurface.cs

The base surface builds one grid from the supplied panel sequence.
It derives layout roles from concrete panel types.
It records the last `PExport` index so the shared surface model can restore or toggle Export visibility.
Applying Export visibility updates the model column, panel, adjacent splitter column and splitter together.
A user toggle then raises the width event.

Panel widths come from each panel's minimum and an optional saved layout.
Collapsible panels feed their state changes back into column widths.
Restored collapsed indexes are applied through each panel's own writer.
The action row and flow are added only after panel columns, splitters and restored visibility have been established.

## PTabGrid.cs

The grid helper owns WPF placement only.
It creates the panel row and adds panels and splitters at indexes resolved by `QSurfaceGrid`.
It removes the trailing gutter and places the compass, action and flow rows.

## PTabPanel.cs

This registry translates concrete panel types into minimum widths, compact-layout participation, collapsed strip widths and collapse adapters.
Unknown types use the default minimum, are not compact, expose no strip width and have no collapse adapter.
`PExport` participates in minimum and compact layout but has no collapse adapter because Export visibility is handled by `PTabSurface`.

## PTabLock.cs

Lock attachment subscribes once to the list lock event and immediately applies the current state.
Each supplied editor is disabled while locked and enabled while unlocked.

## Stamp

Commit: `9a34a722aa84`
- `PConvertTab.cs` `39e119c0c75d09b8`
- `PMergeTab.cs` `19169268b2844095`
- `PTabGrid.cs` `240e6e0738bcf110`
- `PTabLock.cs` `f3b3bd4ee4541bdd`
- `PTabPanel.cs` `652f4f046d864089`
- `PTabSurface.cs` `64a4af3247e27706`
