namespace Cadroue.Core;

public sealed record LRigInventory(
    Func<string, bool> LRigInventoryFilter);
