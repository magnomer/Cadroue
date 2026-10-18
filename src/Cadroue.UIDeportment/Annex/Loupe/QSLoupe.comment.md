# QSLoupe.cs
Hash: `3a9c9176b2689c09`

## `public void QSLoupeFloatRestore() =>`

Reads the float mode from the preference.
An unknown or missing token falls back to owner float, the default for a fresh loupe.

## `public void QSLoupeViewerAttach(QViewer lViewer)`

Binds the loupe to the viewer it mirrors.
The viewer keeps the transport, and the loupe only follows its play, pause, seek and volume notices.
Binding also hands playback to the loupe, which hides the docked host.

## `public async Task QSLoupeStart()`

Creates the loupe engine, then opens the viewer's source on it.
The loupe inherits the viewer's preview, volume, position and play state once the open lands.
A close during the open leaves the new player untouched.
A failed open leaves the loupe paused rather than throwing into the window.

## `public void QSLoupeClose()`

Unbinds the viewer and disposes the loupe player.
The final position and resume state are kept first, so the detach can hand them back.

## `public void QSLoupeDetach()`

Returns the final position and play state to the viewer, so playback continues where the loupe stopped.

## `public void QSLoupePlayToggle()`

Routes the toggle through the viewer, so the viewer and the loupe never disagree on play state.

## `public void QSLoupePlay()`

Restarts from the beginning when the media had ended.

## `public void QSLoupeTick()`

Detects the end of media and reports the loupe position back to the viewer while playing.

## `public void QSLoupePlayingSet(bool lPlaying)`

Every play state change also syncs the viewer's position, unless the loupe is closed.
