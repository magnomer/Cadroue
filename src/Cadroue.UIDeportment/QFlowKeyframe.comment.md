# QFlowKeyframe.cs
Hash: `f35e55375351d9dc`

## `public sealed class QFlowKeyframe`

Owns the keyframe scan orchestration of one timeline, the known entries and scanned ranges.
Timers stay in the veneer, and this class only decides when they start, resume or stop.

## `public QFlowKeyframe(QFlow lOwner)`

Subscribes the orchestrator notice, which is unsubscribed again in close.

## `public event Action<TimeSpan>? QFlowTimerResumed;`

Carries the resume delay read at suspend time, so a preference change applies to the next suspend.

## `public event Action<LKeyframeNotice>? QFlowKeyframeReady;`

Hands a scan notice to the veneer, which passes it back to apply on the UI thread.

## `public bool QFlowResumePending => qFlowResumePending;`

True between a suspend that asked for a resume and the next reset.
While pending, a deferred request must not start a second timer.

## `public static TimeSpan QFlowRequestDelay => TimeSpan.FromMilliseconds(250);`

The short debounce before a scan request, so cursor travel does not trigger a scan per frame.

## `public static TimeSpan QFlowResumeDelay =>`

The wait after a suspend before scanning resumes, taken from the user preference at call time.

## `public void QFlowKeyframeDefer()`

Stops the timer when the tab cannot scan at all.
A pending resume already owns the timer, so a request is ignored then.

## `public void QFlowKeyframeSuspend(bool lResume)`

Pauses the orchestrator.
Resume is armed only when the tab is still active and loaded.

## `public void QFlowKeyframeReset()`

Clears the pending resume and stops the timer.

## `public void QFlowKeyframeRun()`

Resets the timers first, then starts a scan around the cursor only when scanning is allowed.
The trace line records the window the scan covers.

## `public void QFlowKeyframeTick() => QFlowKeyframeRun();`

The timer elapsed handler, which runs the scan.

## `public void QFlowKeyframeClear()`

Drops entries and ranges so a source change never shows keyframes of the previous file.

## `public void QFlowKeyframeClose()`

Stops the timer, detaches from the orchestrator and disposes it.

## `private void QFlowKeyframeHandle(LKeyframeNotice lNotice)`

Notices from an inactive or unloaded tab are dropped before reaching the veneer.

## `public void QFlowKeyframeApply(LKeyframeNotice lNotice)`

Ignores a notice whose serial is not the orchestrator's current one, because it belongs to a superseded scan.
A pending navigation direction continues now that the data has arrived, without requesting another scan.

## `private void QFlowKeyframeRecord(LKeyframeNotice lNotice)`

Writes one trace line per distinct result.
The stamp comparison suppresses repeats of identical scan outcomes.

## `public void QFlowKeyframeMove(int lDirection) => QFlowKeyframeMove(lDirection, true);`

A user move may request a scan when the data is not ready.

## `private void QFlowKeyframeMove(int lDirection, bool lRequestScan)`

Negative goes to the previous keyframe, positive to the next, zero to the nearest.
A not-ready result remembers the direction and finishes once the scan arrives.
A failed result clears the direction and warns, so navigation does not retry forever.
