using System;
using System.Collections.Generic;

using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    public bool PFlowSweepReady => lSpool is not null;

    public TimeSpan PFlowSweepDuration => lSpool?.LSpoolDuration ?? TimeSpan.Zero;

    public bool PFlowCombineApply(
        IReadOnlyList<LSweepSpan> pFlowExcluded,
        IReadOnlyList<LSweepSpan> pFlowKept,
        IReadOnlyList<LSweepBoundary> pFlowBoundaries)
    {
        if (pFlowUnloaded || lSpool is not { } pFlowSpool)
        {
            return false;
        }

        IReadOnlyList<LPiece> pFlowSections = LSweep.LSweepCombineResolve(
            lSegment.LSegmentListRead(),
            pFlowExcluded,
            pFlowKept,
            pFlowBoundaries,
            pFlowSpool.LSpoolDuration,
            Math.Max(1, PSectionPalette.PSectionActiveCount));
        int? pFlowSelect = pFlowSections.Count > 0 ? 0 : null;
        return lSegment.LSegmentBoundSet(pFlowSections, pFlowSelect, pFlowSpool.LSpoolDuration);
    }
}
