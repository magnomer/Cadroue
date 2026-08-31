using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TSectionData;

namespace Cadroue.Tests;

public sealed class TSegmentBoundary
{
    private const bool TSegmentOverlapOff = false;

    [Fact]
    public void CursorInsideActiveSection_SettingStartShrinksSection()
    {
        var sections = new[] { TSegmentPieceCreate(2, 8) };
        var plan = TInterface.TPieceStartSet(sections, 0, TSegmentAtCreate(4), TSegmentAtCreate(10), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(TSegmentAtCreate(4), plan!.Value.Sections[0].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(8), plan.Value.Sections[0].LPieceEnd);
        Assert.False(plan.Value.Added);
    }

    [Fact]
    public void CursorAtActiveEnd_SettingStartAddsForward()
    {
        var sections = new[] { TSegmentPieceCreate(2, 8) };
        var plan = TInterface.TPieceStartSet(sections, 0, TSegmentAtCreate(8), TSegmentAtCreate(10), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.True(plan!.Value.Added);
        Assert.Contains(plan.Value.Sections, section => section.LPieceOrigin == TSegmentAtCreate(8) && section.LPieceEnd == TSegmentAtCreate(10));
    }

    [Fact]
    public void CursorBelowFloor_SettingStartIsRejected()
    {
        var sections = new[] { TSegmentPieceCreate(0, 3), TSegmentPieceCreate(5, 9) };
        Assert.Null(TInterface.TPieceStartSet(sections, 1, TSegmentAtCreate(2), TSegmentAtCreate(10), 0, TSegmentOverlapOff));
    }

    [Fact]
    public void CursorInsideActiveSection_SettingEndShrinksSection()
    {
        var sections = new[] { TSegmentPieceCreate(2, 8) };
        var plan = TInterface.TPieceEndSet(sections, 0, TSegmentAtCreate(6), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(TSegmentAtCreate(6), plan!.Value.Sections[0].LPieceEnd);
        Assert.False(plan.Value.Added);
    }

    [Fact]
    public void CursorBeforeActiveStart_SettingEndCreatesLeadingSection()
    {
        var sections = new[] { TSegmentPieceCreate(4, 8) };
        var plan = TInterface.TPieceEndSet(sections, 0, TSegmentAtCreate(2), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.True(plan!.Value.Added);
        Assert.Contains(plan.Value.Sections, section => section.LPieceOrigin == TimeSpan.Zero && section.LPieceEnd == TSegmentAtCreate(2));
    }

    [Fact]
    public void CursorBeyondNextStart_SettingEndIsRejected()
    {
        var sections = new[] { TSegmentPieceCreate(2, 4), TSegmentPieceCreate(7, 9) };
        Assert.Null(TInterface.TPieceEndSet(sections, 0, TSegmentAtCreate(8), 0, TSegmentOverlapOff));
    }

    [Fact]
    public void EndAdjacentToNextStart_IsAllowed()
    {
        var sections = new[] { TSegmentPieceCreate(5, 8) };
        var plan = TInterface.TPieceEndSet(sections, null, TSegmentAtCreate(5), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Contains(plan!.Value.Sections, section => section.LPieceOrigin == TimeSpan.Zero && section.LPieceEnd == TSegmentAtCreate(5));
    }

    [Fact]
    public void CursorAtZero_CreatingEndIsRejected()
    {
        Assert.Null(TInterface.TPieceEndCreate(Array.Empty<LPiece>(), TimeSpan.Zero, 0, TSegmentOverlapOff));
    }

    [Fact]
    public void SpanOverlappingSection_CreatingEndIsRejected()
    {
        var sections = new[] { TSegmentPieceCreate(0, 2), TSegmentPieceCreate(5, 8) };
        Assert.Null(TInterface.TPieceEndCreate(sections, TSegmentAtCreate(6), 0, TSegmentOverlapOff));
    }
}
