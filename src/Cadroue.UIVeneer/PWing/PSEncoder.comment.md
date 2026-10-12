# PSEncoder.cs
Hash: `9d063d52d22edb77`

## `public PSEncoder(LPreset lsExportSpecificState, System.Action pRefresh, bool psSmartAllowed = false)`

The dialog edits a clone that QSEncoder holds, never the preset it was given.
Controls are seeded from that clone so Cancel discards every edit.
The refresh callback lets the export panel redraw its summary after an apply.

## `private void PSEncoderApply()`

Writes the dialog controls into the clone, then QSEncoder copies the clone onto the stored preset.
Both Apply and OK run it, while Cancel and the close button skip it.
Values QSEncoder tracks itself, such as encoders and rate control, are filled in by QSEncoderApply.

## `private void PSEncoderHelpHandle() => Dispatcher.InvokeAsync(PSAudioRowsApply);`

Encoder help loads in the background, so sample and channel lists arrive after the dialog opens.
The notice comes from a pool thread, hence the hop onto the dispatcher before rebuilding the audio rows.

## `private void PSEncoderCloseHandle(object? sender, System.EventArgs e)`

Closing cancels any running trial scan and drops the help subscription held by QSEncoder.
It also saves the window placement under the encoder key.
