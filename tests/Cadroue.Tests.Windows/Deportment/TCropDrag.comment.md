# TCropDrag.cs
Hash: `15696b358cee96d8`

## `public sealed class TCropDrag`

Checks the crop drag sibling of the viewer crop without any control.
Setup opens a 1920 by 1080 source in an 800 by 450 overlay with the crop tool armed.
Boxes are compared in overlay pixels and commits in source pixels.

## `public void Draw_PressMoveRelease_CommitsSourcePixels()`

A drawn box becomes a source rectangle only on release.
Capture is taken on press and given back on release.
A move without the button held is ignored.

## `public void Draw_WithRatio_ClampsToVideoAndKeepsShape()`

A fixed ratio keeps the drawn box in shape.
The shorter dragged side decides the box size.

## `public void Grip_BottomRight_ResizesFromOrigin()`

Dragging a corner grip resizes the box against the opposite corner.
The edge direction is read from the grip.

## `public void Body_Drag_MovesWholeBoxInsideVideo()`

Dragging the body moves the box without resizing it.
The box stops at the video edge.

## `public void Press_NotEditable_IsIgnored()`

A locked overlay or a disarmed tool must refuse every press kind.
A refused press must not start any viewer tool.

## `public void Lock_DuringDrag_ClearsTransientsAndReleasesCapture()`

Locking in the middle of a drag must end the gesture.
Otherwise the veneer would keep mouse capture with no drag to serve.
