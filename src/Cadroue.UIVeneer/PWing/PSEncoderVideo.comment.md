# PSEncoderVideo.cs
Hash: `cb1c714df84d4f8b`

## `private UIElement PSVideoPlateBuild()`

Builds the video plate with mode, encoder, rate-control, size, frame-rate and pixel rows.
Encoder and rate changes go to LSEncoder, whose video notice rebuilds the dependent rows.
The Result button opens the verification table and stays disabled until a verification has results.

## `private void PSVideoScopeUpdate()`

Encoder rows show only for the Encode mode.
Copy and Smart hide them and show the notice LSEncoder picks for that mode.

## `private void PSVideoRowsApply()`

Answers the LSEncoder video notice after the encoder or rate control changed.
The rate combo gets a new list only when the labels differ, which spares a needless selection reset.

## `private void PSVideoRowsRebuild()`

Rebuilds the rows under the video rate control from the encoder's capability record.
Stored values are reused only when the stored rate control matches the selected one.
Otherwise a quality saved for another mode could land in the wrong scale.

## `private void PSVideoSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)`

Speed values are registered from fastest to slowest.
The slider reads slowest on the left and fastest on the right.
So the slider position is the choice index mirrored around the last step.

## `private void PSVideoExtraBuild(LCapabilityCodec pCodec, LCapabilityMode pMode)`

An extra option that the rate-control mode lists as a conflict gets no row.
A stored extra value that is no longer a listed choice falls back to the default.
