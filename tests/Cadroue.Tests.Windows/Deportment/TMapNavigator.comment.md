# TMapNavigator.cs
Hash: `48bcf5a1240303aa`

## `public sealed class TMapNavigator`

Checks the map strip's hit testing, drags and draw frame without any control.
Each test uses a 60 second source with a working range from 10 to 40 seconds.
The strip is 600 pixels wide and 40 high.

## `public void Hover_FindsGrips_Body_AndCursorArea()`

The top band of the strip holds the navigator and its two grips.
The lower band and the area outside the navigator belong to the cursor.
A strip with no width hits nothing.

## `public void BodyDrag_MovesTheSpool_AndRaisesDragNotices()`

Dragging the navigator body shifts the whole working range without resizing it.
Drag start and end are announced so playback can pause around the drag.
A move after the drag is cleared is refused.

## `public void GripDrag_ResizesOneEnd_AndCursorPressSeeks()`

A grip moves only its own end of the range.
A cursor press seeks at once and keeps seeking while dragged.
The seek is clamped to the media.
A flow with no source takes no press.

## `public void Frame_CarriesNavigatorShapes_AndRedrawsOnFlowNotices()`

The frame is plain records the veneer paints one by one.
A cursor change asks for a redraw and names the trigger.
A strip too small to draw yields no shapes.
