# PSEncoderAudioSample.cs
Hash: `3db2df19d84072c3`

## `private void PSAudioSampleRebuild()`

Rebuilds the sample-rate row for the selected audio encoder.
An encoder that lists its rates gets those as discrete steps.
Other encoders get standard rates as tick marks on a slider that does not snap.
The notice under the row explains that the source step keeps each input's rate.

## `private string PSAudioSampleRead()`

Any text that is not a positive whole number reads as keeping the source rate.
