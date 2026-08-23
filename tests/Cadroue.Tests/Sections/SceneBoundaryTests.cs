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
    public void BoundaryOutsideAnySection_LeavesGapUntouched()
    {
        var sections = new[] { Seg(0, 4), Seg(8, 12) };
        var result = TInterface.PieceSceneResolve(sections, new[] { At(6) }, At(12), 4);
        Assert.Equal(2, result.Count);
        Assert.Equal(At(0), result[0].LPieceOrigin);
        Assert.Equal(At(8), result[1].LPieceOrigin);
    }
}
