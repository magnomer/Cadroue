# PCompass.cs
Hash: `ff5275c515fb7083`

## `public sealed class PCompass : UserControl`

The transport and volume cluster of a tab.
It builds one button per record of the Deportment and keeps faces keyed by button key.

## `private sealed record PCompassFace(Button PCompassFaceButton, Image PCompassFaceImage, TextBlock PCompassFaceLabel);`

The three controls of one button face, kept so an update can rewrite them in place.

## `private static readonly IReadOnlyDictionary<string, Brush> pCompassAccents = new Dictionary<string, Brush>`

Maps accent tokens to brushes.
An unlisted token, including the empty one, gives no tint.

## `private static readonly IReadOnlyDictionary<bool, Brush> pCompassWaveformBrushes = new Dictionary<bool, Brush>`

Gives the waveform icon a blue tint while the waveform shows and a grey one otherwise.

## `private readonly IReadOnlyList<StackPanel> pCompassSectionGroups;`

The group panels that belong to section editing, enabled and disabled together.

## `public QCompass QCompass { get; }`

The Deportment this view forwards to.

## `public PCompass(PFlow pFlow, PViewer pViewer, bool pCompassSectionShow = false)`

The volume group is appended after the table groups and is not part of the Deportment's table.
The initial edit, volume and waveform states are applied before the first layout.

## `private StackPanel PCompassGroupBuild()`

Every group begins with a separator, which the size handler hides at the start of a wrapped line.

## `private Button PCompassButtonBuild(QCompassButton lButton)`

Registers the face by key before applying the record.
The click forwards only the key.

## `private void PCompassFaceApply(QCompassButton lButton)`

Rewrites icon, label and tooltip of a registered face.
This lets the play button change its look while it is playing.

## `private void PCompassEditApply(bool lEditable) =>`

Disables the section groups while the flow forbids section editing.

## `private void PCompassSizeHandle(object sender, SizeChangedEventArgs e) =>`

Reads each group's top in the wrap panel and applies the opacities the Deportment resolves.

## `private Border PCompassVolumeBuild()`

Draws the track as a base bar, a fill bar and a transparent slider over them.
This way the filled part can be colored, which the stock template cannot do.

## `private void PCompassValueHandle(double lVolume)`

Takes a volume that changed elsewhere and moves the slider to it.
The slider handler then runs, and the Deportment's compare stops the echo.

## `private void PCompassVolumeHandle()`

Shows the dragged value at once, then sends it to the Deportment.

## `private static Style PCompassSliderBuild()`

Parses a slider template with an invisible track and a round thumb.
`PART_Track` is the name the stock slider requires.
