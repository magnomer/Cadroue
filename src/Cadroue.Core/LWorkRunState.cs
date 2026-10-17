namespace Cadroue.Core;

public sealed class LWorkRunState
{
    private double lWorkProgress;

    public DateTimeOffset? LWorkStartTime { get; set; }

    public DateTimeOffset? LWorkFinishTime { get; set; }

    public long? LWorkOutputBytes { get; set; }

    public LWorkMedia? LWorkOutputMedia { get; set; }

    public int LWorkOwnerProcess { get; set; }

    public long LWorkOwnerStamp { get; set; }

    public Guid LWorkOwnerRunner { get; set; }

    public LWorkPhase LWorkPhaseCurrent { get; set; }

    public LWorkStage LWorkStageCurrent { get; set; }

    public int LWorkAttemptCount { get; set; }

    public int LWorkRecoverCount { get; set; }

    public int LWorkRetryCount { get; set; }

    public LWorkState LWorkStateCurrent { get; set; } = LWorkState.LWorkStatePending;

    public string LWorkMessage { get; set; } = string.Empty;

    public double LWorkProgress
    {
        get => lWorkProgress;
        set => lWorkProgress = value < 0 ? 0 : value > 1 ? 1 : value;
    }
}
