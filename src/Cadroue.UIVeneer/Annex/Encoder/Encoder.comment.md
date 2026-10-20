# Encoder

The XAML files provide named WPF scaffolding for another layer to populate and drive.

Each of the eight public code-behind types only initializes its component.
They store no application state and register no event handlers here.

## PSEncoder

`PSEncoder` is the encoder window shell.

The action row remains outside the tab content's scroll viewers.
Scrolling a form therefore does not move its Apply, OK, and Cancel controls.

`psEncoderOutput`, `psEncoderPicture`, and `psEncoderAudio` are the three named surfaces hosted by the window.

## PSEncoderOutput

The output surface composes the separate `PSEncoderName` control with named location, folder, container, extension, collision, and suffix fields.

Its folder and suffix rows are named as complete rows, while their child controls remain named individually.

The browse button's ellipsis is the only literal field content in this surface.

## PSEncoderName

The name surface separates the editable `psNameBox` from the initially empty `psNameElements` host.

## PSEncoderPicture

The picture surface leaves `psEncoderModeBar` empty.
`psEncoderPictureRows` groups codec, control, rate, resolution, framerate, and pixel controls.

Codec verification has separate action, result, progress, and error elements.
The progress and error elements start collapsed.

`psEncoderModeHint` also starts collapsed, independently of `psEncoderPictureRows`.

## PSEncoderAudio

The audio surface leaves `psAudioModeBar` empty and groups codec, control, rate, sample, and channel controls beneath `psAudioRows`.

Codec verification has separate action, result, progress, and error elements.
The progress and error elements start collapsed.

`psAudioNotice` starts collapsed after the grouped rows.

## PSEncoderRate

The rate surface defines keyed caution, speed, and extra templates.

The live surface keeps `psEncoderRateFader` inside `psEncoderRateQuality` and leaves `psEncoderRateRows` empty.

## PSEncoderResolution

The resolution surface keeps the tier control, initially empty dimension-row host, hint, and orientation control as separate named contracts.

`psEncoderResolutionDimension` is the keyed shape for a dimension row.

## PSEncoderFramerate

The framerate surface pairs a tick-snapped slider with an editable value box and keeps its wrapped hint below that row.
