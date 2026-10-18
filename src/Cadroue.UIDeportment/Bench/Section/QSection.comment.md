# QSection.cs
Hash: `2c744f76abcc425e`

## `public sealed record QSectionRow(`

The strings and flags one section row shows, so the veneer draws without reading the list.
The unnamed flag lets the veneer style the placeholder text differently from a real name.

## `public sealed class QSection`

Owns the section panel state of one tab on top of its flow.
Rows, selection and edits reach the flow's section sibling, and this class keeps only panel facts.

## `public QSection(QFlow lOwner)`

Takes a copy of the current list and subscribes the section and edit notices of the flow.

## `public QSectionDrag QSectionDrag { get; }`

The drag sibling, which this class clears before any action that could invalidate its row indexes.

## `public int QSectionRowCount => qSectionRowCount;`

The count the veneer last rebuilt for, which differs from the list while a drag defers the rebuild.

## `public int QSectionEditSerial => qSectionEditSerial;`

Grows on every edit start, so a late blur from a previous editor can be recognized and ignored.

## `public static int QSectionCountRead() => LSectionPalette.LSectionActiveCount;`

The active palette size, which also bounds the color index of new sections.

## `public static bool QSectionStepCheck(string lText) =>`

True for the one text that steps between the three name boxes.

## `public string QSectionTitleRead() =>`

Shows the plain title for an empty list and the section count otherwise.

## `public IReadOnlyList<QSectionRow> QSectionRowsRead() =>`

Builds a fresh record per piece, with selection and editing resolved at call time.

## `public bool QSectionMinimizedSet(bool lMinimized)`

Returns whether the state changed, so a repeat raises no notice.

## `public void QSectionToggleHandle(int lIndex)`

Commits any open edit first, then toggles only when the tab is editable.

## `public bool QSectionSeekHandle(int lIndex, int lClickCount, bool lEnd)`

Acts on a double click only and returns whether it consumed the gesture.
Seeking stays allowed on a locked tab.

## `public bool QSectionRenameHandle(int lIndex, int lClickCount)`

Acts on a double click only, and only on an editable tab.
It selects the row before the edit starts.

## `public void QSectionEditStart(int lIndex)`

Copies the name and affixes into edit buffers and bumps the serial before the rebuild.

## `public void QSectionTextSet(string lName, string lPrefix, string lSuffix)`

Keeps the box texts in buffers without touching the flow until commit.

## `public void QSectionEditCommit()`

Does nothing without an open edit.
The index is cleared before the write, so the rebuild shows the row as no longer edited.
Texts are trimmed.

## `public void QSectionEditCancel()`

Discards the buffers and rebuilds the rows.

## `public bool QSectionKeyRun(string lKey)`

Return commits, Escape cancels, and the result says whether the key was consumed.

## `public void QSectionBlurHandle(int lSerial, bool lFocusInside)`

Commits only for the current editor and only when focus left all three boxes.

## `public void QSectionDeleteRun()`

Commits the open edit so the delete sees the final names.

## `public void QSectionSortRun()`

Commits the open edit so the sort sees the final names.

## `public void QSectionClearRun()`

Commits the open edit before the clear question is raised.

## `private void QSectionUpdateHandle(IReadOnlyList<LPiece> lPieces, int? lSelected)`

An identical list only refreshes selection, which avoids rebuilding rows under the pointer.
While a drag is active nothing is raised, and the drag end rebuilds.

## `private void QSectionEditHandle(bool lEditable)`

Losing editability ends any drag and cancels an open edit.

## `public void QSectionRebuildRaise()`

Records the row count the veneer is about to rebuild for.

## `private QSectionRow QSectionRowCreate(LPiece lPiece, int lIndex)`

An unnamed piece shows a numbered default name.
A negative span is shown as zero.

## `private static string QSectionTimeFormat(TimeSpan lTime) =>`

Shows hours only when the time reaches one hour.
