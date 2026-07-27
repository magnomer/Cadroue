using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cadroue.Core;

/// <summary>
/// One unit of scheduled work: a single source range producing a single output file.
/// A split of N sections produces N items sharing one batch id.
///
/// Everything describing *what* to produce is immutable — the item keeps the settings
/// it was scheduled with. Only the run state and its message change afterwards, and
/// those notify so a view can follow them.
/// </summary>
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
        LWorkOutput lWorkOutput)
    {
        LWorkId = Guid.NewGuid();
        LWorkBatchId = lWorkBatchId;
        LWorkKind = lWorkKind;
        LWorkPriority = lWorkPriority;
        LWorkSourcePath = lWorkSourcePath;
        LWorkStart = lWorkStart;
        LWorkEnd = lWorkEnd;
        LWorkOutputName = lWorkOutputName;
        LWorkOutputPath = lWorkOutputPath;
        LWorkOutput = lWorkOutput;
        LWorkCreateTime = DateTimeOffset.Now;
    }

    public Guid LWorkId { get; }

    /// <summary>Groups the items that one Add List press produced.</summary>
    public Guid LWorkBatchId { get; }

    public LWorkKind LWorkKind { get; }

    public LWorkPriority LWorkPriority { get; }

    public string LWorkSourcePath { get; }

    public TimeSpan LWorkStart { get; }

    public TimeSpan LWorkEnd { get; }

    public TimeSpan LWorkDuration => LWorkEnd - LWorkStart;

    /// <summary>Resolved output file name including extension.</summary>
    public string LWorkOutputName { get; }

    /// <summary>Full destination path: the resolved location folder plus the name.</summary>
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

    /// <summary>Fraction of this job encoded, 0 to 1. Only meaningful while running.</summary>
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
