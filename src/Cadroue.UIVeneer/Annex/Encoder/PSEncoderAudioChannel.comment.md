# PSEncoderAudioChannel.cs
Hash: `d4c025bf7a6ace44`

## `private void PSAudioChannelRebuild()`

Rebuilds the channel row with the layouts the selected encoder lists in its ffmpeg help.
Until that help is loaded, or when it lists none, a standard layout set stands in.
The first slider step means keeping the source layout, which the notice under the row explains.
A rebuild keeps the layout already chosen when the new list still contains it.

## `private string PSAudioChannelRead()`

Returns the chosen layout name, or the source token for the first step.
