# Spectrum

This folder holds the drivers of the palette surface.
The surface is passive, so selection, import, save and removal run here.
Conduct decides what each outcome means, and these drivers only ask and paint.

## QSSpectrum.cs

The driver paints one label per snapshot row and rebuilds them on every snapshot.
A rebuild first detaches the old row handlers, so a repaint never leaves a stale handler on a removed label.
A selected label keeps the accent colour and its thicker edge.
Selected outranks hover, so leaving a selected label does not clear its look.
A label that is neither selected nor hovered has its values cleared back to the template.
Tests `QSSpectrumAttach`, `QSSpectrumImport`, `QSSpectrumSave` and `QSSpectrumRemove` pin the surface.

## QSSpectrumFile.cs

The file driver implements the Conduct port over the palette folder.
It imports, saves, and hides or deletes a palette on disk.
The selection stays with the spectrum, and the file driver never changes it.

## QSSpectrumDialog.cs

The dialog driver owns the load and save buttons and the file and confirmation dialogs.
The passive template never opens a window.
The file pickers, the warning and the confirmation are injected.
The pickers default to the real dialogs, and the warning and the confirmation have no default.
A cancelled picker does nothing.
Attach detaches first, so a second attach never doubles a handler.
Attach also hands the removal request to the spectrum with the confirmation.

## Stamp

Commit: `b84e5458de50`
- `QSSpectrum.cs` `1a91d8e5d0eab20c`
- `QSSpectrumDialog.cs` `466bf6d6deb2f6d3`
- `QSSpectrumFile.cs` `6fb0eb062b8aed0d`
