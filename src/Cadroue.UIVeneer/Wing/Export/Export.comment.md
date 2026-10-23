# Export

This folder contains the passive Export surface, its local resources and its notice presentation.
Conduct owns Export decisions, and UIDeportment drivers prepare and operate the surface.
Preset list markup lives in the `Preset` folder.

## PExport.cs

The code-behind only calls `InitializeComponent`.
It neither accepts a state owner nor constructs drivers.
Composition prepares the instance before a tab receives it.

## PExport.xaml

The markup declares the named title, preset body, command host, inner frame and digest required by the UIDeportment surface contract.
Its resources provide `PExportTextBrush` and `PExportMutedBrush` to that driver boundary.
Visual constants and structure remain here so the shell does not acquire behavior.

## PExportCommands

`PExportCommands` is a resource dictionary whose template provides five command buttons in the driver-required child order.
Its code-behind only initializes the markup.
Drivers supply icons, localized tooltips and handlers.

## PExportSummary

`PExportSummary` is a border whose named text blocks receive container, video, audio and output projections.
Its code-behind only initializes the markup.
The driver supplies localized labels and projected values.

## PExportNotice

The two methods present localized announcements for incompatible and missing presets.
Tabs subscribe mode-specific preset events to these shared notices.
The Export surface itself does not own those subscriptions.

## Stamp

Commit: `9a34a722aa84`
- `PExport.cs` `30315843ef160567`
- `PExport.xaml` `949ea30b3a6b4f60`
- `PExportCommands.cs` `effdde8b556a8353`
- `PExportCommands.xaml` `5dd37ba103fbb309`
- `PExportNotice.cs` `5cc14954e91992c2`
- `PExportSummary.cs` `55a9754fc6e3f7e4`
- `PExportSummary.xaml` `5b8d1d516eb60663`
