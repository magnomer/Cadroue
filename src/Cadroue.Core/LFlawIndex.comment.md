# LFlawIndex.cs

## Inline notes

### `if (LFlawSequentialCheck(lFlawIndexedError))`

No demuxer line names the index.
Yet a boundary seek can still fail on a file that reads cleanly front to back.
Random access failing over a clean sequential read is itself the addressing defect, whatever words the demuxer chose.
A file whose sequential read already errors belongs to whichever container or coded detector owns that error, not here.
