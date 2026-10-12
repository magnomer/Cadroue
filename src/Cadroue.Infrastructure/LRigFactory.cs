using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactory
{
    public static void LRigFactoryAttach(
        Func<
            string,
            IReadOnlyCollection<LFlawKind>,
            CancellationToken,
            IProgress<double>?,
            Task<IReadOnlyList<LDossier>>> lRigScanner)
    {
        LRigFactoryTrace.LRigTraceAttach();
        LRigFactoryPreference.LRigPreferenceAttach();
        LRigFactorySidecar.LRigSidecarAttach();
        LRigFactoryPreset.LRigPresetAttach();
        LRigFactoryDepot.LRigDepotAttach();
        LRigFactoryCheckup.LRigCheckupAttach(lRigScanner);
    }

    public static LRig LRigFactoryCreate()
    {
        return new LRig(
            LRigFactoryTrace.LRigTraceCreate(),
            LRigFactoryUsher.LRigUsherCreate(),
            LRigFactoryDepot.LRigDepotCreate(),
            LRigFactoryTool.LRigToolCreate(),
            LRigFactoryEmployer.LRigEmployerCreate(),
            LRigFactoryMedia.LRigMediaCreate(),
            LRigFactoryInventory.LRigInventoryCreate(),
            LRigFactorySentinel.LRigSentinelCreate(),
            LRigFactoryCheckup.LRigCheckupCreate(),
            LRigFactoryLineage.LRigLineageCreate(),
            new LSchedule());
    }
}
