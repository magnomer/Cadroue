# PMap.cs
Hash: `cfa94f9af0509a92`

## `protected override void OnRender(DrawingContext drawingContext)`

The draw is timed inside `LMap`, so the Veneer holds no clock.
The paint order is fixed: under shapes, waveform, section bands, over shapes, then the cursor.

## `protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)`

Captures the mouse on every press, so a drag keeps working past the strip's edges.

## `protected override void OnLostMouseCapture(MouseEventArgs e)`

Ends the drag when capture is taken away, so a lost release never leaves `LMap` dragging.
