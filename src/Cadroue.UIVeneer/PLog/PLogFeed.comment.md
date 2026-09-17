# PLogFeed.cs

## Inline notes

### `if (e.ExtentHeightChange != 0)`

Adding rows increases the extent before ScrollIntoView reaches the new tail.
Preserve the prior follow state during that intermediate layout event.
