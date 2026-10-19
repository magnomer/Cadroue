# PSEncoderVideo.cs
Hash: `cb1c714df84d4f8b`

## `private UIElement PSVideoPlateBuild()`

Builds the video plate with mode, encoder, rate-control, size, frame-rate and pixel rows.
Encoder and rate changes go to QSEncoder, whose video notice rebuilds the dependent rows.
The Result button opens the verification table and stays disabled until a verification has results.

## `private void PSVideoScopeUpdate()`

Encoder rows show only for the Encode mode.
Copy and Smart hide them and show the notice QSEncoder picks for that mode.

## `internal void PSVideoRowsApply()`

Answers the QSEncoderVideo notice after the encoder or rate control changed.
The rate combo gets a new list only when the labels differ, which spares a needless selection reset.

## `internal void PSVideoRowsRebuild()`

Lives in PSEncoderVideoRate and rebuilds the rows under the video rate control from the rows QSEncoderVideo shapes.
QSEncoderVideo reuses stored values only when the stored rate control matches the selected one.
Otherwise a quality saved for another mode could land in the wrong scale.

## `private void PSVideoSpeedBuild(QSEncoderSpeed? pSpeed)`

Speed values are registered from fastest to slowest.
The slider reads slowest on the left and fastest on the right.
So the slider position is the choice index mirrored around the last step.

## `private void PSVideoExtraBuild(IReadOnlyList<QSEncoderExtra> pExtras)`

An extra option that the rate-control mode lists as a conflict gets no row.
A stored extra value that is no longer a listed choice falls back to the default.
