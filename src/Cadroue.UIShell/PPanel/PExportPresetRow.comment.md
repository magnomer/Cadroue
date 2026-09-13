# PExportPresetRow.cs

## `private bool PExportSupportCheck(string lPresetName, LPreset lWorking)`

Whether this row's preset can carry the work of the tab hosting the panel.
The row stays selectable either way.
An unsupported one is only marked, and refused when the user actually runs it.
The selected row is judged by the working copy, which is what would run.
So editing it back into range clears the mark at once.

## `public static bool PExportSupportCheck(LPresetSelection lPresetOwner, LWorkKind lExportKind)`

The refusal the action itself makes, over the selection that would actually be sent.
