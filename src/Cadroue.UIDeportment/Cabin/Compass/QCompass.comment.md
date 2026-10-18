# QCompass.cs
Hash: `78bd5363e0a0a618`

## `public sealed record QCompassButton(`

One transport button with its icon, localized label and tooltip, and accent key.
The accent is a token the veneer maps to a brush, never a color.

## `public sealed record QCompassGroup(IReadOnlyList<QCompassButton> QCompassGroupButtons, bool QCompassGroupSection)`

One visual cluster of buttons.
The section flag marks clusters that only make sense when the tab shows the section band.

## `public sealed class QCompass`

Owns the transport button table and the volume arithmetic for one tab's compass.
It holds no changing state and reads playing and volume from the viewer.

## `public const string QCompassPlayKey = "Play";`

The one key the compass handles itself, because play and pause depend on the viewer.
Every other key is forwarded to the flow as a shortcut token.

## `public QCompass(QFlow lFlow, QViewer lViewer, bool lSectionShown)`

Fixes whether section clusters exist for the lifetime of this compass.

## `public bool QCompassEditActive => qCompassFlow.QFlowEdit.QFlowSectionEditable;`

Section buttons enable from the flow's editability, so a lock or a detection run disables them.

## `public IReadOnlyList<QCompassGroup> QCompassGroupsRead() =>`

Section clusters are dropped when the tab hides the section band.
Labels are resolved here so the veneer never touches the catalog.

## `public IReadOnlyList<int> QCompassSectionRead() =>`

Indexes are positions in the filtered group list, not in the full table.
This lets the veneer enable or disable section clusters after they were built.

## `public static QCompassButton QCompassPlayRead(bool lPlaying) =>`

The play button swaps icon, text and accent while playing, so the face follows the viewer.
The key stays Play in both states so a click always reaches the same toggle.

## `public void QCompassRun(string lKey)`

Play toggles by the viewer's current state, never by the clicked face.
Any other key is a flow shortcut token.

## `public void QCompassVolumeSet(double lRaw)`

Clamps first and skips an equal value, so slider echoes never write the viewer back.

## `public static string QCompassVolumeFormat(double lRaw) =>`

Shows the clamped volume as a whole number.

## `public static double QCompassFillResolve(double lHostWidth, double lRaw)`

Returns the filled track width for the clamped volume.
An unmeasured host yields zero rather than a negative width.

## `public static IReadOnlyList<double> QCompassSeparatorsResolve(IReadOnlyList<double> lGroupTops)`

A separator is hidden when its group starts a wrapped line, since a line edge needs no divider.
Group tops that equal the current line top stay on the same line.
