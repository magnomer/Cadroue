using Cadroue.Application;
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
    public void SectionBeforeMediaStart_IsDropped()
    {
        var sections = new[] { TSegmentPieceCreate(-1, 2), TSegmentPieceCreate(2, 4) };
        var valid = TInterface.TPieceValidSelect(sections, TSegmentAtCreate(10));
        Assert.Single(valid);
        Assert.Equal(TSegmentAtCreate(2), valid[0].LPieceOrigin);
    }

    [Fact]
    public void NegativeColorIndex_IsNormalizedToZero()
    {
        var sections = new[] { TSegmentPieceCreate(0, 2) with { LPieceColorIndex = int.MinValue } };
        var valid = TInterface.TPieceValidSelect(sections, TSegmentAtCreate(10));
        Assert.Equal(0, Assert.Single(valid).LPieceColorIndex);
    }

    [Fact]
    public void BoundSet_RejectsOutOfRangeAndReportsResult()
    {
        var segment = TInterface.TSegmentCreate();
        Assert.True(TInterface.TSegmentBoundSet(
            segment, new[] { TSegmentPieceCreate(-2, 1), TSegmentPieceCreate(1, 3), TSegmentPieceCreate(5, 20) }, 0, TSegmentAtCreate(10)));
        var kept = Assert.Single(TInterface.TSegmentListRead(segment));
        Assert.Equal(TSegmentAtCreate(1), kept.LPieceOrigin);
    }

    [Fact]
    public void Load_DropsInvalidSavedSections_AndRaisesInvalidFault()
    {
        LSidecarSectionRecord TSidecarSectionCreate(long start, long end, int color) => new()
        {
            LSidecarStartMilliseconds = start, LSidecarEndMilliseconds = end, LSidecarColorIndex = color
        };
        TInterface.TSegmentSeamSet(_ => new[] { TSidecarSectionCreate(-500, 1000, 0), TSidecarSectionCreate(1000, 2000, -7), TSidecarSectionCreate(3000, 99000, 1) });
        try
        {
            var segment = TInterface.TSegmentCreate();
            int dropped = 0;
            TInterface.TSegmentFaultAttach(segment, (kind, count) =>
            {
                if (kind == LSegmentFault.LSegmentFaultInvalid) dropped = count;
            });
            TInterface.TSegmentLoad(segment, "clip.mp4", TSegmentAtCreate(10));
            var kept = Assert.Single(TInterface.TSegmentListRead(segment));
            Assert.Equal(TSegmentAtCreate(1), kept.LPieceOrigin);
            Assert.Equal(0, kept.LPieceColorIndex);
            Assert.Equal(2, dropped);
        }
        finally
        {
            TInterface.TSegmentSeamSet(null);
        }
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
        Assert.Equal(
            TSegmentAtCreate(10),
            TInterface.TPieceLimitRead(sections, TSegmentAtCreate(0), TSegmentAtCreate(10), -1, TSegmentOverlapOn));
    }

    [Fact]
    public void NextSectionStart_CapsLimit()
    {
        var sections = new[] { TSegmentPieceCreate(5, 8) };
        Assert.Equal(
            TSegmentAtCreate(5),
            TInterface.TPieceLimitRead(sections, TSegmentAtCreate(0), TSegmentAtCreate(10), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void SectionStartingAtCeiling_DoesNotLowerLimit()
    {
        var sections = new[] { TSegmentPieceCreate(5, 8) };
        Assert.Equal(
            TSegmentAtCreate(5),
            TInterface.TPieceLimitRead(sections, TSegmentAtCreate(0), TSegmentAtCreate(5), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void EmptySections_FloorIsZero()
    {
        Assert.Equal(
            TimeSpan.Zero,
            TInterface.TPieceFloorRead(Array.Empty<LPiece>(), TSegmentAtCreate(5), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void PriorSectionNearestCursor_SetsFloor()
    {
        var sections = new[] { TSegmentPieceCreate(0, 2), TSegmentPieceCreate(3, 4) };
        Assert.Equal(
            TSegmentAtCreate(4),
            TInterface.TPieceFloorRead(sections, TSegmentAtCreate(6), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void OverlapAllowed_FloorIsZero()
    {
        var sections = new[] { TSegmentPieceCreate(0, 4) };
        Assert.Equal(TimeSpan.Zero, TInterface.TPieceFloorRead(sections, TSegmentAtCreate(6), -1, TSegmentOverlapOn));
    }

    [Fact]
    public void TouchingSections_DoNotIntersect()
    {
        var sections = new[] { TSegmentPieceCreate(0, 4) };
        Assert.False(TInterface.TPieceIntersectCheck(
            sections, TSegmentAtCreate(4), TSegmentAtCreate(8), -1, TSegmentOverlapOff));
        Assert.True(TInterface.TPieceIntersectCheck(
            sections, TSegmentAtCreate(3), TSegmentAtCreate(8), -1, TSegmentOverlapOff));
    }

    [Fact]
    public void ListAboveCeiling_IsRefusedWithFault()
    {
        var segment = TInterface.TSegmentCreate();
        LSegmentFault? fault = null;
        TInterface.TSegmentFaultAttach(segment, (kind, _) => fault = kind);
        var sections = Enumerable.Range(0, LPiece.LPieceCeiling + 1)
            .Select(index => TSegmentPieceCreate(index, index + 1))
            .ToArray();
        TInterface.TSegmentSet(segment, sections, 0);
        Assert.Equal(LSegmentFault.LSegmentFaultCeiling, fault);
        Assert.Empty(TInterface.TSegmentListRead(segment));
    }

    [Fact]
    public void StaleApproval_DoesNotDeleteOrClear()
    {
        var segment = TInterface.TSegmentCreate();
        TInterface.TSegmentSet(segment, new[] { TSegmentPieceCreate(0, 2) }, 0);
        int approved = TInterface.TSegmentVersionRead(segment);
        TInterface.TSegmentSet(segment, new[] { TSegmentPieceCreate(0, 2), TSegmentPieceCreate(3, 4) }, 0);
        Assert.False(TInterface.TSegmentDelete(segment, new[] { 0 }, approved));
        Assert.False(TInterface.TSegmentClear(segment, approved));
        Assert.Equal(2, TInterface.TSegmentListRead(segment).Count);
        Assert.True(TInterface.TSegmentDelete(segment, new[] { 1 }, TInterface.TSegmentVersionRead(segment)));
        Assert.Single(TInterface.TSegmentListRead(segment));
    }
}
