# TWindowShortcut.cs
Hash: `af2ae520347bd805`

## `public sealed class TWindowShortcut`

Checks the window deportment's shortcut gate and the dispatch of a bound token.
Key presses are built as plain records with injected foreground, input-focus, key-name and key-state reads.
No real window message or control is involved.

## `public void Run_DispatchesTokensToTheAttachedSeams()`

Each token reaches the seam attached for it.
A seam that returns null or false makes the run report false.
Tokens that need a flow, such as zoom, run only once a tab with a flow is attached.
Unknown tokens report false.

## `public void PlayPause_NeedsAViewer_AndTogglesByState()`

Play and pause do nothing without a viewer.
With one, the token asks for play while the viewer is not playing.

## `public void Handle_GatesOnMessageModalFocusAndInput()`

A press is consumed only when it is a key-down on this window with no modal open.
A foreground window other than this one or a focused text input lets the key pass.
An already handled event is reported as handled without running anything.

## `public void Handle_ResolvesTheGestureAndRunsTheBoundToken()`

The key name resolves to a gesture and the bound token runs.
An empty key name or a held modifier key state does not run.
A foreground window accepted by the viewer lookup also lets the key run.
