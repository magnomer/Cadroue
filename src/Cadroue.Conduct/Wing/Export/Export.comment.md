# Export

Conduct holds the Export decisions that need no window.
It receives every engine operation as a delegate, so the policy runs without the preset store or any control.
Deportment injects the real operations, and the tests inject fakes.

## CExport.cs

The class is the medium-free projection of the Export preset and the policy for its actions.
It reads the selected name and the working copy, and it answers what an action means for the current selection.
A native preset is never saved or deleted, so those requests return null and nothing is written.
Null means refused, true means done, and false means the engine failed.
The caller alone decides what to show for each of the three.
An empty selected name reads as no selection.
Restore needs a selection but not a user preset, since a native preset can also carry a modified working copy.
Add resolves the final name first.
It returns the name beside the save result, so a failure names the preset written.

## CExportRow.cs

The row state is a pure function of the name, the selection and the working copy.
Only the selected row is compared with the working copy.
A native row counts as matched, so it is never shown as modified.
Support is asked for every row with the selected flag.
The selected row is judged by the working copy that would run.
The row state carries the supported flag for the caller, and this class refuses nothing.
Test `RowRead_NativeSelectionMatchAndSupport_PreserveShortCircuitAndWorkingIdentity` pins the short circuit and the working identity.

## CExportTransfer.cs

File transfer keeps three outcomes apart, so no failure is dressed as another.
Null is a refusal, either a catalogue path on save or a file that loads as nothing on import.
A thrown read becomes a refusal that carries the error text.
False is a write that failed, and true is success.
Import saves under the resolved name before it selects, and a failed save selects nothing.
A blank imported name takes the caller's fallback.
Tests `TransferSave_CatalogueWriteFailureAndSuccess_AreDistinctAndOrdered`, `TransferImport_ThrowAndNull_AreReadOutcomesWithoutMutation` and `TransferImport_BlankNameUsesFallback_SavesBeforeSelectingAndStopsOnFailure` pin this.

## Stamp

Commit: `b84e5458de50`
- `CExport.cs` `7bc83ab822f5969e`
- `CExportRow.cs` `0be128c4aea07ae3`
- `CExportTransfer.cs` `cd1de9615e949bdd`
