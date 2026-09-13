using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TSectionData;

namespace Cadroue.Tests;

public sealed class TSegmentCreation
{
    private const bool TSegmentOverlapOff = false;

    [Fact]
    public void EmptySections_AddCreatesCursorToDuration()
    {
        var plan = TInterface.TPieceAdd(Array.Empty<LPiece>(), TSegmentAtCreate(3), TSegmentAtCreate(10), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Single(plan!.LPieceSections);
        Assert.Equal(TSegmentAtCreate(3), plan!.LPieceSections[0].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(10), plan!.LPieceSections[0].LPieceEnd);
        Assert.Equal(0, plan!.LPieceActive);
    }

    [Fact]
    public void CursorAtDuration_AddIsRejected()
    {
        Assert.Null(TInterface.TPieceAdd(Array.Empty<LPiece>(), TSegmentAtCreate(10), TSegmentAtCreate(10), 0, TSegmentOverlapOff));
    }

    [Fact]
    public void CursorInsideSection_AddIsRejected()
    {
        var sections = new[] { TSegmentPieceCreate(2, 8) };
        Assert.Null(TInterface.TPieceAdd(sections, TSegmentAtCreate(5), TSegmentAtCreate(10), 0, TSegmentOverlapOff));
    }

    [Fact]
    public void NextSectionStart_CapsAddedSectionEnd()
    {
        var sections = new[] { TSegmentPieceCreate(6, 9) };
        var plan = TInterface.TPieceAdd(sections, TSegmentAtCreate(3), TSegmentAtCreate(10), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(TSegmentAtCreate(6), plan!.LPieceSections[^1].LPieceEnd);
    }

    [Fact]
    public void NoActiveSection_SettingStartAddsForward()
    {
        var plan = TInterface.TPieceStartSet(Array.Empty<LPiece>(), null, TSegmentAtCreate(3), TSegmentAtCreate(10), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(TSegmentAtCreate(3), plan!.LPieceSections[0].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(10), plan!.LPieceSections[0].LPieceEnd);
    }

    [Fact]
    public void CursorPastActiveEnd_SettingStartAddsForward()
    {
        var sections = new[] { TSegmentPieceCreate(1, 3) };
        var plan = TInterface.TPieceStartSet(sections, 0, TSegmentAtCreate(5), TSegmentAtCreate(10), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.LPieceSections.Count);
        Assert.Equal(TSegmentAtCreate(5), plan!.LPieceSections[^1].LPieceOrigin);
        Assert.True(plan!.LPieceAdded);
    }

    [Fact]
    public void NoActiveSection_SettingEndCreatesZeroToCursor()
    {
        var plan = TInterface.TPieceEndSet(Array.Empty<LPiece>(), null, TSegmentAtCreate(4), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(TimeSpan.Zero, plan!.LPieceSections[0].LPieceOrigin);
        Assert.Equal(TSegmentAtCreate(4), plan!.LPieceSections[0].LPieceEnd);
        Assert.True(plan!.LPieceAdded);
    }

    [Fact]
    public void PriorSectionEnd_AnchorsCreatedSectionStart()
    {
        var sections = new[] { TSegmentPieceCreate(0, 2) };
        var plan = TInterface.TPieceEndCreate(sections, TSegmentAtCreate(5), 0, TSegmentOverlapOff);
        Assert.NotNull(plan);
        Assert.Contains(plan!.LPieceSections, section => section.LPieceOrigin == TSegmentAtCreate(2) && section.LPieceEnd == TSegmentAtCreate(5));
    }
}
