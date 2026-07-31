using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cadroue.Core;

public sealed class LWorkItem : INotifyPropertyChanged
{
    private LWorkState lWorkStateCurrent = LWorkState.LWorkStatePending;
    private string lWorkMessage = string.Empty;
    private double lWorkProgress;
    private TimeSpan lWorkEnd;

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
        this.lWorkEnd = lWorkEnd;
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

    public TimeSpan LWorkEnd
    {
        get => lWorkEnd;
        set
        {
            if (lWorkEnd == value)
            {
                return;
            }

            lWorkEnd = value;
            LWorkPropertyChange();
            LWorkPropertyChange(nameof(LWorkDuration));
        }
    }

    public TimeSpan LWorkDuration => LWorkEnd - LWorkOrigin;

    public string LWorkOutputName { get; }

    public string LWorkOutputPath { get; }

    public LWorkOutput LWorkOutput { get; }

    public LWorkCrop LWorkCrop { get; }

    public LWorkVideo LWorkVideo { get; }

    public LWorkAudio LWorkAudio { get; }

    public IReadOnlyList<string> LWorkMergeSources { get; }

    public Guid LWorkRelayTarget { get; set; }

    public Guid LWorkRelaySource { get; set; }

    public Guid LWorkLineage { get; set; }

    public DateTimeOffset LWorkCreateTime { get; }

    public DateTimeOffset? LWorkStartTime { get; set; }

    public DateTimeOffset? LWorkFinishTime { get; set; }

    public long? LWorkOutputBytes { get; set; }

    public long? LWorkSourceBytes { get; set; }

    public LWorkMedia? LWorkSourceMedia { get; set; }

    public LWorkMedia? LWorkOutputMedia { get; set; }

    public int LWorkOwnerProcess { get; set; }

    public Guid LWorkOwnerRunner { get; set; }

    public LWorkPhase LWorkPhaseCurrent { get; set; }

    public int LWorkAttemptCount { get; set; }

    public LWorkState LWorkStateCurrent
    {
        get => lWorkStateCurrent;
        set
        {
            if (lWorkStateCurrent == value)
            {
                return;
            }

            lWorkStateCurrent = value;
            LWorkPropertyChange();
        }
    }

    public string LWorkMessage
    {
        get => lWorkMessage;
        set
        {
            if (lWorkMessage == value)
            {
                return;
            }

            lWorkMessage = value;
            LWorkPropertyChange();
        }
    }

    public double LWorkProgress
    {
        get => lWorkProgress;
        set
        {
            double lWorkClamped = value < 0 ? 0 : value > 1 ? 1 : value;
            if (lWorkProgress.Equals(lWorkClamped))
            {
                return;
            }

            lWorkProgress = lWorkClamped;
            LWorkPropertyChange();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void LWorkPropertyChange([CallerMemberName] string? lWorkPropertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(lWorkPropertyName));
    }
}
