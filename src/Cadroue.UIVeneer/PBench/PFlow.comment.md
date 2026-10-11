# PFlow.cs
Hash: `34937c8eaa7ff1db`

## `public sealed class PFlow : UserControl`

The timeline container, which hosts the map and viewfinder reels and the height divider.
It constructs the flow and keeps only controls, timers and modal hops.

## `private static readonly IReadOnlyDictionary<bool, int> pFlowConfirmChoices = new Dictionary<bool, int>`

Maps a confirm result onto the flow's answer codes, where yes is primary and no is dismiss.

## `private static readonly IReadOnlyDictionary<PSDecisionChoice, int> pFlowSelectChoices =`

Maps the three-way decision result onto the same answer codes.

## `private readonly IReadOnlyDictionary<LFlowLosslesscutKind, Action<LFlowLosslesscutPrompt, Action<int>>>`

Chooses the modal by prompt kind, so no branch decides it.

## `private readonly DispatcherTimer pKeyframeRequestTimer = new() { Interval = LFlowKeyframe.LFlowRequestDelay };`

The debounce timer.
The flow decides when it starts and stops.

## `private readonly DispatcherTimer pKeyframeResumeTimer = new() { Interval = LFlowKeyframe.LFlowResumeDelay };`

The timer that restarts scanning after a suspend, with the delay set again on each resume.

## `public LFlow LFlow { get; } = new();`

The Deportment of this timeline, shared with the map, viewfinder and compass.

## `public PFlow()`

Builds the reels as a five-row grid where the map and viewfinder rows are swapped by the flow's order.
Height starts from the saved frame.
Keyframe and waveform notices are marshaled to the dispatcher.
Import prompts open their modal at once, since the flow waits for the answer.

## `public void PFlowClose()`

Closes the flow, stops both timers and detaches the divider and the name popup.

## `public void PFlowOrderApply()`

Places each reel in the row the flow assigns, which lets the two strips swap order.

## `public void PFlowPaletteApply()`

Reloads the palette size and re-announces the sections so bands redraw with new colors.

## `public static Geometry PFlowWaveformBuild(LFlowWaveformOutline lOutline)`

Turns an outline into a frozen filled polygon without strokes.
The map and viewfinder both draw it.

## `protected override void OnMouseWheel(MouseWheelEventArgs e)`

Marks the wheel handled only when the flow consumed it.

## `private void PFlowLabelsApply()`

Writes the four time labels from the spool, two for each reel.

## `private void PFlowTimerDefer()`

Restarts the request timer so only the last request in a burst fires.

## `private void PFlowKeyframeHandle(LKeyframeNotice lNotice) =>`

Hops to the dispatcher at background priority, because the notice may come from a worker thread.

## `private void PFlowWaveformHandle() =>`

Hops to the dispatcher at background priority, because the notice may come from a worker thread.

## `private void PFlowNoticeShow(LFlowLosslesscutPrompt lPrompt, Action<int> lAnswer)`

A notice has one button, so it always answers primary after it is dismissed.

## `private void PFlowWarningShow(LFlowLosslesscutPrompt lPrompt, Action<int> lAnswer)`

A warning has one button, so it always answers primary after it is dismissed.

## `private static Grid PReelGridBuild(FrameworkElement reelBody, TextBlock labelLeft, TextBlock labelRight)`

Places a strip between two fixed-width label columns.
Both reels use the same widths, so the strips line up vertically.
