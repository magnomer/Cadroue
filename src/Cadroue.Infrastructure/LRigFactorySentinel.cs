using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactorySentinel
{
    public static LRigSentinel LRigSentinelCreate()
    {
        return new LRigSentinel(
            LSentinel.LSentinelOwnerCheck,
            LSentinelRunner.LSentinelRunnerAdd,
            LSentinelRunner.LSentinelRunnerRemove,
            LScheduleLease.LScheduleLeaseSeconds);
    }
}
