# TFlowSectionActive.cs
Hash: `50f4973dd9eb006b`

## `public sealed class TFlowSectionActive`

Checks the flow deportment facts about its source, edit gate and change reports without any control.
Each test builds a flow with a 60 second source and the command gate open.

## `public void EditCheck_RequiresSectionActiveAndEditable()`

Sections can be edited only while the section feature is on and the editable flag holds.
The editable setter reports a change once and raises the notice only then.

## `public void Source_ClampsCursor_AndMatchesCaseInsensitive()`

The cursor always stays inside the source duration.
Source paths are compared without regard to case.
Clearing the source drops the spool and the duration.

## `public void ScanCheck_FailsWithoutCommandOrAfterUnload()`

No scan may start without the command gate or after the flow unloaded.
A blank source path counts as no source.

## `public void StampAndLosslesscut_ReportChangesOnly()`

The keyframe stamp and the LosslessCut path setters return true only when the value changed.
The path comparison ignores case.
