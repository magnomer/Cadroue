using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LPieceTests
{
    private const bool OverlapOff = false;
    private const bool OverlapOn = true;

    private static LPiece Seg(double startSeconds, double endSeconds) =>
        new(TimeSpan.FromSeconds(startSeconds), TimeSpan.FromSeconds(endSeconds), 0, string.Empty);

    private static TimeSpan At(double seconds) => TimeSpan.FromSeconds(seconds);

    // ---- ValidSelect ----

    [Fact]
    public void ValidSelect_SectionPastDuration_Dropped()
    {
        var sections = new[] { Seg(0, 5), Seg(8, 12) };
        var valid = LPiece.LPieceValidSelect(sections, At(10));
        Assert.Single(valid);
        Assert.Equal(At(5), valid[0].LPieceEnd);
    }

    [Fact]
    public void ValidSelect_ZeroOrNegativeLength_Dropped()
    {
        var sections = new[] { Seg(3, 3), Seg(6, 4), Seg(1, 2) };
        var valid = LPiece.LPieceValidSelect(sections, At(10));
        Assert.Single(valid);
        Assert.Equal(At(1), valid[0].LPieceStart);
        Assert.Equal(At(2), valid[0].LPieceEnd);
    }

    [Fact]
    public void ValidSelect_AllValid_RoundTripsPreservingOrder()
    {
        var sections = new[] { Seg(0, 3), Seg(4, 6), Seg(7, 10) };
        var valid = LPiece.LPieceValidSelect(sections, At(10));
        Assert.Equal(3, valid.Count);
        Assert.Equal(At(0), valid[0].LPieceStart);
        Assert.Equal(At(4), valid[1].LPieceStart);
        Assert.Equal(At(7), valid[2].LPieceStart);
    }

    [Fact]
    public void ValidSelect_EndEqualToDuration_Kept()
    {
        var sections = new[] { Seg(5, 10) };
        var valid = LPiece.LPieceValidSelect(sections, At(10));
        Assert.Single(valid);
    }

    // ---- InsideCheck ----

    [Fact]
    public void InsideCheck_OverlapAllowed_AlwaysFalse()
    {
        var sections = new[] { Seg(0, 10) };
        Assert.False(LPiece.LPieceInsideCheck(sections, At(5), -1, OverlapOn));
    }

    [Fact]
    public void InsideCheck_TimeStrictlyInside_True()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.True(LPiece.LPieceInsideCheck(sections, At(5), -1, OverlapOff));
    }

    [Fact]
    public void InsideCheck_TimeAtEndBoundary_False()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.False(LPiece.LPieceInsideCheck(sections, At(8), -1, OverlapOff));
    }

    [Fact]
    public void InsideCheck_TimeAtStartBoundary_True()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.True(LPiece.LPieceInsideCheck(sections, At(2), -1, OverlapOff));
    }

    [Fact]
    public void InsideCheck_SkipIndex_Ignored()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.False(LPiece.LPieceInsideCheck(sections, At(5), 0, OverlapOff));
    }

    // ---- LimitRead ----

    [Fact]
    public void LimitRead_OverlapAllowed_ReturnsCeiling()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(10), LPiece.LPieceLimitRead(sections, At(0), At(10), -1, OverlapOn));
    }

    [Fact]
    public void LimitRead_NextStartCaps()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(5), LPiece.LPieceLimitRead(sections, At(0), At(10), -1, OverlapOff));
    }

    [Fact]
    public void LimitRead_StartEqualToCeiling_NotCapped()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(5), LPiece.LPieceLimitRead(sections, At(0), At(5), -1, OverlapOff));
    }

    // ---- FloorRead ----

    [Fact]
    public void FloorRead_EmptyList_Zero()
    {
        Assert.Equal(TimeSpan.Zero, LPiece.LPieceFloorRead(Array.Empty<LPiece>(), At(5), -1, OverlapOff));
    }

    [Fact]
    public void FloorRead_NearestPriorEnd()
    {
        var sections = new[] { Seg(0, 2), Seg(3, 4) };
        Assert.Equal(At(4), LPiece.LPieceFloorRead(sections, At(6), -1, OverlapOff));
    }

    [Fact]
    public void FloorRead_OverlapAllowed_Zero()
    {
        var sections = new[] { Seg(0, 4) };
        Assert.Equal(TimeSpan.Zero, LPiece.LPieceFloorRead(sections, At(6), -1, OverlapOn));
    }

    // ---- Add ----

    [Fact]
    public void Add_EmptyList_CreatesCursorToDuration()
    {
        var plan = LPiece.LPieceAdd(Array.Empty<LPiece>(), At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Single(plan!.Value.Sections);
        Assert.Equal(At(3), plan.Value.Sections[0].LPieceStart);
        Assert.Equal(At(10), plan.Value.Sections[0].LPieceEnd);
        Assert.Equal(0, plan.Value.Active);
    }

    [Fact]
    public void Add_CursorAtDuration_Null()
    {
        Assert.Null(LPiece.LPieceAdd(Array.Empty<LPiece>(), At(10), At(10), 0, OverlapOff));
    }

    [Fact]
    public void Add_CursorInsideExisting_Null()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.Null(LPiece.LPieceAdd(sections, At(5), At(10), 0, OverlapOff));
    }

    [Fact]
    public void Add_CapsEndAtNextStart()
    {
        var sections = new[] { Seg(6, 9) };
        var plan = LPiece.LPieceAdd(sections, At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(6), plan!.Value.Sections[^1].LPieceEnd);
    }

    // ---- StartSet ----

    [Fact]
    public void StartSet_NoActive_AddsForward()
    {
        var plan = LPiece.LPieceStartSet(Array.Empty<LPiece>(), null, At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(3), plan!.Value.Sections[0].LPieceStart);
        Assert.Equal(At(10), plan.Value.Sections[0].LPieceEnd);
    }

    [Fact]
    public void StartSet_CursorPastActiveEnd_AddsForward()
    {
        var sections = new[] { Seg(1, 3) };
        var plan = LPiece.LPieceStartSet(sections, 0, At(5), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Value.Sections.Count);
        Assert.Equal(At(5), plan.Value.Sections[^1].LPieceStart);
        Assert.True(plan.Value.Added);
    }

    [Fact]
    public void StartSet_ShrinksStart()
    {
        var sections = new[] { Seg(2, 8) };
        var plan = LPiece.LPieceStartSet(sections, 0, At(4), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(4), plan!.Value.Sections[0].LPieceStart);
        Assert.Equal(At(8), plan.Value.Sections[0].LPieceEnd);
        Assert.False(plan.Value.Added);
    }

    [Fact]
    public void StartSet_CursorAtEnd_Null()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.Null(LPiece.LPieceStartSet(sections, 0, At(8), At(10), 0, OverlapOff));
    }

    [Fact]
    public void StartSet_BelowFloor_Null()
    {
        var sections = new[] { Seg(0, 3), Seg(5, 9) };
        Assert.Null(LPiece.LPieceStartSet(sections, 1, At(2), At(10), 0, OverlapOff));
    }

    // ---- EndSet ----

    [Fact]
    public void EndSet_NoActive_EmptyList_CreatesZeroToCursor()
    {
        var plan = LPiece.LPieceEndSet(Array.Empty<LPiece>(), null, At(4), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(TimeSpan.Zero, plan!.Value.Sections[0].LPieceStart);
        Assert.Equal(At(4), plan.Value.Sections[0].LPieceEnd);
        Assert.True(plan.Value.Added);
    }

    [Fact]
    public void EndSet_ShrinksEnd()
    {
        var sections = new[] { Seg(2, 8) };
        var plan = LPiece.LPieceEndSet(sections, 0, At(6), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(6), plan!.Value.Sections[0].LPieceEnd);
        Assert.False(plan.Value.Added);
    }

    [Fact]
    public void EndSet_CursorBeforeStart_Null()
    {
        var sections = new[] { Seg(4, 8) };
        Assert.Null(LPiece.LPieceEndSet(sections, 0, At(2), 0, OverlapOff));
    }

    [Fact]
    public void EndSet_CursorBeyondNextStart_Null()
    {
        var sections = new[] { Seg(2, 4), Seg(7, 9) };
        Assert.Null(LPiece.LPieceEndSet(sections, 0, At(8), 0, OverlapOff));
    }

    // Regression: a new end-anchored section may end exactly where the next one starts.
    [Fact]
    public void EndSet_NoActive_AdjacentToNextStart_Allowed()
    {
        var sections = new[] { Seg(5, 8) };
        var plan = LPiece.LPieceEndSet(sections, null, At(5), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Contains(plan!.Value.Sections, s => s.LPieceStart == TimeSpan.Zero && s.LPieceEnd == At(5));
    }

    // ---- EndCreate ----

    [Fact]
    public void EndCreate_CursorZero_Null()
    {
        Assert.Null(LPiece.LPieceEndCreate(Array.Empty<LPiece>(), TimeSpan.Zero, 0, OverlapOff));
    }

    [Fact]
    public void EndCreate_StartsAtFloor()
    {
        var sections = new[] { Seg(0, 2) };
        var plan = LPiece.LPieceEndCreate(sections, At(5), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Contains(plan!.Value.Sections, s => s.LPieceStart == At(2) && s.LPieceEnd == At(5));
    }

    [Fact]
    public void EndCreate_OverlappingSpanBlocked()
    {
        var sections = new[] { Seg(0, 2), Seg(5, 8) };
        Assert.Null(LPiece.LPieceEndCreate(sections, At(6), 0, OverlapOff));
    }

    // ---- Divide ----

    [Fact]
    public void Divide_NoActive_Null()
    {
        var sections = new[] { Seg(0, 8) };
        Assert.Null(LPiece.LPieceDivide(sections, null, At(4), 0));
    }

    [Fact]
    public void Divide_Midpoint_TwoHalves()
    {
        var sections = new[] { Seg(0, 8) };
        var plan = LPiece.LPieceDivide(sections, 0, At(3), 1);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Value.Sections.Count);
        Assert.Equal(At(0), plan.Value.Sections[0].LPieceStart);
        Assert.Equal(At(3), plan.Value.Sections[0].LPieceEnd);
        Assert.Equal(At(3), plan.Value.Sections[1].LPieceStart);
        Assert.Equal(At(8), plan.Value.Sections[1].LPieceEnd);
        Assert.Equal(0, plan.Value.First);
        Assert.Equal(1, plan.Value.Second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Divide_CursorAtBounds_Null(double cursorSeconds)
    {
        var sections = new[] { Seg(0, 8) };
        Assert.Null(LPiece.LPieceDivide(sections, 0, At(cursorSeconds), 0));
    }
}
