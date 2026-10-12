# PViewfinder.cs
Hash: `7ffdffeef1eb464d`

## `protected override void OnRender(DrawingContext drawingContext)`

The draw is timed inside `QViewfinder`, so the Veneer holds no clock.
Keyframe marks paint after the over shapes, and the cursor with its chip paints last.

## `protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)`

Captures the mouse on every press, so a seek drag keeps working past the strip's edges.

## `protected override void OnLostMouseCapture(MouseEventArgs e)`

Ends the drag when capture is taken away, so a lost release never leaves `QViewfinder` dragging.

## `private static void PViewfinderKeyframesDraw(DrawingContext drawingContext, IReadOnlyList<QViewfinderShape> lMarks)`

Snaps each keyframe mark to whole pixels, so one-pixel marks stay sharp instead of blurring.

## `private FormattedText PViewfinderNameBuild(string pName, double pRoom) => new(`

Trims a section name to one line in the room `QViewfinderText` leaves beside the badge.
