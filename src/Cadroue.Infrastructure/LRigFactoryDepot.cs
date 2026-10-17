using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactoryDepot
{
    public static void LRigDepotAttach()
    {
        LScheduleScope.LScheduleSharedSeam = () => LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared;
        LScheduleLease.LScheduleTimeSeam = LGate.LGateTimeRead;
        LRetentionSweep.LRetentionSweptSeam = LRetention.LRetentionSweptCheck;
        LRetentionSweep.LRetentionExpiredSeam = LRetention.LRetentionExpiredCheck;
    }

    public static LRigDepot LRigDepotCreate()
    {
        return new LRigDepot(
            LDepot.LDepotAudioRead,
            LDepot.LDepotMergeRead,
            LDepot.LDepotBridgeRead,
            LDepot.LDepotPassRead,
            LDepot.LDepotRootRead);
    }
}
