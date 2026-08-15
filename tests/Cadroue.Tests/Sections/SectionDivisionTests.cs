using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.SectionData;

namespace Cadroue.Tests;

public sealed class SectionDivisionTests
{
    [Fact]
    public void NoActiveSection_DivisionIsRejected()
    {
        var sections = new[] { Seg(0, 8) };
        Assert.Null(TInterface.PieceDivide(sections, null, At(4), 0));
    }

    [Fact]
    public void CursorInsideActiveSection_DividesIntoTwoHalves()
    {
        var sections = new[] { Seg(0, 8) };
        var plan = TInterface.PieceDivide(sections, 0, At(3), 1);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Value.Sections.Count);
        Assert.Equal(At(0), plan.Value.Sections[0].LPieceOrigin);
        Assert.Equal(At(3), plan.Value.Sections[0].LPieceEnd);
        Assert.Equal(At(3), plan.Value.Sections[1].LPieceOrigin);
        Assert.Equal(At(8), plan.Value.Sections[1].LPieceEnd);
        Assert.Equal(0, plan.Value.First);
        Assert.Equal(1, plan.Value.Second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void CursorAtSectionBoundary_DivisionIsRejected(double cursorSeconds)
    {
        var sections = new[] { Seg(0, 8) };
        Assert.Null(TInterface.PieceDivide(sections, 0, At(cursorSeconds), 0));
    }
}
