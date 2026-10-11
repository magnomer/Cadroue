# TViewerNeutral.cs
Hash: `4de67a6d9fa4c72d`

## `public sealed class TViewerNeutral`

Checks the viewer's neutral-point tool sibling: arming, cancelling, picking and the automatic estimate.
The media source is a path that does not exist, so a pick can never decode a frame.

## `public void ToolSet_ArmOnce_RetargetsInPlace_SerialMovesOnCancel()`

Arming while already armed only switches the target.
It keeps the serial, so an in-flight pick stays valid.
Cancelling advances the serial and disarms the tool.
A second cancel does nothing.
The tool notice fires on the first arm and on the disarm, not on the retarget.
Focus is requested once.

## `public void KeyHandle_EscapeWhileArmed_Cancels()`

Escape is consumed only while the tool is armed.
Other keys pass through.

## `public void Press_OutsideVideo_StaysArmed()`

A press outside the displayed video rectangle picks nothing.
The tool stays armed so the user can try again.

## `public async Task Press_InsideVideo_EndsToolAndReportsDecodeFailure()`

A press inside the video ends the tool and releases mouse capture at once.
The decode runs in the background.
With a missing source it raises one sample whose outcome is the decode failure.

## `public void EstimateStart_NoMedia_RaisesEmptyWheelOnce()`

Without media the estimate raises a single empty wheel.
The display then clears instead of keeping an old dot.
