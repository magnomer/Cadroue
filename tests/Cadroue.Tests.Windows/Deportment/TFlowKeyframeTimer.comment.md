# TFlowKeyframeTimer.cs
Hash: `81ff70e6bad01b98`

## `public sealed class TFlowKeyframeTimer`

Checks the flow without any control through its keyframe timer decisions and neighbouring notices.
It covers the request and resume timers, cursor seeks, shortcuts, the wheel, the rename popup and the strip notices.
The timers are replaced by recorded notices.

## `public void Defer_StartsTheRequestTimer_OnlyWithASourceAndNoResumePending()`

A pending resume owns the next scan, so a defer during it starts nothing.
Clearing the source resets the timer instead of starting it.

## `public void Suspend_WithoutCommand_NeverArmsTheResume()`

Without the command flag the flow is not live, so a suspend only resets the timer.

## `public void CursorUpdate_Defers_AndSeekSuspends()`

A seek past the end clamps to the duration.
Once the command flag is off, a cursor update is ignored.

## `public void Apply_IgnoresStaleSerials_AndForwardsTheCurrentOne()`

A scan notice from an older serial must never overwrite the keyframes of the current source.

## `public void Shortcut_RunsOnlyWithCommandAndSpool()`

Section shortcuts need the section flag.
Without the command flag nothing runs, and an unknown token is not handled.

## `public void Wheel_SeeksByDefault_AndReportsHandled()`

A zero delta is not handled.
One wheel notch from the start of the media seeks to a later time.

## `public void Name_EditsTheSelection_AndCommitsTrimmedText()`

The popup refuses to open with no selected section.
A commit trims the text and closes the popup, while hiding an idle popup raises no second close.

## `public void Attach_And_Clear_RaiseTheStripNotices()`

Clearing a flow with no media reports false and raises nothing.
Attach and clear each also raise the media notice, and a closed flow reports unloaded.
