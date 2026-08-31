using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TSectionData;

namespace Cadroue.Tests;

public sealed class TSegmentDivision
{
    [Fact]
    public void NoActiveSection_DivisionIsRejected()
    {
        var sections = new[] { TSegmentPieceCreate(0, 8) };
        Assert.Null(TInterface.TPieceDivide(sections, null, TSegmentAtCreate(4), 0));
    }

    [Fact]
    public void CursorInsideActiveSection_DividesIntoTwoHalves()
    {
        var sections = new[] { TSegmentPieceCreate(0, 8) };
        var plan = TInterface.TPieceDivide(sections, 0, TSegmentAtCreate(3), 1);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Value.Sections.Count);
        Assert.Equal(TSegmentAtCreate(0), plan.Value.Sections[0].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(3), plan.Value.Sections[0].LPieceEnd);
        Assert.Equal(TSegmentAtCreate(3), plan.Value.Sections[1].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(8), plan.Value.Sections[1].LPieceEnd);
        Assert.Equal(0, plan.Value.First);
        Assert.Equal(1, plan.Value.Second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void CursorAtSectionBoundary_DivisionIsRejected(double cursorSeconds)
    {
        var sections = new[] { TSegmentPieceCreate(0, 8) };
        Assert.Null(TInterface.TPieceDivide(sections, 0, TSegmentAtCreate(cursorSeconds), 0));
    }
}
