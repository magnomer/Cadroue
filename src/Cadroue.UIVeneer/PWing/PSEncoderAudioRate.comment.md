# PSEncoderAudioRate.cs
Hash: `5f6de38db4ce1fd7`

## `private void PSAudioRowsRebuild()`

Rebuilds the rows under the audio rate control from the encoder's capability record.
Stored values are reused only when the stored rate control matches the selected one.
Otherwise a quality saved for another mode could land in the wrong scale.

## `private void PSAudioQualityBuild(LCapabilityMode pMode, bool pModeStored)`

A mode with known bounds gets a slider beside the box, and others get the box alone.
The notice names the ffmpeg option and its range so the value can be checked against ffmpeg.

## `private void PSAudioExtraBuild(LCapabilityCodec pCodec)`

A stored extra value that is no longer a listed choice falls back to the default.
