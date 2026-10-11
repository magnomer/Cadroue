# PSDiagnosis.cs
Hash: `2b8d9c5cb1da387b`

## `internal static void PSDiagnosisShow(Window pOwner)`

Opens the modeless capability-check window, or activates it when one is already open.
Only one window exists at a time, so repeated clicks never stack probes.
The content builders live in the sibling partial file so this shell stays small.

## `private PSDiagnosis(Window pOwner)`

The window is owned by the owner's own owner when there is one.
That keeps it alive after the Options window that opened it closes.
The probe starts at construction, so the rows fill in while the window is already shown.

## `private void PSDiagnosisCloseHandle(object? pSender, EventArgs pEvent)`

Closing stops the probe and clears the single-instance slot only if it still points at this window.
