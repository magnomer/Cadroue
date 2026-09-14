# LJobFile.cs

## `private bool LJobClaim(string pPath)`

Atomically claim a path so no other process (or job) can take the same name.
An exclusive create is the OS-level compare-and-swap that closes the choose-then-write race.
The 0-byte placeholder is the reservation.
The encode overwrites it (ffmpeg runs with -y -nostdin).
Any placeholder never written over is removed by `LJobReservedClear` once the job ends.

## `private void LJobReservedClear()`

Remove reservation placeholders the encode never wrote into.
A real output has bytes.
So an empty reserved file is a placeholder left behind by a failed, cancelled or skipped job.
Never touches a file that received content.

## Inline notes

### `if (Directory.Exists(pPath))`

A temporary stage may name a folder rather than a file: the two-pass log folder.
FFmpeg derives the log file names itself, so the folder is the only stable handle to remove.

### `bool pPreExisting = LJobCollisionCheck(pOutput, LJobInputsRead())`

Never delete a file this job did not create.
The recorded output can coincide with a user-owned file.
That is an input (source == output), or the pre-existing collision target the encode staged around (`lJobFinalPath`).
Preserve those.
