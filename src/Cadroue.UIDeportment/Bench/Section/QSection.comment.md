# QSection.cs

## `public sealed class QSection`

The class wires the sections panel to the section gate of its flow.
It reads rows from the gate, hands each input to the gate and relays the gate's tells as events.
Only the minimized state of the panel is its own.
Test `Rows_ComeFromPieces_WithNumbersTimesAffixesAndSelection` pins the rows it reads.

## `public QSection(QFlow lOwner)`

The constructor takes the gates from the flow and builds the drag and edit siblings over them.
It subscribes the flow's section and editable notices and then syncs the gate's rows once.

## `private void QSectionUpdateHandle()`

A flow change only asks the gate to update its rows.
Whether the list is the same or changed is decided in the gate.
Test `SameList_AppliesSelectionOnly_ChangedListRebuilds` pins that a same list applies selection only.

## `private void QSectionEditHandle(bool lEditable)`

The handler raises the enabled event first and then lets the gate handle the editable switch.
Test `EditableOff_DisablesCancelsAndClearsTheDrag` pins that losing editability disables the panel, cancels the edit and clears the drag.

## `public bool QSectionMinimizedSet(bool lMinimized)`

It returns whether the state changed, so a repeat raises no notice.

## `public void QSectionDeleteRun()`

The pending edit is committed first, so the delete sees the final names.
The flow's delete asks through the ask notice, so the result waits for the ask port.

## `public void QSectionClearRun()`

The pending edit is committed first, before the clear question is raised.

## `public string QSectionTitleRead()`

An empty list shows the plain title, and a held count shows the section count.
The gate returns no count for an empty list.

## `public static bool QSectionStepCheck(string lText)`

It is true for the one comma text, which steps between the name boxes.
The comparison is ordinal.

## `public static int QSectionCountRead()`

It returns the active palette size.
