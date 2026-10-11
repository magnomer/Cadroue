# LFlowSection.cs
Hash: `9aeeedb35d042271`

## `public sealed class LFlowSection`

Owns the section list of one timeline through its segment, and every edit with its trace line.
Each edit is gated by the editable flag of the edit sibling.

## `public LFlowSection(LFlow lOwner)`

Creates the segment and subscribes its change and fault notices.

## `public event Action<IReadOnlyList<LPiece>, int?>? LFlowSectionChanged;`

Raised with the list and the selected index after every change and on explicit raise.

## `public LSegment LFlowSegment => lSegment;`

Exposes the segment so the LosslessCut import and the workspace can reach the sections directly.

## `public IReadOnlyList<LSplitSectionDescription> LFlowSplitRead() =>`

Describes every section as a split plan entry for the export request.

## `public bool LFlowEmptyCheck() =>`

Empty means no sections and no selection.

## `public void LFlowSectionAttach()`

Rebinds the segment to the current source and resets it.
The edit sibling then takes over the fresh selection.

## `public void LFlowSectionLoad()`

Restores saved sections for the current duration, then announces the result.

## `public void LFlowSectionReset()`

Detaches the segment from any source and empties it.

## `public void LFlowSectionRaise() =>`

Re-announces the current state without changing it.

## `public void LFlowSectionAdd()`

Needs an editable tab with a source.
The edit trace line is written only if the segment actually fired a notice.

## `public void LFlowStartSet()`

The segment decides whether this adds a section or moves a start.
Nothing is traced when it refuses.

## `public void LFlowEndSet()`

The segment decides whether this adds a section or moves an end.
Nothing is traced when it refuses.

## `public void LFlowSectionDivide()`

Cuts the selected section at the cursor.
Both halves are traced when the cut happened.

## `public void LFlowSectionDelete()`

Asks first when the destructive-confirm preference is on.
The delete remembers the segment version at asking time.

## `private void LFlowDeleteRun(bool lAnswer, IReadOnlyList<int> lSelected, int lApproved)`

Refuses to delete if the sections changed while the question was open.
This keeps stale indexes from removing the wrong sections.

## `public void LFlowSectionClear()`

Asks first when the destructive-confirm preference is on.
The question is skipped when there is nothing to clear.

## `private void LFlowClearRun(bool lAnswer, int lCount, int lApproved)`

The segment rejects a clear whose version is stale, and the refusal is traced.

## `public void LFlowSectionSeek(int lIndex, bool lEnd)`

Selects the section and moves the cursor to its start or end.
It works even on a locked tab, but needs an active loaded timeline.

## `public void LFlowSectionToggle(int lIndex)`

Flips whether a section is hidden and traces the new state.

## `public bool LFlowSectionMove(int lSource, int lTarget)`

Returns whether the order changed.
The trace names the position the section landed on.

## `public bool LFlowSectionSort()`

Returns whether the order changed after sorting by name.

## `public void LFlowNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix)`

Ignores an index outside the list.
The trace tells a first naming from a rename.

## `public bool LFlowCombineApply(`

Replaces the sections with the combined result of a detection sweep.
The first section becomes selected, and an unloaded tab refuses.

## `private void LFlowSegmentHandle(IReadOnlyList<LPiece> lSections, int? lActive)`

Marks the edit as fired before it forwards the change.
Callers read that flag to know whether to trace.

## `private void LFlowFaultHandle(LSegmentFault lFault, int lCount)`

Turns each segment fault into one warning with the affected count.

## `private void LFlowSectionRecord(string lAction, int lIndex)`

Writes one info line with the section number, name and times.
An out-of-range index records only the remaining count.

## `private int LFlowColorRead() => lSegment.LSegmentListRead().Count % lFlow.LFlowPaletteCount;`

New sections take palette colors in rotation by list position.

## `private static LAsk? LFlowAskCreate(string lQuestionKey, string lActionKey) =>`

Returns null when the user turned confirmation off, which means ask nothing.
