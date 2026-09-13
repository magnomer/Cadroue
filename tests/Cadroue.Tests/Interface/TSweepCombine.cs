using System.Linq;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSweepCombine
{
    [Fact]
    public void LSweepCombineResolve_UnionsHolesTreatSpansBoundariesAndUserPieces()
    {
        var lUser = new LPiece(TimeSpan.Zero, TimeSpan.FromSeconds(10), 0, "keep") { LPieceDetected = false };

        IReadOnlyList<LPiece> lResult = LSweep.LSweepCombineResolve(
            new[] { lUser },
            new[] { new LSweepSpan(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)) },
            new[] { new LSweepSpan(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(7)) },
            new[]
            {
                new LSweepBoundary(TimeSpan.FromSeconds(4), TimeSpan.Zero),
                new LSweepBoundary(TimeSpan.FromSeconds(8), TimeSpan.Zero)
            },
            TimeSpan.FromSeconds(10),
            4);

        Assert.Equal(7, lResult.Count);

        LPiece lKept = Assert.Single(lResult, lSection => lSection.LPieceName == "keep");
        Assert.False(lKept.LPieceDetected);
        Assert.Equal(TimeSpan.Zero, lKept.LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(10), lKept.LPieceEnd);

        IReadOnlyList<LPiece> lDetected = lResult.Where(lSection => lSection.LPieceDetected).ToList();
        Assert.Equal(6, lDetected.Count);

        Assert.DoesNotContain(lDetected, lSection =>
            lSection.LPieceOrigin < TimeSpan.FromSeconds(3) && lSection.LPieceEnd > TimeSpan.FromSeconds(2));

        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(6) && lSection.LPieceEnd == TimeSpan.FromSeconds(7));

        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(3) && lSection.LPieceEnd == TimeSpan.FromSeconds(4));
        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(4) && lSection.LPieceEnd == TimeSpan.FromSeconds(6));
        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(7) && lSection.LPieceEnd == TimeSpan.FromSeconds(8));
        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(8) && lSection.LPieceEnd == TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void LSweepCombineResolve_DropsSceneCutBelowMinimumButKeepsZeroMinimum()
    {
        IReadOnlyList<LPiece> lResult = LSweep.LSweepCombineResolve(
            Array.Empty<LPiece>(),
            Array.Empty<LSweepSpan>(),
            Array.Empty<LSweepSpan>(),
            new[]
            {
                new LSweepBoundary(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)),
                new LSweepBoundary(TimeSpan.FromSeconds(5), TimeSpan.Zero)
            },
            TimeSpan.FromSeconds(10),
            4);

        Assert.All(lResult, lSection => Assert.True(lSection.LPieceDetected));

        Assert.DoesNotContain(lResult, lSection => lSection.LPieceEnd == TimeSpan.FromSeconds(1));
        Assert.Contains(lResult, lSection =>
            lSection.LPieceOrigin == TimeSpan.Zero && lSection.LPieceEnd == TimeSpan.FromSeconds(5));
        Assert.Contains(lResult, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(5) && lSection.LPieceEnd == TimeSpan.FromSeconds(10));
    }

}
