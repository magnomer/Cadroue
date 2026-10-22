# Export

The Export panel is passive structure.
Behaviour lives in Conduct policy and in the Deportment drivers.
What remains here declares markup and composes the drivers.
The preset list surfaces live in the `Preset` folder.

## PExport.cs

The panel attaches the summary driver to its digest surface and builds the command driver.
Then it hands the controls to the commands driver.
The preset driver is created here and passed in as delegates.
The repaint callback rebuilds the preset list, then updates the summary, so the two never disagree.
An internal constructor takes handlers for the commands, so tests can replace a dialog.
The public constructor passes none.

## PExportCommands and PExportSummary

The command strip is a resource dictionary with one template, and the summary is a `Border`.
Both are markup only.
The drivers set their texts and icons.

## PExportNotice

The two notices show the incompatible and the missing preset announcements.
They are not part of the panel.
The tabs subscribe their preset events to them.

## PExportTheme

The panel hands the text brush to the command driver and the muted brush to the preset driver.

## Stamp

Commit: `b84e5458de50`
- `PExport.cs` `1b0532cf797b8a88`
- `PExport.xaml` `df3f89721b78725c`
- `PExportCommands.cs` `effdde8b556a8353`
- `PExportCommands.xaml` `5dd37ba103fbb309`
- `PExportNotice.cs` `5cc14954e91992c2`
- `PExportSummary.cs` `55a9754fc6e3f7e4`
- `PExportSummary.xaml` `5b8d1d516eb60663`
- `PExportTheme.cs` `779fd77e01f676df`
