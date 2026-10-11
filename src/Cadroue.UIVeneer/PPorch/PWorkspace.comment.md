# PWorkspace.cs
Hash: `a6fd5a63eb89340e`

## `public sealed class PWorkspace`

Builds one tab's surface, root and the veneer half of its workspace wiring.
The deportment `LWorkspace` decides, and this class answers its notices on the child controls.

## `private static readonly IReadOnlyDictionary<string, Func<LPresetSelection, LSceneTabEntry?, PTabSurface>>`

Maps a tab key to the factory of its surface.
An unknown key throws at construction, so every registered tab layout needs an entry here.

## `private static readonly IReadOnlyDictionary<bool, Func<PWorkspace, FrameworkElement>> pWorkspaceRoots =`

Keyed by whether the tab has a media source.
A source tab gets the grid with the source box and info row, any other tab is its bare surface.

## `private static readonly IReadOnlyDictionary<bool, Action<PWorkspace>> pWorkspacePairCloses =`

Keyed by whether the flow and viewer pair exist.
Tabs without the pair close nothing extra.

## `private static readonly IReadOnlyDictionary<bool, Action<PWorkspace>> pWorkspaceLists =`

Keyed by whether the tab has a list.
Only a list tab forwards path and clear notices to the workspace.

## `public PWorkspace(`

Order matters.
The deportment is created and attached to the tab before the surface, which reads its preset owner.
The viewer allowance is set before the workspace attaches the pair.
The root is built last, once the surface and its children exist.

## `public FrameworkElement PWorkspaceRoot { get; }`

The element the deck places in its grid.
It is the surface itself or a grid wrapping it.

## `public void PWorkspaceWidthAttach(Action pHandler) => PWorkspaceWidthChanged += pHandler;`

Lets the toolbar follow the surface's column width without holding the surface.

## `public void PWorkspaceCommandApply(double pFlowHeight)`

Turns on the command state of flow and viewer together.
The section strip follows the key facts of the tab, so a tab without sections never shows it.

## `public void PWorkspaceCommandReset()`

Turns the command state of flow and viewer off again.
The viewer drag flag is cleared first, so no seek is left in flight.

## `public void PWorkspaceClose()`

Each teardown step runs on its own, so one failure cannot skip the rest.
Subscriptions are released first, so no late notice reaches a half-closed workspace.

## `private static void PWorkspaceStepRun(string pStepName, Action pStep)`

A failing step is recorded to the trace and swallowed.
The close always reaches the last step.

## `private async void PWorkspacePathsAdd(IReadOnlyList<string> lPaths)`

The source run starts only after the list has finished adding.
Running it earlier would see an incomplete docket.

## `private void PWorkspaceLosslesscutDefer() =>`

The sidecar search runs at background priority.
So the media load that raised the notice finishes drawing first.

## `private void PWorkspaceRelayDefer(LRelay lRelay, TimeSpan lDuration) =>`

A relayed tab is restored at background priority.
This lets the new surface finish loading before its state is applied.

## `private static FrameworkElement PWorkspaceGridBuild(PWorkspace pWorkspace)`

Stacks the source box, the info row and the surface.
The surface takes the remaining height, the first two rows size to their content.

## `private UIElement PWorkspaceInfoBuild(PInfo pInfo)`

The info panel fills the row, and the export toggle sits at its right edge.
