# TViewerEngine.cs
Hash: `a776c21b14a7bb7d`

## `public sealed class TViewerEngine`

Checks the viewer renderer sibling that decides which preview engine is shown and what its switch offers.
Each test uses a viewer with an active command and no real engine.
The Logging collection keeps it from running beside tests that share the trace writer.

## `public void EngineRead_MarksTheActiveChoice_AndMpvAvailability()`

The plan always carries both engine choices with their tokens, text and tips.
Exactly one of them is marked active.
Flyleaf is always enabled.

## `public void EngineSelect_SameEngine_LeavesPreferenceAlone()`

Selecting the engine that is already active must not write the preference.

## `public void EngineUpdate_NoChange_ReportsFalse_AndKeepsHost()`

An update that finds the same engine reports false and leaves the host untouched.
The veneer's host apply is not called.

## `public void OverlayHandle_ShownOnlyForVisibleSizedMpvHost()`

The mpv overlay needs the mpv engine, a visible host, a shown flag and a non-empty size.
Hiding the host removes it again.

## `public void SwitchRead_FollowsBypass_AndEngine()`

The switch text depends on whether the filters are bypassed.
The switch is enabled only when the mpv engine can apply audio.

## `public void SurfaceMatch_ZeroForegroundNeverMatches()`

A zero foreground handle never counts as the viewer surface.
A foreground equal to either of the two known handles does.
