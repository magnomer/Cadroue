# QSectionEdit.cs

## `public sealed class QSectionEdit`

The class hands the edited text, the editor's keys and its focus loss to the edit gate.
The buffers and the commit rules live in the gate.
Test `Rename_StartsEditing_CommitAppliesTrimmedAffixes_CancelRestores` pins commit and cancel through this class.

## `public int QSectionEditSerial`

The serial comes from the gate, so a late blur from a previous editor can be recognized.

## `public bool QSectionKeyRun(string lKey)`

Return commits and Escape cancels, and both return true.
Any other key returns false, so it stays the surface's own.

## `public void QSectionBlurHandle(int lSerial, bool lFocusInside)`

The gate decides from the serial and the focus.
Test `Blur_CommitsOnlyTheLiveEditor_WhenFocusLeftTheBoxes` pins that only the live editor commits.
