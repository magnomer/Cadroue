# LMediaLoader.cs
Hash: `3d1a26841817c6fc`

## `public sealed class LMediaLoader : IDisposable`

Owns the backend lifecycle of the current media source.
A load is one operation: validation, probing, cancellation, ordering, and committing current identity happen here.

## `public bool LMediaLoadTail { get; set; } = true;`

Whether the probe also scans for the last video frame.
The viewer turns it off under mpv, which finds the end itself.

## `public async Task<LMediaLoadOutcome> LMediaLoadStart(`

A new load cancels the one in flight, and only the newest request may commit.
A result overtaken by a later load or close comes back obsolete and raises no notice.
Only a successful probe replaces the current path and info.

## `public static string? LMediaPathResolve(string lMediaPath)`

Returns the full path, an empty string for a blank path, or null for a malformed one.

## `public bool LMediaLoadClose()`

Cancels any pending load and forgets the current media.
It raises the unloaded notice only when there was something to forget.

## `private LMediaLoadOutcome LMediaLoadCancel(string lMediaLoadPath, string lMediaLoadError)`

Fails a request whose path is malformed.
The load in flight is cancelled too, since the user asked for a different file.

## `private void LMediaLoadRaise(LMediaLoadOutcome lMediaLoadOutcome)`

A throwing handler is recorded through the error seam and the rest still hear the notice.
