namespace Cadroue.Core;

public sealed class LWorkItem
{
    private double lWorkProgress;

    public LWorkItem(
        Guid lWorkBatchId,
        LWorkKind lWorkKind,
        LWorkPriority lWorkPriority,
        string lWorkSourcePath,
        TimeSpan lWorkStart,
        TimeSpan lWorkEnd,
        string lWorkOutputName,
        string lWorkOutputPath,
        LWorkOutput lWorkOutput,
        Guid? lWorkId = null,
        DateTimeOffset? lWorkCreateTime = null,
        LWorkCrop? lWorkCrop = null,
        LWorkVideo? lWorkVideo = null,
        LWorkAudio? lWorkAudio = null,
        IReadOnlyList<string>? lWorkMergeSources = null)
    {
        LWorkCrop = lWorkCrop ?? LWorkCrop.LWorkCropCreate();
        LWorkVideo = lWorkVideo ?? LWorkVideo.LWorkVideoCreate();
        LWorkAudio = lWorkAudio ?? LWorkAudio.LWorkAudioCreate();
        LWorkMergeSources = lWorkMergeSources ?? Array.Empty<string>();
        LWorkId = lWorkId ?? Guid.NewGuid();
        LWorkBatchId = lWorkBatchId;
        LWorkKind = lWorkKind;
        LWorkPriority = lWorkPriority;
        LWorkSourcePath = lWorkSourcePath;
        LWorkOrigin = lWorkStart;
        LWorkEnd = lWorkEnd;
        LWorkOutputName = lWorkOutputName;
        LWorkOutputPath = lWorkOutputPath;
        LWorkOutput = lWorkOutput;
        LWorkCreateTime = lWorkCreateTime ?? DateTimeOffset.Now;
    }

    public Guid LWorkId { get; }

    public Guid LWorkBatchId { get; }

    public LWorkKind LWorkKind { get; }

    public LWorkPriority LWorkPriority { get; }

    public string LWorkSourcePath { get; }

    public TimeSpan LWorkOrigin { get; }

    public TimeSpan LWorkEnd { get; set; }

    public TimeSpan LWorkDuration => LWorkEnd - LWorkOrigin;

    public string LWorkOutputName { get; private set; }

    public string LWorkOutputPath { get; private set; }

    public void LWorkOutputSet(string lWorkOutputPath, string lWorkOutputName)
    {
        LWorkOutputPath = lWorkOutputPath;
        LWorkOutputName = lWorkOutputName;
    }

    public LWorkOutput LWorkOutput { get; }

    public LWorkCrop LWorkCrop { get; }

    public LWorkVideo LWorkVideo { get; }

    public LWorkAudio LWorkAudio { get; }

    public IReadOnlyList<string> LWorkMergeSources { get; }

    public Guid LWorkRelayTarget { get; set; }

    public Guid LWorkRelaySource { get; set; }

    public Guid LWorkLineage { get; set; }

    public string LWorkTab { get; set; } = string.Empty;

    public DateTimeOffset LWorkCreateTime { get; }

    public DateTimeOffset? LWorkStartTime { get; set; }

    public DateTimeOffset? LWorkFinishTime { get; set; }

    public long? LWorkOutputBytes { get; set; }

    public long? LWorkSourceBytes { get; set; }

    public LWorkMedia? LWorkSourceMedia { get; set; }

    public LWorkMedia? LWorkOutputMedia { get; set; }

    public int LWorkOwnerProcess { get; set; }

    public long LWorkOwnerStamp { get; set; }

    public Guid LWorkOwnerRunner { get; set; }

    public LWorkPhase LWorkPhaseCurrent { get; set; }

    public int LWorkAttemptCount { get; set; }

    public int LWorkRecoverCount { get; set; }

    public LWorkState LWorkStateCurrent { get; set; } = LWorkState.LWorkStatePending;

    public string LWorkMessage { get; set; } = string.Empty;

    public double LWorkProgress
    {
        get => lWorkProgress;
        set => lWorkProgress = value < 0 ? 0 : value > 1 ? 1 : value;
    }
}
