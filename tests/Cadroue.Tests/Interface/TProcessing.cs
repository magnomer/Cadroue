using Cadroue.Infrastructure;

namespace Cadroue.Tests;

public sealed class TProcessing
{
    public bool CurrentOwnerLives()
    {
        long stamp = LSentinel.LSentinelStampRead();
        return LSentinel.LSentinelOwnerCheck(Environment.ProcessId, stamp);
    }

    public bool MissingOwnerDetected() => !LSentinel.LSentinelOwnerCheck(int.MaxValue, 1);

    public bool LiveRunnerOwnerLives()
    {
        Guid runnerId = Guid.NewGuid();
        LSentinel.LSentinelRunnerAdd(runnerId);
        try
        {
            return LSentinel.LSentinelOwnerCheck(Environment.ProcessId, LSentinel.LSentinelStampRead(), runnerId);
        }
        finally
        {
            LSentinel.LSentinelRunnerRemove(runnerId);
        }
    }

    public bool DeadRunnerOwnerDetected()
    {
        Guid runnerId = Guid.NewGuid();
        return !LSentinel.LSentinelOwnerCheck(Environment.ProcessId, LSentinel.LSentinelStampRead(), runnerId);
    }
}
