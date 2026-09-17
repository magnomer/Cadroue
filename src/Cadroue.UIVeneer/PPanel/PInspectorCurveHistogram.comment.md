# PInspectorCurveHistogram.cs

## `public void PCurveHistogramApply(LHistogramCounts? pHistogram)`

Store the current frame's counts (or clear) and repaint.
The counts feed only the behind-the-curve guide and never touch the FFmpeg command.

## `private void PCurveHistogramDraw()`

Faint filled area behind the grid for the active channel: luminance for Master, else that channel.
Non-interactive so it never intercepts point editing.
