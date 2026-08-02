using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LSegmentTests
{
    private const bool OverlapOff = false;
    private const bool OverlapOn = true;

    private static LSegment Seg(double startSeconds, double endSeconds) =>
        new(TimeSpan.FromSeconds(startSeconds), TimeSpan.FromSeconds(endSeconds), 0, string.Empty);

    private static TimeSpan At(double seconds) => TimeSpan.FromSeconds(seconds);

    // ---- InsideCheck ----

    [Fact]
    public void InsideCheck_OverlapAllowed_AlwaysFalse()
    {
        var sections = new[] { Seg(0, 10) };
        Assert.False(LSegment.LSegmentInsideCheck(sections, At(5), -1, OverlapOn));
    }

    [Fact]
    public void InsideCheck_TimeStrictlyInside_True()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.True(LSegment.LSegmentInsideCheck(sections, At(5), -1, OverlapOff));
    }

    [Fact]
    public void InsideCheck_TimeAtEndBoundary_False()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.False(LSegment.LSegmentInsideCheck(sections, At(8), -1, OverlapOff));
    }

    [Fact]
    public void InsideCheck_TimeAtStartBoundary_True()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.True(LSegment.LSegmentInsideCheck(sections, At(2), -1, OverlapOff));
    }

    [Fact]
    public void InsideCheck_SkipIndex_Ignored()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.False(LSegment.LSegmentInsideCheck(sections, At(5), 0, OverlapOff));
    }

    // ---- LimitRead ----

    [Fact]
    public void LimitRead_OverlapAllowed_ReturnsCeiling()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(10), LSegment.LSegmentLimitRead(sections, At(0), At(10), -1, OverlapOn));
    }

    [Fact]
    public void LimitRead_NextStartCaps()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(5), LSegment.LSegmentLimitRead(sections, At(0), At(10), -1, OverlapOff));
    }

    [Fact]
    public void LimitRead_StartEqualToCeiling_NotCapped()
    {
        var sections = new[] { Seg(5, 8) };
        Assert.Equal(At(5), LSegment.LSegmentLimitRead(sections, At(0), At(5), -1, OverlapOff));
    }

    // ---- FloorRead ----

    [Fact]
    public void FloorRead_EmptyList_Zero()
    {
        Assert.Equal(TimeSpan.Zero, LSegment.LSegmentFloorRead(Array.Empty<LSegment>(), At(5), -1, OverlapOff));
    }

    [Fact]
    public void FloorRead_NearestPriorEnd()
    {
        var sections = new[] { Seg(0, 2), Seg(3, 4) };
        Assert.Equal(At(4), LSegment.LSegmentFloorRead(sections, At(6), -1, OverlapOff));
    }

    [Fact]
    public void FloorRead_OverlapAllowed_Zero()
    {
        var sections = new[] { Seg(0, 4) };
        Assert.Equal(TimeSpan.Zero, LSegment.LSegmentFloorRead(sections, At(6), -1, OverlapOn));
    }

    // ---- Add ----

    [Fact]
    public void Add_EmptyList_CreatesCursorToDuration()
    {
        var plan = LSegment.LSegmentAdd(Array.Empty<LSegment>(), At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Single(plan!.Value.Sections);
        Assert.Equal(At(3), plan.Value.Sections[0].LSegmentStart);
        Assert.Equal(At(10), plan.Value.Sections[0].LSegmentEnd);
        Assert.Equal(0, plan.Value.Active);
    }

    [Fact]
    public void Add_CursorAtDuration_Null()
    {
        Assert.Null(LSegment.LSegmentAdd(Array.Empty<LSegment>(), At(10), At(10), 0, OverlapOff));
    }

    [Fact]
    public void Add_CursorInsideExisting_Null()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.Null(LSegment.LSegmentAdd(sections, At(5), At(10), 0, OverlapOff));
    }

    [Fact]
    public void Add_CapsEndAtNextStart()
    {
        var sections = new[] { Seg(6, 9) };
        var plan = LSegment.LSegmentAdd(sections, At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(6), plan!.Value.Sections[^1].LSegmentEnd);
    }

    // ---- StartSet ----

    [Fact]
    public void StartSet_NoActive_AddsForward()
    {
        var plan = LSegment.LSegmentStartSet(Array.Empty<LSegment>(), null, At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(3), plan!.Value.Sections[0].LSegmentStart);
        Assert.Equal(At(10), plan.Value.Sections[0].LSegmentEnd);
    }

    [Fact]
    public void StartSet_CursorPastActiveEnd_AddsForward()
    {
        var sections = new[] { Seg(1, 3) };
        var plan = LSegment.LSegmentStartSet(sections, 0, At(5), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Value.Sections.Count);
        Assert.Equal(At(5), plan.Value.Sections[^1].LSegmentStart);
    }

    [Fact]
    public void StartSet_ShrinksStart()
    {
        var sections = new[] { Seg(2, 8) };
        var plan = LSegment.LSegmentStartSet(sections, 0, At(4), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(4), plan!.Value.Sections[0].LSegmentStart);
        Assert.Equal(At(8), plan.Value.Sections[0].LSegmentEnd);
    }

    [Fact]
    public void StartSet_CursorAtEnd_Null()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.Null(LSegment.LSegmentStartSet(sections, 0, At(8), At(10), 0, OverlapOff));
    }

    [Fact]
    public void StartSet_BelowFloor_Null()
    {
        var sections = new[] { Seg(0, 3), Seg(5, 9) };
        Assert.Null(LSegment.LSegmentStartSet(sections, 1, At(2), At(10), 0, OverlapOff));
    }

    // ---- EndSet ----

    [Fact]
    public void EndSet_NoActive_EmptyList_CreatesZeroToCursor()
    {
        var plan = LSegment.LSegmentEndSet(Array.Empty<LSegment>(), null, At(4), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(TimeSpan.Zero, plan!.Value.Sections[0].LSegmentStart);
        Assert.Equal(At(4), plan.Value.Sections[0].LSegmentEnd);
    }

    [Fact]
    public void EndSet_ShrinksEnd()
    {
        var sections = new[] { Seg(2, 8) };
        var plan = LSegment.LSegmentEndSet(sections, 0, At(6), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(6), plan!.Value.Sections[0].LSegmentEnd);
    }

    [Fact]
    public void EndSet_CursorBeforeStart_Null()
    {
        var sections = new[] { Seg(4, 8) };
        Assert.Null(LSegment.LSegmentEndSet(sections, 0, At(2), 0, OverlapOff));
    }

    [Fact]
    public void EndSet_CursorBeyondNextStart_Null()
    {
        var sections = new[] { Seg(2, 4), Seg(7, 9) };
        Assert.Null(LSegment.LSegmentEndSet(sections, 0, At(8), 0, OverlapOff));
    }

    // Regression: a new end-anchored section may end exactly where the next one starts.
    [Fact]
    public void EndSet_NoActive_AdjacentToNextStart_Allowed()
    {
        var sections = new[] { Seg(5, 8) };
        var plan = LSegment.LSegmentEndSet(sections, null, At(5), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Contains(plan!.Value.Sections, s => s.LSegmentStart == TimeSpan.Zero && s.LSegmentEnd == At(5));
    }

    // ---- EndCreate ----

    [Fact]
    public void EndCreate_CursorZero_Null()
    {
        Assert.Null(LSegment.LSegmentEndCreate(Array.Empty<LSegment>(), TimeSpan.Zero, 0, OverlapOff));
    }

    [Fact]
    public void EndCreate_StartsAtFloor()
    {
        var sections = new[] { Seg(0, 2) };
        var plan = LSegment.LSegmentEndCreate(sections, At(5), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Contains(plan!.Value.Sections, s => s.LSegmentStart == At(2) && s.LSegmentEnd == At(5));
    }

    [Fact]
    public void EndCreate_OverlappingSpanBlocked()
    {
        var sections = new[] { Seg(0, 2), Seg(5, 8) };
        Assert.Null(LSegment.LSegmentEndCreate(sections, At(6), 0, OverlapOff));
    }

    // ---- Divide ----

    [Fact]
    public void Divide_NoActive_Null()
    {
        var sections = new[] { Seg(0, 8) };
        Assert.Null(LSegment.LSegmentDivide(sections, null, At(4), 0));
    }

    [Fact]
    public void Divide_Midpoint_TwoHalves()
    {
        var sections = new[] { Seg(0, 8) };
        var plan = LSegment.LSegmentDivide(sections, 0, At(3), 1);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Value.Sections.Count);
        Assert.Equal(At(0), plan.Value.Sections[0].LSegmentStart);
        Assert.Equal(At(3), plan.Value.Sections[0].LSegmentEnd);
        Assert.Equal(At(3), plan.Value.Sections[1].LSegmentStart);
        Assert.Equal(At(8), plan.Value.Sections[1].LSegmentEnd);
        Assert.Equal(0, plan.Value.First);
        Assert.Equal(1, plan.Value.Second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Divide_CursorAtBounds_Null(double cursorSeconds)
    {
        var sections = new[] { Seg(0, 8) };
        Assert.Null(LSegment.LSegmentDivide(sections, 0, At(cursorSeconds), 0));
    }
}
