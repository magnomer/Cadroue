# QViewerLoupe.cs
Hash: `946856d9f911d3c0`

## `public sealed class QViewerLoupe`

Owns the hand-over of playback between the docked viewer and the loupe.
It holds no state, so every flag it changes lives on the viewer.
The host show and the command apply stay on the media owner, which this sibling calls.

## `public void LViewerLoupeAttach()`

Hands playback to the loupe by hiding the host and dropping the viewer's command.

## `public void LViewerLoupeDetach(TimeSpan lPosition, bool lPlaying)`

Takes playback back from the loupe at the position and play state it ended with.
