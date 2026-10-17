namespace Cadroue.Core;

public interface LScheduleRemoverContract
{
    bool LScheduleRemove(Guid lWorkId);

    IReadOnlyList<Guid> LScheduleRemovableRead(IEnumerable<Guid> lWorkIds);

    IReadOnlyDictionary<Guid, LScheduleRemoval> LScheduleBatchRemove(IEnumerable<Guid> lWorkIds);

    int LScheduleDoneClear();

    int LScheduleAllClear();
}
