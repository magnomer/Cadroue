using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The engine's face for one flow's keyframe scan: every member forwards to the same-named member of that flow's
// keyframe facade, which the flow outlet has the engine make. It rejects a null facade and keeps it for every forward.
public sealed class LKeyframeOutlet : LKeyframePort
{
    private readonly LKeyframeFacade _lKeyframeOutletEngine;

    internal LKeyframeOutlet(LKeyframeFacade lFacade)
    {
        ArgumentNullException.ThrowIfNull(lFacade);
        _lKeyframeOutletEngine = lFacade;
    }

    public event Action<LKeyframeNotice>? LKeyframeArrived
    {
        add => _lKeyframeOutletEngine.LKeyframeArrived += value;
        remove => _lKeyframeOutletEngine.LKeyframeArrived -= value;
    }

    public IReadOnlyList<LKeyframeEntry> LKeyframeEntries => _lKeyframeOutletEngine.LKeyframeEntries;

    public IReadOnlyList<LKeyframeScanRange> LKeyframeRanges => _lKeyframeOutletEngine.LKeyframeRanges;

    public TimeSpan LKeyframeDelayRead() => _lKeyframeOutletEngine.LKeyframeDelayRead();

    public void LKeyframeStart(string lSource, LMediaInfo lMedia, TimeSpan lCursor, TimeSpan lDuration) =>
        _lKeyframeOutletEngine.LKeyframeStart(lSource, lMedia, lCursor, lDuration);

    public void LKeyframeSuspend() => _lKeyframeOutletEngine.LKeyframeSuspend();

    public void LKeyframeSync(TimeSpan lCursor) => _lKeyframeOutletEngine.LKeyframeSync(lCursor);

    public bool LKeyframeApply(LKeyframeNotice lNotice) => _lKeyframeOutletEngine.LKeyframeApply(lNotice);

    public void LKeyframeClear() => _lKeyframeOutletEngine.LKeyframeClear();

    public (bool LKeyframeFailed, bool LKeyframeReady, TimeSpan? LKeyframeTarget) LKeyframeMove(
        int lDirection, TimeSpan lCursor) =>
        _lKeyframeOutletEngine.LKeyframeMove(lDirection, lCursor);

    public void LKeyframeClose() => _lKeyframeOutletEngine.LKeyframeClose();
}
