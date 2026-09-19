# LLog.cs

## Inline notes

### `if (!lFeedViewer || lExtentChange != 0 || lScrollable is null || lOffset is null)`

Adding rows increases the extent before ScrollIntoView reaches the new tail.
Preserve the prior follow state during that intermediate layout event.
