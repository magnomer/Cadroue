# QFlowEdit.cs
Hash: `20bf8c20dfe9d517`

## `public sealed class QFlowEdit`

Owns the section editing state of one timeline: selected index, editable and fired.
None of this state is read by the spool, cursor or drag roles.

## `public QFlowEdit(QFlow lOwner)`

Holds the owner so a lock can hide the rename popup through the owner's name sibling.

## `public event Action<bool>? QFlowEditChanged`

Raised only when editability really changes, so subscribers never redraw on a repeat.

## `public bool QFlowEditCheck()`

Editing needs the owner's section band shown and editing not switched off by a lock or a detection run.

## `public void QFlowSectionSelect(int? lSectionIndex)`

Records the selected section index, or null when the source clears.

## `public bool QFlowEditSet(bool lSectionEditable)`

Hides the rename popup on every switch-off, even a repeat, then reports whether the state changed.

## `public void QFlowFiredSet(bool lSegmentFired)`

Records whether the last section edit already raised its notice.
