using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.SectionData;

namespace Cadroue.Tests;

public sealed class SectionValidationTests
{
    private const bool OverlapOff = false;
    private const bool OverlapOn = true;

    [Fact]
    public void SectionPastMediaDuration_IsDropped()
    {
        var sections = new[] { Seg(0, 5), Seg(8, 12) };
        var valid = LPiece.LPieceValidSelect(sections, At(10));
        Assert.Single(valid);
        Assert.Equal(At(5), valid[0].LPieceEnd);
    }

    [Fact]
    public void SectionsWithoutPositiveLength_AreDropped()
    {
        var sections = new[] { Seg(3, 3), Seg(6, 4), Seg(1, 2) };
        var valid = LPiece.LPieceValidSelect(sections, At(10));
        Assert.Single(valid);
        Assert.Equal(At(1), valid[0].LPieceStart);
        Assert.Equal(At(2), valid[0].LPieceEnd);
    }

    [Fact]
    public void ValidSections_PreserveOrder()
    {
        var sections = new[] { Seg(0, 3), Seg(4, 6), Seg(7, 10) };
        var valid = LPiece.LPieceValidSelect(sections, At(10));
        Assert.Equal(3, valid.Count);
        Assert.Equal(At(0), valid[0].LPieceStart);
        Assert.Equal(At(4), valid[1].LPieceStart);
        Assert.Equal(At(7), valid[2].LPieceStart);
    }

    [Fact]
    public void SectionEndingAtMediaDuration_IsKept()
    {
        var sections = new[] { Seg(5, 10) };
        var valid = LPiece.LPieceValidSelect(sections, At(10));
        Assert.Single(valid);
    }

    [Fact]
    public void OverlapAllowed_InsideCheckIsFalse()
    {
        var sections = new[] { Seg(0, 10) };
        Assert.False(LPiece.LPieceInsideCheck(sections, At(5), -1, OverlapOn));
    }

    [Fact]
    public void TimeStrictlyInsideSection_IsInside()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.True(LPiece.LPieceInsideCheck(sections, At(5), -1, OverlapOff));
    }

    [Fact]
    public void TimeAtEndBoundary_IsNotInside()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.False(LPiece.LPieceInsideCheck(sections, At(8), -1, OverlapOff));
    }

    [Fact]
    public void TimeAtStartBoundary_IsInside()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.True(LPiece.LPieceInsideCheck(sections, At(2), -1, OverlapOff));
    }

    [Fact]
    public void SkippedSection_IsIgnoredByInsideCheck()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.False(LPiece.LPieceInsideCheck(sections, At(5), 0, OverlapOff));
    }

    [Fact]
    public void OverlapAllowed_LimitIsCeiling()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(10), LPiece.LPieceLimitRead(sections, At(0), At(10), -1, OverlapOn));
    }

    [Fact]
    public void NextSectionStart_CapsLimit()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(5), LPiece.LPieceLimitRead(sections, At(0), At(10), -1, OverlapOff));
    }

    [Fact]
    public void SectionStartingAtCeiling_DoesNotLowerLimit()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(5), LPiece.LPieceLimitRead(sections, At(0), At(5), -1, OverlapOff));
    }

    [Fact]
    public void EmptySections_FloorIsZero()
    {
        Assert.Equal(TimeSpan.Zero, LPiece.LPieceFloorRead(Array.Empty<LPiece>(), At(5), -1, OverlapOff));
    }

    [Fact]
    public void PriorSectionNearestCursor_SetsFloor()
    {
        var sections = new[] { Seg(0, 2), Seg(3, 4) };
        Assert.Equal(At(4), LPiece.LPieceFloorRead(sections, At(6), -1, OverlapOff));
    }

    [Fact]
    public void OverlapAllowed_FloorIsZero()
    {
        var sections = new[] { Seg(0, 4) };
        Assert.Equal(TimeSpan.Zero, LPiece.LPieceFloorRead(sections, At(6), -1, OverlapOn));
    }
}
