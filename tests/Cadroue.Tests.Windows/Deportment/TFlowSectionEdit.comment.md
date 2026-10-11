# TFlowSectionEdit.cs
Hash: `671c30fb22febbb3`

## `public sealed class TFlowSectionEdit`

Checks the flow section edits without any control.
Every edit goes through the section owner, which gates it on the command, editable and source facts.

## `public void Add_RefusesWithoutEditableOrSource()`

Both an uneditable flow and a flow with no source refuse the add and leave the list empty.

## `public void Divide_SplitsTheSelectedSection_AndKeepsTheLeftHalfSelected()`

The cut point is the cursor.
The left half keeps the selection.

## `public void Delete_WithoutConfirmation_RemovesTheSelectedSections()`

The default preference asks no destructive question, so the delete runs at once.
The remaining section keeps its own bounds and does not grow into the gap.

## `public void Clear_EmptiesTheList_AndSkipsWhenAlreadyEmpty()`

An empty list raises no change notice, so subscribers never redraw for nothing.

## `public void Toggle_Move_Sort_Name_ChangeTheRecordsOnlyWhileEditable()`

Sort and move report whether they changed the order.
After editing is switched off, toggle, move and rename are refused.

## `public void Seek_SelectsTheSection_AndMovesTheCursorToItsBound()`

An index outside the list changes neither the selection nor the cursor.

## `public void Add_RecordsTheSectionLine()`

The trace line names the position, the source file and the bounds.
The trace writer is asynchronous, so the test waits for the entry.
