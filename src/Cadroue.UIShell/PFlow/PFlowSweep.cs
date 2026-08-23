using System;
using System.Collections.Generic;

using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    public bool PFlowSweepReady => lSpool is not null;

    public TimeSpan PFlowSweepDuration => lSpool?.LSpoolDuration ?? TimeSpan.Zero;

    public void PFlowSweepApply(IReadOnlyList<(TimeSpan Start, TimeSpan End)> pFlowBlanks)
    {
        if (lSpool is not { } pFlowSpool)
        {
            return;
        }

        IReadOnlyList<LPiece> pFlowSections = LSweep.LSweepSectionResolve(
            lSegment.LSegmentListRead(),
            pFlowBlanks,
            pFlowSpool.LSpoolDuration,
            Math.Max(1, PSectionPalette.PSectionActiveCount));
        int? pFlowSelect = pFlowSections.Count > 0 ? 0 : null;
        lSegment.LSegmentBoundSet(pFlowSections, pFlowSelect, pFlowSpool.LSpoolDuration);
    }

    public void PFlowSceneApply(IReadOnlyList<TimeSpan> pFlowBoundaries)
    {
        if (lSpool is not { } pFlowSpool)
        {
            return;
        }

        IReadOnlyList<LPiece> pFlowSections = LPiece.LPieceSceneResolve(
            lSegment.LSegmentListRead(),
            pFlowBoundaries,
            pFlowSpool.LSpoolDuration,
            Math.Max(1, PSectionPalette.PSectionActiveCount));
        int? pFlowSelect = pFlowSections.Count > 0 ? 0 : null;
        lSegment.LSegmentBoundSet(pFlowSections, pFlowSelect, pFlowSpool.LSpoolDuration);
    }
}
