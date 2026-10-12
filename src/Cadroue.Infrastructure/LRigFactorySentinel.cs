using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactorySentinel
{
    public static LRigSentinel LRigSentinelCreate()
    {
        return new LRigSentinel(
            LSentinel.LSentinelOwnerCheck,
            LSentinel.LSentinelRunnerAdd,
            LSentinel.LSentinelRunnerRemove,
            LSchedule.LScheduleLeaseSeconds);
    }
}
