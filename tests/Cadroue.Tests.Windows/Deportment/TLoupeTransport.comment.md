# TLoupeTransport.cs
Hash: `47884bd3ed4f6f76`

## `public sealed class TLoupeTransport`

Checks the loupe popup's transport and float mode without any window.
Most tests attach the loupe to a viewer and gives its player a stub engine seam.
The Preference collection keeps it from running beside tests that share the preference state.

## `public void FloatSet_WritesPreference_SelectsOneButton()`

The float mode is stored in the preference and decides ownership and topmost state.
Exactly one segment button reads as selected.
A new loupe restores the stored mode.

## `public void PlayToggle_RoutesThroughViewer_SyncsPlaying()`

The loupe does not play by itself.
The toggle goes through the viewer and the loupe follows the viewer's playing state.
The play face and the playing notice change with it.

## `public void Tick_AtEnd_StopsAndMarksEnded_SeekClears()`

Reaching the end stops playback and marks the loupe ended.
An ended loupe offers no resume.
Any seek clears the ended mark.

## `public void Close_ThenDetach_ReleasesPlayerAndViewer()`

Closing the loupe disposes its player but keeps the viewer's loupe link.
Detaching releases the viewer.
Neither step raises the close notice.
