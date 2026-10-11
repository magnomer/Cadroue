# PViewer.cs
Hash: `20d1012e72de6279`

## `public sealed class PViewer : PPanel`

The viewer panel holds the controls only, and LViewer and its siblings hold every fact.
Delete this file and the program still knows the source, the engine and the play state.

## `private static readonly IReadOnlyDictionary<bool, Action<Config>> pViewerProcessors =`

Maps the forced-processor fact to the Flyleaf config writes, so no branch decides it here.

## `private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerOverlayMoves =`

Moves the overlay host between the Flyleaf host and the mpv popup by the active engine.
The host is first taken from its old parent, because a WPF element has one parent.

## `private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerEngineCreates =`

Creates the engine wrapper LViewer asks for, keyed by the mpv-active flag.

## `private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerLoupeShows =`

Activates the open loupe or opens a new one, by the loupe flag LViewer keeps.

## `private static readonly IReadOnlyDictionary<LViewerAskKind, Action<PViewer, LViewerAsk, Action<bool>>> pViewerAsks =`

Shows the modal an ask kind needs and hands the answer back to LViewerSource.
A warning offers no choice, so it always answers no.

## `private static readonly IReadOnlyDictionary<bool, Func<Microsoft.Win32.OpenFileDialog, string?>> pViewerLocates =`

Turns the dialog result into a path or null, so a cancelled locate answers null.

## `public PViewer(bool pAudioEligible = false, bool pEditEligible = false, bool pColorPreview = false) : base("")`

Builds both engine hosts once and collapses the one LViewer does not show.
The picker redraw runs at once while the engine handle is queued, so the cards show the new choice first.
The host trace lines are written by LViewerMedia from plain strings the events hand over.

## `private static Button PViewerButtonBuild(string pIconPath, string pTipKey, double pRightMargin)`

Builds one corner button with its icon and localized tooltip.
The button starts collapsed, and the host apply shows it when the host is visible.

## `private static Border PViewerSlotBuild()`

Builds an empty top-left slot that one engine picker card is placed into.

## `private void PViewerEngineShow()`

Rebuilds both engine pickers from one plan, so the surface and overlay cards always agree.

## `private void PViewerEngineHandle() => Dispatcher.BeginInvoke(LViewer.LViewerRenderer.LViewerEngineHandle);`

The engine notice can arrive off the UI thread, so the engine handle is marshalled onto the dispatcher.

## `private void PViewerHostApply()`

Applies the host facts LViewer resolves, through the visibility table.
The Flyleaf windows are made inert after each apply, so they never take activation from the main window.

## `private void PViewerFlyleafCreate()`

Builds the Flyleaf player config and hands its seam to LPlayer.
The Flyleaf key bindings are cleared, so every shortcut reaches the program's own dispatch.

## `private LViewerHostFacts PViewerFactsRead()`

Reads the raw host facts for the host trace, and decides nothing about them.
A disposed Flyleaf host can throw on read, so the facts fall back to empty values.

## `internal bool PViewerSurfaceMatch(nint pForeground)`

Hands the Flyleaf window handles to LViewer, which decides whether the foreground is one of them.
A disposed host can throw on read, so the answer is then no.

## `public void PViewerClose()`

Tears down the Deportment first, then unhooks the engine notice and the controls.
Each host disposal runs as a QViewerTeardown stage, so one failure never blocks the other.
The stage shares the teardown's trace text and error record, so this file names no trace type.

## `private void PViewerFlyleafDispose()`

Detaches the player and the overlay content before the dispose, so the host releases only itself.
