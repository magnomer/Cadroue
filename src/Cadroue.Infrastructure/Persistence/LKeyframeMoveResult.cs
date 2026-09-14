namespace Cadroue.Infrastructure;

public readonly record struct LKeyframeMoveResult(bool LKeyframeReady, TimeSpan? LKeyframeTarget, bool LKeyframeFailed)
{
    public static LKeyframeMoveResult LKeyframePending => new(false, null, false);

    public static LKeyframeMoveResult LKeyframeFailedCreate() => new(false, null, true);

    public static LKeyframeMoveResult LKeyframeReadyCreate(TimeSpan? target) => new(true, target, false);
}
