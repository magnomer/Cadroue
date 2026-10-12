# PSMonitorTransport.cs
Hash: `ad5dc67852c01177`

## `private UIElement PSMonitorTransportBuild()`

Builds the transport row under the rails, which holds the play button alone.
The zoom buttons sit above the rails and the bypass radios sit on the rails.
Every control forwards its gesture to QSMonitor, which decides what it means.

## `private void PSMonitorFaceApply()`

The play icon and tooltip come from QSMonitor, so the button never reads the playing state itself.

## `private void PSMonitorZoomApply()`

The scrollbar shape comes whole from QSMonitor after each zoom step.
At full view the scrollbar stays visible but disabled and faded.

## `private RadioButton PSMonitorRadioBuild(string pLabelKey, string pTooltipKey, bool pBypass)`

The Before radio asks for bypass and the After radio asks for the processed sound.
QSMonitor ignores a radio that asks for the state already in force.

## `private void PSMonitorSeekHandle(object pSender, MouseEventArgs pEvent)`

A move seeks only while the canvas holds the capture, so hovering never seeks.
The viewer duration is passed in, so QSMonitor maps the pixel into the zoomed span.

## `private void PSMonitorCursorHandle(TimeSpan pCursor) =>`

The flow cursor notice is posted to this window's dispatcher before it reaches QSMonitor.

## `private void PSMonitorBypassApply()`

Mirrors the viewer bypass into the two radios, including a change made outside this window.
