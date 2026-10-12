using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactoryInventory
{
    public static LRigInventory LRigInventoryCreate()
    {
        return new LRigInventory(
            LInventory.LInventoryFilterExist);
    }
}
