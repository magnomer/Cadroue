# QWorkspacePair.cs
Hash: `73ac3d0606c61883`

## `public static class QWorkspacePair`

Wires a tab's flow and viewer to each other for playback, cursor, drag and volume.
It holds no state, because the workspace owns both ends.

## `public static void QWorkspacePairAttach(QFlow? lFlow, QViewer? lViewer)`

Connects the pair when both exist, and does nothing for a tab missing either.

## `public static void QWorkspacePairDetach(QFlow? lFlow, QViewer? lViewer)`

Undoes every subscription the attach made and clears the flow's playing source.
