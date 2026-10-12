namespace Cadroue.Core;

public sealed record LRigSentinel(
    Func<int, long, bool> LRigSentinelOwner,
    Action<Guid> LRigSentinelEnrollment,
    Action<Guid> LRigSentinelWithdrawal,
    int LRigSentinelLease);
