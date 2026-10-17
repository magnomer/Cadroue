namespace Cadroue.Core;

public interface LScheduleTransitionContract
{
    void LScheduleCommit(LWorkItem lWorkItem, bool lScheduleSucceeded, string lScheduleMessage);

    bool LScheduleItemCancel(LWorkItem lWorkItem);

    bool LScheduleItemReset(Guid lWorkId);
}
