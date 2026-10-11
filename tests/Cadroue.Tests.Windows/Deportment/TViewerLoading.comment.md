# TViewerLoading.cs
Hash: `99a1f62fd308276e`

## `public sealed class TViewerLoading`

Checks the viewer media sibling that loads a source into the player and commits the resulting cargo.
Each test builds a viewer with an active command and a fake engine seam that records opened paths.
The engine seam can be told to refuse the open.
The Logging collection keeps it from running beside tests that share the trace writer.

## `public async Task LoadStart_MissingFile_CommitsFailureCargo()`

A path that does not exist never reaches the engine.
The viewer commits one cargo that carries the error and clears the intent.

## `public async Task LoadStart_CommandInactive_DoesNothing()`

A viewer whose command is inactive ignores a load request.
Even the load serial stays where it was.

## `public async Task FlyleafApply_OpensCreatedEngine_AndCommitsPreview()`

A successful apply opens the path on the engine the creation callback provides.
The viewer shows the host, takes the media info and clears the intent.

## `public async Task FlyleafApply_OpenThrows_CommitsPreviewError_NoEngine()`

An engine refusal must not leave a half-ready player.
The cargo reports the refusal text, while the probed media info is still kept.

## `public async Task FlyleafApply_StaleSerial_DiscardsWithoutCommit()`

A newer load supersedes an older one still in flight.
The stale result commits nothing and leaves the player not ready.

## `public async Task FlyleafApply_AudioOnlyRefused_WhenTabForbids()`

A tab that does not allow audio-only media gets a refusal cargo for an audio file.
The engine is never opened.

## `public async Task MediaClose_Force_DisposesEngine_RaisesEmptyCargo()`

An inactive command blocks a plain close, and only a forced close tears the media down.
The forced close raises an empty cargo and resets the crop.
A second forced close finds nothing and reports false.

## `public void LoadCancel_ClearsIntent_AdvancesSerial_OnlyWhenPending()`

Cancelling invalidates the in-flight load by advancing the serial.
With no pending intent it does nothing and reports false.

## `public async Task CommandApply_Off_SuspendsPlaying_On_ResumesIt()`

Deactivating the command pauses playback and remembers that it must resume.
Reactivating resumes playback and restarts the clock.

## `public void Close_Unloads_AndIgnoresLaterCalls()`

Close marks the viewer unloaded and invalidates pending loads.
Later media closes become no-ops.
