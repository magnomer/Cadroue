namespace Cadroue.Core;

public interface LScheduleIntakeContract
{
    IReadOnlyList<LWorkItem> LScheduleAcceptedAdd(
        IReadOnlyList<LWorkItem> lWorkItems,
        Guid lScheduleRelayTarget = default,
        Guid lScheduleRelaySource = default);

    IReadOnlyList<LWorkItem> LScheduleDeliveredAdd(IReadOnlyList<LWorkItem> lWorkItems);

    bool LScheduleOrderSet(Guid lWorkBatchId, IReadOnlyList<Guid> lWorkIds);
}
