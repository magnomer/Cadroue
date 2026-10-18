using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The flow's keyframe gate: when a scan may run, when its timers wait or resume, which notice is applied and where a
// keyframe move lands, from the flow's media, cursor and switches. CFlow makes one per flow; the engine scans, keeps
// the keyframes and traces. A notice passes through unread: it waits here until the UI thread applies it.
public sealed class CKeyframe
{
    private readonly LKeyframePort _cKeyframePort;
    private readonly CFlow _cKeyframeFlow;
    private readonly ConcurrentQueue<LKeyframeNotice> _cKeyframeNotices = new();

    internal CKeyframe(LKeyframePort port, CFlow flow)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(flow);
        _cKeyframePort = port;
        _cKeyframeFlow = flow;
        _cKeyframePort.LKeyframeArrived += CKeyframeHandle;
    }

    // The request timer starts over.
    public event Action? CKeyframeDeferred;

    // The resume timer starts with this delay.
    public event Action<TimeSpan>? CKeyframeResumed;

    // Both timers stop.
    public event Action? CKeyframeStopped;

    // Told on the scanner's thread: a notice waits, and each call to CKeyframeApply takes one, in order.
    public event Action? CKeyframeArrived;

    // The keyframes or their ranges changed.
    public event Action? CKeyframeChanged;

    public bool CKeyframeResumePending { get; private set; }

    // The direction of a keyframe move that waits for its notice: -1 previous, 0 nearest, 1 next; null when none waits.
    public int? CKeyframeDirection { get; private set; }

    // The engine's keyframes, passed through unread, only for the readers not yet moved to a gate.
    public IReadOnlyList<LKeyframeEntry> CKeyframeEntries => _cKeyframePort.LKeyframeEntries;

    public IReadOnlyList<LKeyframeScanRange> CKeyframeRanges => _cKeyframePort.LKeyframeRanges;

    // A scan waits for the request timer only while commands run on a loaded source and no resume is pending.
    public void CKeyframeDefer()
    {
        if (!_cKeyframeFlow.CFlowCommandActive || _cKeyframeFlow.CFlowUnloaded || !_cKeyframeFlow.CFlowSourceCheck())
        {
            CKeyframeStopped?.Invoke();
            return;
        }

        if (CKeyframeResumePending)
        {
            return;
        }

        CKeyframeDeferred?.Invoke();
    }

    public void CKeyframeSuspend(bool resume)
    {
        CKeyframeReset();
        _cKeyframePort.LKeyframeSuspend();
        if (resume && _cKeyframeFlow.CFlowCommandActive && !_cKeyframeFlow.CFlowUnloaded)
        {
            CKeyframeResumePending = true;
            CKeyframeResumed?.Invoke(_cKeyframePort.LKeyframeDelayRead());
        }
    }

    public void CKeyframeReset()
    {
        CKeyframeResumePending = false;
        CKeyframeStopped?.Invoke();
    }

    public void CKeyframeRun()
    {
        CKeyframeReset();
        if (!_cKeyframeFlow.CFlowScanCheck())
        {
            return;
        }

        _cKeyframePort.LKeyframeStart(
            _cKeyframeFlow.CFlowSource!, _cKeyframeFlow.CFlowMedia!, _cKeyframeFlow.CFlowCursor,
            _cKeyframeFlow.CFlowDuration);
    }

    // The flow drops the waiting move when its cursor, source or commands change; a driver may set one.
    public void CKeyframeDirectionSet(int? direction) => CKeyframeDirection = direction;

    public void CKeyframeSync(TimeSpan cursor) => _cKeyframePort.LKeyframeSync(cursor);

    public void CKeyframeClear() => _cKeyframePort.LKeyframeClear();

    public void CKeyframeClose()
    {
        CKeyframeReset();
        _cKeyframePort.LKeyframeClose();
    }

    // Applies the oldest waiting notice unless the flow is unloaded; a pending move then runs without a new scan.
    public void CKeyframeApply()
    {
        if (!_cKeyframeNotices.TryDequeue(out LKeyframeNotice? notice) || _cKeyframeFlow.CFlowUnloaded)
        {
            return;
        }

        if (!_cKeyframePort.LKeyframeApply(notice))
        {
            return;
        }

        CKeyframeChanged?.Invoke();
        if (CKeyframeDirection is int direction)
        {
            CKeyframeMove(direction, false);
        }
    }

    public void CKeyframePreviousMove() => CKeyframeMove(-1, true);

    public void CKeyframeNearestMove() => CKeyframeMove(0, true);

    public void CKeyframeNextMove() => CKeyframeMove(1, true);

    // A notice is kept only while commands run on a loaded flow.
    private void CKeyframeHandle(LKeyframeNotice notice)
    {
        if (!_cKeyframeFlow.CFlowCommandActive || _cKeyframeFlow.CFlowUnloaded)
        {
            return;
        }

        _cKeyframeNotices.Enqueue(notice);
        CKeyframeArrived?.Invoke();
    }

    // Without a source the move waits for the request timer; a move the scan cannot answer yet waits for its notice.
    private void CKeyframeMove(int direction, bool requestScan)
    {
        if (!_cKeyframeFlow.CFlowSourceCheck())
        {
            CKeyframeDefer();
            return;
        }

        (bool failed, bool ready, TimeSpan? target) =
            _cKeyframePort.LKeyframeMove(direction, _cKeyframeFlow.CFlowCursor);
        if (failed)
        {
            CKeyframeDirection = null;
            return;
        }

        if (!ready)
        {
            CKeyframeDirection = direction;
            if (requestScan)
            {
                CKeyframeRun();
            }

            return;
        }

        CKeyframeDirection = null;
        if (target is { } landing)
        {
            _cKeyframeFlow.CFlowCursorPropagate(landing, true, true);
        }
    }
}
