using System.Collections.ObjectModel;

namespace Cadroue.Core;

public enum LScheduleNotice
{
    LScheduleNoticeProgress,
    LScheduleNoticeStatus
}

public enum LScheduleRemoval
{
    LScheduleRemovalRemoved,
    LScheduleRemovalHeld,
    LScheduleRemovalBlocked,
    LScheduleRemovalMissing
}

public interface LScheduleContract
{
    ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    int LScheduleDoneCount { get; }

    event Action<LScheduleContract>? LScheduleChanged;

    event Action<LWorkItem, LScheduleNotice>? LScheduleItemChanged;

    void LScheduleChangeRaise();

    void LScheduleItemRaise(LWorkItem lWorkItem, LScheduleNotice lScheduleNotice);

    void LScheduleLoad();

    IReadOnlyList<LWorkItem> LSchedulePendingRead();
}
