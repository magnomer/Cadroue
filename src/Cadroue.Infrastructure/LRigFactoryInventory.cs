using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactoryInventory
{
    public static LRigInventory LRigInventoryCreate()
    {
        return new LRigInventory(
            LInventory.LInventoryFilterExist,
            lEncoder => LInventoryHelp.LInventoryLayoutRead(LCapability.LCapabilityNameRead(lEncoder)),
            lEncoder => LInventoryHelp.LInventorySampleRead(LCapability.LCapabilityNameRead(lEncoder)),
            lHandler => LInventoryHelp.LInventoryHelpReady += lHandler,
            lHandler => LInventoryHelp.LInventoryHelpReady -= lHandler);
    }
}
