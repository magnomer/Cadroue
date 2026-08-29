using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.SectionData;

namespace Cadroue.Tests;

public sealed class SectionBoundaryTests
{
    private const bool OverlapOff = false;

    [Fact]
    public void CursorInsideActiveSection_SettingStartShrinksSection()
    {
        var sections = new[] { Seg(2, 8) };
        var plan = TInterface.PieceStartSet(sections, 0, At(4), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(4), plan!.Value.Sections[0].LPieceOrigin);
        Assert.Equal(At(8), plan.Value.Sections[0].LPieceEnd);
        Assert.False(plan.Value.Added);
    }

    [Fact]
    public void CursorAtActiveEnd_SettingStartAddsForward()
    {
        var sections = new[] { Seg(2, 8) };
        var plan = TInterface.PieceStartSet(sections, 0, At(8), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.True(plan!.Value.Added);
        Assert.Contains(plan.Value.Sections, section => section.LPieceOrigin == At(8) && section.LPieceEnd == At(10));
    }

    [Fact]
    public void CursorBelowFloor_SettingStartIsRejected()
    {
        var sections = new[] { Seg(0, 3), Seg(5, 9) };
        Assert.Null(TInterface.PieceStartSet(sections, 1, At(2), At(10), 0, OverlapOff));
    }

    [Fact]
    public void CursorInsideActiveSection_SettingEndShrinksSection()
    {
        var sections = new[] { Seg(2, 8) };
        var plan = TInterface.PieceEndSet(sections, 0, At(6), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(6), plan!.Value.Sections[0].LPieceEnd);
        Assert.False(plan.Value.Added);
    }

    [Fact]
    public void CursorBeforeActiveStart_SettingEndCreatesLeadingSection()
    {
        var sections = new[] { Seg(4, 8) };
        var plan = TInterface.PieceEndSet(sections, 0, At(2), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.True(plan!.Value.Added);
        Assert.Contains(plan.Value.Sections, section => section.LPieceOrigin == TimeSpan.Zero && section.LPieceEnd == At(2));
    }

    [Fact]
    public void CursorBeyondNextStart_SettingEndIsRejected()
    {
        var sections = new[] { Seg(2, 4), Seg(7, 9) };
        Assert.Null(TInterface.PieceEndSet(sections, 0, At(8), 0, OverlapOff));
    }

    [Fact]
    public void EndAdjacentToNextStart_IsAllowed()
    {
        var sections = new[] { Seg(5, 8) };
        var plan = TInterface.PieceEndSet(sections, null, At(5), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Contains(plan!.Value.Sections, section => section.LPieceOrigin == TimeSpan.Zero && section.LPieceEnd == At(5));
    }

    [Fact]
    public void CursorAtZero_CreatingEndIsRejected()
    {
        Assert.Null(TInterface.PieceEndCreate(Array.Empty<LPiece>(), TimeSpan.Zero, 0, OverlapOff));
    }

    [Fact]
    public void SpanOverlappingSection_CreatingEndIsRejected()
    {
        var sections = new[] { Seg(0, 2), Seg(5, 8) };
        Assert.Null(TInterface.PieceEndCreate(sections, At(6), 0, OverlapOff));
    }
}
