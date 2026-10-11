# LLog.cs
Hash: `2ee5bc6a5fb19391`

## `public bool LLogSnapshotCheck(long lSequence) => lSequence <= lLogSnapshot;`

An entry at or below the snapshot was already in the file read, so it is not appended twice.

## `public void LLogFilesUpdate(bool lFollowCurrent = false)`

Lists the log files with the current one always present, even before its first write.
The selection stays on the chosen file unless it vanished or the caller follows the current one.
An unchanged list raises nothing.

## `public void LLogFileSelect(int lIndex)`

Loads the chosen file into the rows.
For the live file the read also fixes the snapshot, and pending entries it covers are dropped.

## `public void LLogTick()`

Moves the window to a new current file when the writer rolled over while the live file was shown.
Pending live entries are appended only while the live file is shown.

## `public void LLogErrorRaise(string lMessageKey, string lDetail)`

Records a Copy failure as an error, then raises the localized notice.
A clipboard error has no engine owner, so the log window records it itself.
The trace text is plain English and names no message key.
The underlying error text follows the summary unchanged.

## `private void LLogNoticeRaise(string lMessageKey, string lDetail) =>`

Raises the localized notice for a failure its owner already recorded.
`LTraceWriter` records read and list failures, and `LUsher` records open failures.

## `private bool LLogCategoryCheck(LLogRow lRow) =>`

An empty category filter shows every row.

## `private void LLogExcessRemove()`

Keeps at most 5000 rows by dropping the oldest.
The removed count reports only shown rows, so the window trims exactly those.

## Inline notes

### `if (!lFeedViewer || lExtentChange != 0 || lScrollable is null || lOffset is null)`

Adding rows increases the extent before ScrollIntoView reaches the new tail.
Preserve the prior follow state during that intermediate layout event.
