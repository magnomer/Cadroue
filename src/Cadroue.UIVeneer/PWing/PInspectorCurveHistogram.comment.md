# PInspectorCurveHistogram.cs

## `private void PCurveHistogramDraw()`

Faint filled area behind the grid for the active channel: luminance for Master, else that channel.
Non-interactive so it never intercepts point editing.
The counts come from `LCurve` and feed only this guide, never the FFmpeg command.
