using System.Linq;

using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.SectionData;

namespace Cadroue.Tests;

public sealed class SceneBoundaryTests
{
    [Fact]
    public void BoundaryInsideFullSection_YieldsTwoAbuttingSections()
    {
        var sections = new[] { Seg(0, 10) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(4) }, At(10), 4);
        Assert.Equal(2, result.Count);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(4), result[0].LPieceEnd);
        Assert.Equal(At(4), result[1].LPieceOrigin);
        Assert.Equal(At(10), result[1].LPieceEnd);
        Assert.Equal(result[0].LPieceEnd - result[0].LPieceOrigin + (result[1].LPieceEnd - result[1].LPieceOrigin), At(10));
    }

    [Fact]
    public void TrailingSection_IsMarkedDetected_HeadIsPreserved()
    {
        var sections = new[] { Seg(0, 10) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(4) }, At(10), 4);
        Assert.False(result[0].LPieceDetected);
        Assert.True(result[1].LPieceDetected);
    }

    [Fact]
    public void BoundaryOnExistingEdge_IsNoOp()
    {
        var sections = new[] { Seg(0, 10) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(0), At(10) }, At(10), 4);
        Assert.Single(result);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(10), result[0].LPieceEnd);
    }

    [Fact]
    public void BoundaryOutOfRange_IsNoOp()
    {
        var sections = new[] { Seg(0, 10) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(-2), At(12) }, At(10), 4);
        Assert.Single(result);
    }

    [Fact]
    public void MultipleBoundariesInOneSection_ProduceOrderedContiguousPieces()
    {
        var sections = new[] { Seg(0, 12) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(3), At(7), At(9) }, At(12), 4);
        Assert.Equal(4, result.Count);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(3), result[0].LPieceEnd);
        Assert.Equal(At(3), result[1].LPieceOrigin);
        Assert.Equal(At(7), result[1].LPieceEnd);
        Assert.Equal(At(7), result[2].LPieceOrigin);
        Assert.Equal(At(9), result[2].LPieceEnd);
        Assert.Equal(At(9), result[3].LPieceOrigin);
        Assert.Equal(At(12), result[3].LPieceEnd);
    }

    [Fact]
    public void EmptySection_BoundariesInRange_SplitsFullDurationContiguously()
    {
        var sections = System.Array.Empty<LPiece>();
        var result = TInterface.PieceSceneResolve(sections, new[] { At(3), At(7) }, At(10), 4);
        Assert.Equal(3, result.Count);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(3), result[0].LPieceEnd);
        Assert.Equal(At(3), result[1].LPieceOrigin);
        Assert.Equal(At(7), result[1].LPieceEnd);
        Assert.Equal(At(7), result[2].LPieceOrigin);
        Assert.Equal(At(10), result[2].LPieceEnd);
        Assert.False(result[0].LPieceDetected);
        Assert.True(result[1].LPieceDetected);
        Assert.True(result[2].LPieceDetected);
    }

    [Fact]
    public void EmptySection_NoBoundariesInRange_YieldsOneFullDurationSection()
    {
        var sections = System.Array.Empty<LPiece>();
        var result = TInterface.PieceSceneResolve(sections, new[] { At(-1), At(12) }, At(10), 4);
        Assert.Single(result);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(10), result[0].LPieceEnd);
    }

    [Fact]
    public void EmptySection_NoBoundaries_YieldsOneFullDurationSection()
    {
        var sections = System.Array.Empty<LPiece>();
        var result = TInterface.PieceSceneResolve(sections, System.Array.Empty<TimeSpan>(), At(10), 4);
        Assert.Single(result);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(10), result[0].LPieceEnd);
    }

    [Fact]
    public void BoundaryOutsideAnySection_LeavesGapUntouched()
    {
        var sections = new[] { Seg(0, 4), Seg(8, 12) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(6) }, At(12), 4);
        Assert.Equal(2, result.Count);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(8), result[1].LPieceOrigin);
    }

    [Fact]
    public void BoundariesWithinMinimumGap_AreMerged()
    {
        var sections = new[] { Seg(0, 12) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(4), At(4.3), At(4.6), At(5) }, At(12), 4, At(0.5));
        Assert.Equal(3, result.Count);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(4), result[0].LPieceEnd);
        Assert.Equal(At(4), result[1].LPieceOrigin);
        Assert.Equal(At(4.6), result[1].LPieceEnd);
        Assert.Equal(At(4.6), result[2].LPieceOrigin);
        Assert.Equal(At(12), result[2].LPieceEnd);
    }

    [Fact]
    public void TrailingSubMinimumSection_IsMergedIntoPrior()
    {
        var sections = new[] { Seg(0, 10) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(4), At(9.8) }, At(10), 4, At(0.5));
        Assert.Equal(2, result.Count);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(4), result[0].LPieceEnd);
        Assert.Equal(At(4), result[1].LPieceOrigin);
        Assert.Equal(At(10), result[1].LPieceEnd);
    }

    [Fact]
    public void HeadSubMinimumBoundary_IsDropped()
    {
        var sections = new[] { Seg(0, 10) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(0.2), At(5) }, At(10), 4, At(0.5));
        Assert.Equal(2, result.Count);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(5), result[0].LPieceEnd);
        Assert.Equal(At(5), result[1].LPieceOrigin);
        Assert.Equal(At(10), result[1].LPieceEnd);
    }

    [Fact]
    public void NoSectionShorterThanMinimum()
    {
        var sections = new[] { Seg(0, 20) };
        var boundaries = new[] { At(1), At(1.2), At(1.3), At(5), At(5.1), At(9), At(19.9) };
        var result = TInterface.PieceSceneResolve(sections, boundaries, At(20), 4, At(0.5));
        foreach (var piece in result)
        {
            Assert.True(piece.LPieceEnd - piece.LPieceOrigin >= At(0.5));
        }
    }

    [Fact]
    public void RunawayBoundaries_TruncateToCeiling()
    {
        TimeSpan[] boundaries = Enumerable.Range(1, 6000).Select(second => At(second)).ToArray();
        var result = TInterface.PieceSceneResolve(System.Array.Empty<LPiece>(), boundaries, At(7000), 4);
        Assert.Equal(LPiece.LPieceCeiling, result.Count);
    }
}
