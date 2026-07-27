using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cadroue.Core;

public sealed class LWorkItem : INotifyPropertyChanged
{
    private LWorkState lWorkStateCurrent = LWorkState.LWorkStatePending;
    private string lWorkMessage = string.Empty;
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
        DateTimeOffset? lWorkCreateTime = null)
    {
        LWorkId = lWorkId ?? Guid.NewGuid();
        LWorkBatchId = lWorkBatchId;
        LWorkKind = lWorkKind;
        LWorkPriority = lWorkPriority;
        LWorkSourcePath = lWorkSourcePath;
        LWorkStart = lWorkStart;
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

    public TimeSpan LWorkStart { get; }

    public TimeSpan LWorkEnd { get; }

    public TimeSpan LWorkDuration => LWorkEnd - LWorkStart;

    public string LWorkOutputName { get; }

    public string LWorkOutputPath { get; }

    public LWorkOutput LWorkOutput { get; }

    public DateTimeOffset LWorkCreateTime { get; }

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
            if (Math.Abs(lWorkProgress - lWorkClamped) < 0.0005)
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
