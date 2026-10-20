namespace Cadroue.Core;

public sealed record LRigInventory(
    Func<string, bool> LRigInventoryFilter,
    Func<string, IReadOnlyList<string>> LRigInventoryLayout,
    Func<string, IReadOnlyList<int>> LRigInventorySample,
    Action<Action<string>> LRigInventoryEnrollment,
    Action<Action<string>> LRigInventoryWithdrawal);
