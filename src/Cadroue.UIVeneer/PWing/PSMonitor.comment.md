# PSMonitor.cs
Hash: `cd2924b6e37fcd19`

## `internal static PSMonitor PSMonitorShow(Window? pOwner, LSMonitor pSource, PFlow pFlow, PViewer pViewer)`

Opens the loudness monitor window and ties it to the flow and viewer of one tab.
Any monitor already open is closed first, so only one exists at a time.
Seek, play and pause requests from the window go to the flow.
Bypass and the playing state travel between LSMonitor and the viewer.

## `private PSMonitor(Window? pOwner, LSMonitor pSource, PFlow pFlow, PViewer pViewer)`

The window takes its placement from the saved `NormalizePreview` key, which predates the Monitor name.
The cursor is seeded from the flow, so the play head shows before the first clock tick.

## `private Grid PSMonitorRailBuild(RadioButton pRadio, out Canvas pCanvas, out TextBlock pStatus, out Border pHead)`

Each rail stacks the waveform canvas, the play head, the status line and its radio.
Only the canvas takes the mouse, so a press on the head or status still seeks.

## `private void PSMonitorReadyHandle() => Dispatcher.BeginInvoke(PSMonitorReadyApply);`

A scan result can arrive off the UI thread, so it is marshalled onto the dispatcher.

## `private void PSMonitorReadyApply()`

Restarting the short timer coalesces a burst of scan results into one redraw.

## `private void PSMonitorRailDraw(Canvas pCanvas, TextBlock pStatus, Brush pFill, bool pAfter)`

Draws one rail from the frame LSMonitor resolves for the canvas size.
The rail adds grid lines, axis labels and the waveform outline, and decides none of them.

## `private void PSMonitorCloseHandle(object? pSender, EventArgs pEvent)`

Unhooks every notice and detaches the viewer, so a closed window holds no subscriptions.
The placement is saved here, so the next monitor opens where this one closed.
