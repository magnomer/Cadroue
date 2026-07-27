using System.Collections.ObjectModel;

namespace Cadroue.Core;

/// <summary>
/// Ground truth for scheduled work, owned by the backend.
///
/// The shell never keeps its own copy of the queue: it reads <see cref="LScheduleRecords"/>
/// and follows <see cref="LScheduleChange"/>. Enqueue order is preserved for display;
/// precedence is applied when a runner asks for the next item, so a high-priority
/// Execute does not visibly reshuffle rows the user is looking at.
///
/// Not thread-safe. Every call must come from one thread (today, the UI thread). A
/// background runner must marshal its state writes rather than mutate from a worker.
/// </summary>
public sealed class LSchedule
{
    private readonly ObservableCollection<LWorkItem> lScheduleItems = new();

    public LSchedule()
    {
        LScheduleRecords = new ReadOnlyObservableCollection<LWorkItem>(lScheduleItems);
    }

    /// <summary>Process-wide schedule. The one place queued work exists.</summary>
    public static LSchedule LScheduleCurrent { get; } = new();

    public ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    /// <summary>Raised after any add, remove, clear, or run-state change.</summary>
    public event Action<LSchedule>? LScheduleChange;

    /// <summary>
    /// Whether the queue is meant to be running. Ground truth for the transport, so a
    /// runner added later observes this rather than the UI owning the flag.
    /// </summary>
    public bool LScheduleRunning { get; private set; }

    public int LScheduleDoneCount =>
        lScheduleItems.Count(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone);

    public void LScheduleStart()
    {
        if (LScheduleRunning)
        {
            return;
        }

        LScheduleRunning = true;
        LScheduleChange?.Invoke(this);
    }

    public void LSchedulePause()
    {
        if (!LScheduleRunning)
        {
            return;
        }

        LScheduleRunning = false;
        LScheduleChange?.Invoke(this);
    }

    /// <summary>
    /// Stop the queue and cancel everything still pending. Finished items keep their
    /// result. Returns how many items were cancelled.
    /// </summary>
    public int LScheduleCancel()
    {
        LScheduleRunning = false;
        int lScheduleCancelledCount = 0;
        foreach (LWorkItem lWorkItem in lScheduleItems)
        {
            if (lWorkItem.LWorkStateCurrent is not (LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning))
            {
                continue;
            }

            lWorkItem.LWorkStateCurrent = LWorkState.LWorkStateCancelled;
            lScheduleCancelledCount++;
        }

        LScheduleChange?.Invoke(this);
        return lScheduleCancelledCount;
    }

    /// <summary>Append work items. Returns how many were added.</summary>
    public int LScheduleAdd(IReadOnlyList<LWorkItem> lWorkItems)
    {
        if (lWorkItems.Count == 0)
        {
            return 0;
        }

        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            lScheduleItems.Add(lWorkItem);
        }

        LScheduleChange?.Invoke(this);
        return lWorkItems.Count;
    }

    public bool LScheduleRemove(Guid lWorkId)
    {
        for (int lScheduleIndex = 0; lScheduleIndex < lScheduleItems.Count; lScheduleIndex++)
        {
            if (lScheduleItems[lScheduleIndex].LWorkId != lWorkId)
            {
                continue;
            }

            lScheduleItems.RemoveAt(lScheduleIndex);
            LScheduleChange?.Invoke(this);
            return true;
        }

        return false;
    }

    public void LScheduleClear()
    {
        if (lScheduleItems.Count == 0)
        {
            return;
        }

        lScheduleItems.Clear();
        LScheduleChange?.Invoke(this);
    }

    public IReadOnlyList<LWorkItem> LSchedulePendingRead() =>
        lScheduleItems
            .Where(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStatePending)
            .ToArray();

    /// <summary>
    /// The item a runner should pick up next: highest priority first, oldest first
    /// within a priority.
    /// </summary>
    public LWorkItem? LScheduleNextRead() =>
        lScheduleItems
            .Where(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStatePending)
            .OrderByDescending(lWorkItem => lWorkItem.LWorkPriority)
            .ThenBy(lWorkItem => lWorkItem.LWorkCreateTime)
            .FirstOrDefault();
}
