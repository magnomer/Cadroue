namespace Cadroue.Infrastructure;

public static class LSentinelRunner
{
    private static readonly HashSet<Guid> lSentinelLiveRunners = new();
    private static readonly object lSentinelRunnerLock = new();

    public static void LSentinelRunnerAdd(Guid lRunnerId)
    {
        lock (lSentinelRunnerLock)
        {
            lSentinelLiveRunners.Add(lRunnerId);
        }
    }

    public static void LSentinelRunnerRemove(Guid lRunnerId)
    {
        lock (lSentinelRunnerLock)
        {
            lSentinelLiveRunners.Remove(lRunnerId);
        }
    }

    public static bool LSentinelRunnerCheck(Guid lRunnerId)
    {
        lock (lSentinelRunnerLock)
        {
            return lSentinelLiveRunners.Contains(lRunnerId);
        }
    }
}
