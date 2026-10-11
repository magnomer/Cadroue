# TViewerMediaStatus.cs
Hash: `a68bc560afb09e30`

## `public sealed class TViewerMediaStatus`

Checks the viewer deportment media state and its notices without any control or engine.
The Logging collection keeps it from running beside tests that read the shared trace.

## `public void MediaCommit_SetsSource_StopsPlayback_ClearsEnd()`

Committing new media starts a clean playback state.
The source, info, duration and video presence come from the cargo.

## `public void SourceMatch_RequestedPath_WhilePendingOrLoaded_NotAfterFailureOrClose()`

The match answers whether a requested path is already loaded or pending.
The path is compared case insensitively as it was requested, before any sidecar resolve.
A failed load and a close release the claim.

## `public void MediaCommit_CropPersistent_KeepsCropbox()`

A crop box survives a new media commit only when the crop is persistent.

## `public void MediaRaise_ThrowingSubscriber_RecordsAndContinues()`

One failing subscriber must not stop the others or escape into close teardown.
The failure is recorded to the trace with the exception text.

## `public void Bypass_SilencesFilter_NoticesOnce()`

Bypass makes the resolved audio filter empty while the stored filter is kept.
A notice is raised only when the bypass value actually changes.

## `public void Playing_NoticeOnlyOnFlip_PositionKept()`

The playing notice fires only when the flag flips.
A null argument leaves the matching value unchanged.

## `public void Engine_NoticeOnlyOnChange()`

Setting the engine the viewer already uses raises nothing.
