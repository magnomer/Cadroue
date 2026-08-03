namespace Cadroue.Infrastructure;

public readonly record struct LKeyframeMoveResult(bool LKeyframeReady, TimeSpan? LKeyframeTarget)
{
    public static LKeyframeMoveResult LKeyframePending => new(false, null);

    public static LKeyframeMoveResult LKeyframeReadyResult(TimeSpan? target) => new(true, target);
}
