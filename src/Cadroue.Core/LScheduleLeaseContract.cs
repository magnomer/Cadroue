namespace Cadroue.Core;

public interface LScheduleLeaseContract
{
    bool LSchedulePendingExist();

    LWorkItem? LScheduleClaim(Guid lRunnerId);

    void LScheduleLeaseUpdate(Guid lWorkId, Guid lRunnerId);

    void LSchedulePhaseSet(Guid lWorkId, Guid lRunnerId, LWorkPhase lWorkPhase);

    void LScheduleOutputCommit(Guid lWorkId, Guid lRunnerId, string lWorkOutputPath, string lWorkOutputName);

    int LScheduleRelease(Guid lRunnerId);

    bool LScheduleItemRelease(Guid lWorkId, Guid lRunnerId, string lScheduleMessage);

    int LScheduleRetryRelease(Guid lWorkId, Guid lRunnerId, int lScheduleRetryMaximum, string lScheduleReason);

    bool LScheduleOwnerCheck(LWorkItem lWorkItem, Guid lRunnerId);

    bool LScheduleForeignCheck(LWorkItem lWorkItem, Guid lRunnerId);
}
