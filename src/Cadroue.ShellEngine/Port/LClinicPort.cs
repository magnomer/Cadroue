namespace Cadroue.ShellEngine;

public interface LClinicPort
{
    (IReadOnlyList<(string LClinicKind, bool LClinicRepair, bool LClinicPersistent)> LClinicSteps,
        bool LClinicActive,
        bool LClinicSeparate,
        bool LClinicFixed,
        bool LClinicPersistent) LClinicPlanRead();

    void LClinicPlanSet(
        IReadOnlyList<(string LClinicKind, bool LClinicRepair, bool LClinicPersistent)> lSteps,
        bool lActive,
        bool lSeparate,
        bool lFixed,
        bool lPersistent);
}
