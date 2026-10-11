# TPlayerRequest.cs
Hash: `a4df13f7bf316c18`

## `public sealed class TPlayerRequest`

Checks the player deportment that sits between the viewer and an engine seam.
It covers seek coalescing, filter bookkeeping and the background open.
Tests use a fake seam whose members record calls, so no engine is needed.

## `public void Accurate_ReportsPriorRun_UntilCommit()`

Setting the accurate flag reports whether a seek was already running.
This lets a caller skip a duplicate request.
A completed seek clears the flag, and reset clears it explicitly.

## `public void Renderer_ResolvesOnFirstCompletedSeek_Only()`

The renderer-pending flag resolves on the first seek that completes with a non-negative result.
A failed seek keeps it pending.
Later seeks never resolve it again.

## `public async Task OpenStart_RunsOpen_WithPath()`

The open runs on a background task and receives the requested path.

## `public async Task OpenStart_PropagatesFailure()`

An engine failure surfaces through the returned task.
The caller decides how to report it.

## `public void Applied_TracksFilter_ResetClears()`

Applying the filter string already in effect does not reach the engine again.
Reset forgets both applied strings so the next apply goes through.

## `public void Seek_ClampsToVideoEnd_WhenKnown()`

Once a video end is known, a seek past it lands on the end.
Before that, the requested time passes through unchanged.

## `public void EngineSet_DisposesPrevious_AndResetsApplied()`

Replacing the engine disposes the old one and forgets the applied filters.
Clearing the engine leaves the player not ready, with zero time and no ended state.

## `public void PreviewApply_HandsResolvedValues_ContrastOnlyWhenFlyleafActive()`

Brightness always reaches the seam.
Contrast reaches the seam only while the local Flyleaf library is loaded, otherwise it is zero.
