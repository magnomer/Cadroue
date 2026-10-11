# TViewerLoadSerial.cs
Hash: `03b05bf43d6c0d3b`

## `public sealed class TViewerLoadSerial`

Checks the viewer's load serial without any control.
The serial lets an async load result be dropped when the viewer has moved on.

## `public void Serial_Advances_StaleResultIgnored()`

Every change advances the serial by one.
Only the latest serial is still valid.

## `public void Serial_CommandInactive_Rejects()`

While commands are off no serial is valid.
Turning them on makes the current serial valid again.

## `public void Serial_Unloaded_RejectsForever()`

Once the viewer is unloaded no result is accepted.
That includes a serial taken afterwards.

## `public void MediaClose_AdvancesSerial_ClearsIntent()`

Closing the media drops the intent and the source path.
It also invalidates any load still in flight.
