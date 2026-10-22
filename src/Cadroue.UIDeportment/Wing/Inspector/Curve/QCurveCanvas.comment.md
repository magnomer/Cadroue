# QCurveCanvas.cs

## `public sealed class QCurveCanvas`

The class draws the curve editor on a canvas and forwards pointer input to the drag sibling.
The grid, track, dots and histogram are read as plain records, and the rebuild turns them into shapes.
Tests `TrackRead_SamplesWholeWidth`, `GridRead_IdentityLine_OnlyWhileIdentity`, `DotsRead_SelectedIsLarger_EndpointsFilledAsGuide` and `HistogramRead_ScalesPeakToHeadroom_EmptyWithoutCounts` pin the reads.

## `public void QCurveAttach(Canvas lCanvas, bool lAttached)`

The handlers are removed first, so a repeated attach never doubles them.
They are added again only when the canvas is to stay attached.

## `public void QCurveRebuild(Canvas lCanvas)`

The rebuild clears the canvas and builds every shape again from the reads.
Test `Rebuild_SecondCanvas_PreservesFirst` pins that a second canvas leaves the first intact.

## `public static (double, double) QCurveValueResolve(double lX, double lY, double lSize)`

Canvas pixel to value.
Input runs on X from 0 at the left to 1 at the right.
Output runs on Y from 0 at the bottom to 1 at the top.
Both results are clamped to the range 0 to 1.
The drag code uses it to turn a pointer position into a point.
Tests `Press_NearPoint_SelectsIt` and `Press_OnEmpty_AddsAndSelectsPoint` pin the press.

## `private static QCurvePoint QCurvePixelResolve(double lInput, double lOutput, double lSize)`

Value to canvas pixel, the inverse of `QCurveValueResolve`.
The input and output are clamped to the range 0 to 1 first.
The track and the dots both place their points through it.

## `public IReadOnlyList<QCurvePoint> QCurveHistogramRead(double lSize)`

The area is empty without a histogram or without a positive peak.
Otherwise the peak scales to 0.94 of the height, so the tallest bin never touches the top edge.
