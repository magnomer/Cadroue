# Export

This folder adapts Export policy and preset storage to a prepared WPF surface.
Conduct owns the decisions.
These drivers own control lookup, localization, dialogs, event routing, repaint order and surface lifecycle wiring.
The preset list and row drivers live in the `Preset` folder.

## QExport.cs

The class binds one preset selection to Conduct operations and the real preset store.
It constructs and retains the drag, transfer, list, naming and summary drivers for that binding.

Store changes enter `QExportSync`, while selection changes enter `QExportUpdate`.
Sync is skipped while a name is edited or a drag is active.
This prevents a store notification from rebuilding the list during either interaction.

The failure-raise overloads set a request flag around each Conduct operation.
The flag suppresses reentrant updates and is cleared in a `finally` block.
A true result publishes one update after the flag clears.
A false result invokes the supplied failure callback.
A null result does neither.

Attach subscribes to store and selection changes before its initial sync.
Detach removes the active rename handlers and both subscriptions while retaining the edited-name state for a later rebuild.

The summary digest is projected from the working copy at call time.

## QExportCommands.cs

The class creates default command routes and attaches the title, scrollbar, preset body and five command buttons.
Optional creation, deletion, configuration, outbound and inbound routes replace their defaults independently.
Default routes own localized dialogs, warnings and encoder-window presentation, leaving the XAML surface passive.

Attachment installs icons, tooltips and button looks.
It registers clipping on inner-border size changes and routes one Export repaint subscription through the surface dispatcher.
Initial preparation attaches preset input and runs the first repaint immediately.
Each `Loaded` replaces the repaint subscription, reattaches preset input before the state owner attaches and runs an explicit repaint.
Each `Unloaded` removes the repaint subscription, then detaches preset input, headers and rows before the state owner detaches.

## QExportSurface.cs

The static factory receives an already-created surface and returns that same typed surface after preparing it.
It resolves named controls and two brushes from the surface contract.
It builds the command and preset drivers, attaches the summary and supplies the repaint order.
That repaint rebuilds presets before updating the digest.
Missing controls, resources or incompatible template structure fail through contract lookup or casts instead of producing a partial surface.

## QExportDigest.cs

The digest is an immutable snapshot of video, audio, container and output text.

## QExportSummary.cs

The summary driver resolves and retains the corresponding text controls, localizes their labels, and writes a fresh projection on update.

## QExportTransfer.cs

The driver invokes success, failure or refusal according to the nullable Conduct outcome.
A cancelled save or import invokes none of them.
File dialogs remain in the commands driver, while this class handles only their confirmed paths and results.

## Stamp

Commit: `9a34a722aa84`
- `QExport.cs` `ac2b5c243910ee8b`
- `QExportCommands.cs` `3e081f2cf9aa565d`
- `QExportDigest.cs` `daeee07d925415c4`
- `QExportSummary.cs` `dc73e24351178a26`
- `QExportSurface.cs` `21501f049ecf9e6f`
- `QExportTransfer.cs` `c877c18ed1cd5078`
