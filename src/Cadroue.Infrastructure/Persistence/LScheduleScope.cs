using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LScheduleScope
{
    public static Func<bool> LScheduleSharedSeam { get; set; } = () => false;

    internal static bool LScheduleSignetMatch(LWorkDocument lWorkRecord) =>
        lWorkRecord.LWorkSignet == Guid.Empty
        || lWorkRecord.LWorkSignet == LSignet.LSignetCurrent;

    internal static bool LScheduleScopeMatch(LWorkDocument lWorkRecord) =>
        LScheduleSharedSeam()
        || LScheduleSignetMatch(lWorkRecord);

    internal static bool LScheduleRelayMatch(LWorkDocument lWorkRecord) =>
        lWorkRecord.LWorkRelayProcess == 0
        || lWorkRecord.LWorkRelayProcess == Environment.ProcessId
        || !LSentinel.LSentinelOwnerCheck(lWorkRecord.LWorkRelayProcess, lWorkRecord.LWorkRelayStamp);

    internal static bool LScheduleClaimMatch(LWorkDocument lWorkRecord) =>
        LScheduleScopeMatch(lWorkRecord) && LScheduleRelayMatch(lWorkRecord);
}
