using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TSectionData;

namespace Cadroue.Tests;

public sealed class TSegmentValidation
{
    private const bool TSegmentOverlapOff = false;
    private const bool TSegmentOverlapOn = true;

    [Fact]
    public void SectionPastMediaDuration_IsDropped()
    {
        var sections = new[] { TSegmentPieceCreate(0, 5), TSegmentPieceCreate(8, 12) };
        var valid = TInterface.TPieceValidSelect(sections, TSegmentAtCreate(10));
        Assert.Single(valid);
        Assert.Equal(TSegmentAtCreate(5), valid[0].LPieceEnd);
    }

    [Fact]
    public void SectionsWithoutPositiveLength_AreDropped()
    {
        var sections = new[] { TSegmentPieceCreate(3, 3), TSegmentPieceCreate(6, 4), TSegmentPieceCreate(1, 2) };
        var valid = TInterface.TPieceValidSelect(sections, TSegmentAtCreate(10));
        Assert.Single(valid);
        Assert.Equal(TSegmentAtCreate(1), valid[0].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(2), valid[0].LPieceEnd);
    }

    [Fact]
    public void ValidSections_PreserveOrder()
    {
        var sections = new[] { TSegmentPieceCreate(0, 3), TSegmentPieceCreate(4, 6), TSegmentPieceCreate(7, 10) };
        var valid = TInterface.TPieceValidSelect(sections, TSegmentAtCreate(10));
        Assert.Equal(3, valid.Count);
        Assert.Equal(TSegmentAtCreate(0), valid[0].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(4), valid[1].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(7), valid[2].LPieceOrigin);
    }

    [Fact]
    public void SectionEndingAtMediaDuration_IsKept()
    {
        var sections = new[] { TSegmentPieceCreate(5, 10) };
        var valid = TInterface.TPieceValidSelect(sections, TSegmentAtCreate(10));
        Assert.Single(valid);
    }

    [Fact]
    public void OverlapAllowed_InsideCheckIsFalse()
    {
        var sections = new[] { TSegmentPieceCreate(0, 10) };
        Assert.False(TInterface.TPieceInsideCheck(sections, TSegmentAtCreate(5), -1, TSegmentOverlapOn));
    }

    [Fact]
    public void TimeStrictlyInsideSection_IsInside()
    {
        var sections = new[] { TSegmentPieceCreate(2, 8) };
        Assert.True(TInterface.TPieceInsideCheck(sections, TSegmentAtCreate(5), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void TimeAtEndBoundary_IsNotInside()
    {
        var sections = new[] { TSegmentPieceCreate(2, 8) };
        Assert.False(TInterface.TPieceInsideCheck(sections, TSegmentAtCreate(8), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void TimeAtStartBoundary_IsInside()
    {
        var sections = new[] { TSegmentPieceCreate(2, 8) };
        Assert.True(TInterface.TPieceInsideCheck(sections, TSegmentAtCreate(2), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void SkippedSection_IsIgnoredByInsideCheck()
    {
        var sections = new[] { TSegmentPieceCreate(2, 8) };
        Assert.False(TInterface.TPieceInsideCheck(sections, TSegmentAtCreate(5), 0, TSegmentOverlapOff));
    }

    [Fact]
    public void OverlapAllowed_LimitIsCeiling()
    {
        var sections = new[] { TSegmentPieceCreate(5, 8) };
        Assert.Equal(TSegmentAtCreate(10), TInterface.TPieceLimitRead(sections, TSegmentAtCreate(0), TSegmentAtCreate(10), -1, TSegmentOverlapOn));
    }

    [Fact]
    public void NextSectionStart_CapsLimit()
    {
        var sections = new[] { TSegmentPieceCreate(5, 8) };
        Assert.Equal(TSegmentAtCreate(5), TInterface.TPieceLimitRead(sections, TSegmentAtCreate(0), TSegmentAtCreate(10), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void SectionStartingAtCeiling_DoesNotLowerLimit()
    {
        var sections = new[] { TSegmentPieceCreate(5, 8) };
        Assert.Equal(TSegmentAtCreate(5), TInterface.TPieceLimitRead(sections, TSegmentAtCreate(0), TSegmentAtCreate(5), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void EmptySections_FloorIsZero()
    {
        Assert.Equal(TimeSpan.Zero, TInterface.TPieceFloorRead(Array.Empty<LPiece>(), TSegmentAtCreate(5), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void PriorSectionNearestCursor_SetsFloor()
    {
        var sections = new[] { TSegmentPieceCreate(0, 2), TSegmentPieceCreate(3, 4) };
        Assert.Equal(TSegmentAtCreate(4), TInterface.TPieceFloorRead(sections, TSegmentAtCreate(6), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void OverlapAllowed_FloorIsZero()
    {
        var sections = new[] { TSegmentPieceCreate(0, 4) };
        Assert.Equal(TimeSpan.Zero, TInterface.TPieceFloorRead(sections, TSegmentAtCreate(6), -1, TSegmentOverlapOn));
    }
}
