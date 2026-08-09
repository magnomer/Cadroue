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
}
