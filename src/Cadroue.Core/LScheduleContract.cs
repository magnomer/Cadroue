using System.Collections.ObjectModel;

namespace Cadroue.Core;

public enum LScheduleNotice
{
    LScheduleNoticeProgress,
    LScheduleNoticeStatus
}

public interface LScheduleContract
{
    ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    int LScheduleDoneCount { get; }

    event Action<LScheduleContract>? LScheduleChange;

    event Action<LWorkItem, LScheduleNotice>? LScheduleItemChange;

    void LScheduleChangeRaise();

    void LScheduleItemRaise(LWorkItem lWorkItem, LScheduleNotice lScheduleNotice);

    void LScheduleLoad();

    void LScheduleDurationSet(Guid lWorkId, TimeSpan lWorkDuration);

    int LScheduleAdd(
        IReadOnlyList<LWorkItem> lWorkItems,
        Guid lScheduleRelayTarget = default,
        Guid lScheduleRelaySource = default);

    IReadOnlyList<LWorkItem> LScheduleAcceptedAdd(
        IReadOnlyList<LWorkItem> lWorkItems,
        Guid lScheduleRelayTarget = default,
        Guid lScheduleRelaySource = default);

    Guid LScheduleLineageRead(LWorkItem lWorkItem);

    void LScheduleCommit(LWorkItem lWorkItem, bool lScheduleSucceeded, string lScheduleMessage);

    bool LScheduleItemCancel(LWorkItem lWorkItem);

    bool LScheduleItemReset(Guid lWorkId);

    bool LScheduleRemove(Guid lWorkId);

    IReadOnlyList<Guid> LScheduleRemovableRead(IEnumerable<Guid> lWorkIds);

    int LScheduleBatchRemove(IEnumerable<Guid> lWorkIds);

    int LScheduleDoneClear();

    int LScheduleAllClear();

    IReadOnlyList<LWorkItem> LSchedulePendingRead();

    bool LSchedulePendingExist();

    LWorkItem? LScheduleClaim(Guid lRunnerId);

    void LScheduleLeaseUpdate(Guid lWorkId, Guid lRunnerId);

    void LSchedulePhaseSet(Guid lWorkId, Guid lRunnerId, LWorkPhase lWorkPhase);

    int LScheduleRelease(Guid lRunnerId);

    bool LScheduleItemRelease(Guid lWorkId, Guid lRunnerId, string lScheduleMessage);

    int LScheduleStaleClaim();

    bool LScheduleOwnerCheck(LWorkItem lWorkItem, Guid lRunnerId);

    bool LScheduleForeignCheck(LWorkItem lWorkItem, Guid lRunnerId);
}
