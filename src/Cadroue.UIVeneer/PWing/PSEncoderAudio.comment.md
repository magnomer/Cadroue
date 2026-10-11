# PSEncoderAudio.cs
Hash: `4622eec81052916c`

## `private void PSAudioEncoderUpdate()`

Shows the unavailable notice when the chosen audio encoder failed the last audio verification.
Before any verification, an encoder counts as unavailable when ffmpeg does not list it as installed.

## `private void PSAudioContainerHandle()`

Refills the available audio encoders that suit the current container.
The current choice is kept when it still fits, otherwise the first item is selected.

## `private UIElement PSAudioPlateBuild()`

Builds the audio plate with stream, mode, encoder and rate-control rows.
Encoder and rate changes go to LSEncoder, whose audio notice rebuilds the dependent rows.
The Result button opens the verification table and stays disabled until a verification has results.

## `private void PSAudioRowsApply()`

Answers the LSEncoder audio notice after the encoder or rate control changed.
The rate combo gets a new list only when the labels differ, which spares a needless selection reset.

## `private void PSAudioScopeUpdate()`

Encoder rows show only when audio is encoded.
Excluding the stream or copying it hides them and shows a notice naming which case applies.
