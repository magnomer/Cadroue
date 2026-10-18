using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests.Conduct;

internal static partial class TInterface
{
    internal static (IReadOnlyList<(string TClinicKind, bool TClinicRepair, bool TClinicPersistent)> TClinicSteps,
        bool TClinicActive, bool TClinicSeparate, bool TClinicFixed, bool TClinicPersistent) TClinicPlanRead(
        LClinicPort port) => port.LClinicPlanRead();
    internal static void TClinicPlanSet(
        LClinicPort port,
        IReadOnlyList<(string TClinicKind, bool TClinicRepair, bool TClinicPersistent)> steps,
        bool active,
        bool separate,
        bool repaired,
        bool persistent) => port.LClinicPlanSet(steps, active, separate, repaired, persistent);
}
