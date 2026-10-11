# TInterfacePorch.cs
Hash: `d1e6e568c8ba0a89`

## `internal static partial class TInterface`

The porch half of the test interface.
Tests reach the rail, chrome, workspace, surface, deck and strip deportments only through these forwards.
Each forward adds no logic.

## `internal static void TWorkspaceAttach(`

Attaches with no station and a default layout read.
So a test can give a workspace only the owners it needs and leave the rest null.

## `internal static LSceneTabEntry TSceneTabCreate(bool exportHidden, bool autoRelay, params int[] collapsed) => new()`

Builds a tab layout record with the given flags and collapsed panel indexes.
