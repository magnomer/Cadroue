using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.SectionData;

namespace Cadroue.Tests;

public sealed class SectionCreationTests
{
    private const bool OverlapOff = false;

    [Fact]
    public void EmptySections_AddCreatesCursorToDuration()
    {
        var plan = TInterface.PieceAdd(Array.Empty<LPiece>(), At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Single(plan!.Value.Sections);
        Assert.Equal(At(3), plan.Value.Sections[0].LPieceOrigin);
        Assert.Equal(At(10), plan.Value.Sections[0].LPieceEnd);
        Assert.Equal(0, plan.Value.Active);
    }

    [Fact]
    public void CursorAtDuration_AddIsRejected()
    {
        Assert.Null(TInterface.PieceAdd(Array.Empty<LPiece>(), At(10), At(10), 0, OverlapOff));
    }

    [Fact]
    public void CursorInsideSection_AddIsRejected()
    {
        var sections = new[] { Seg(2, 8) };
        Assert.Null(TInterface.PieceAdd(sections, At(5), At(10), 0, OverlapOff));
    }

    [Fact]
    public void NextSectionStart_CapsAddedSectionEnd()
    {
        var sections = new[] { Seg(6, 9) };
        var plan = TInterface.PieceAdd(sections, At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(6), plan!.Value.Sections[^1].LPieceEnd);
    }

    [Fact]
    public void NoActiveSection_SettingStartAddsForward()
    {
        var plan = TInterface.PieceStartSet(Array.Empty<LPiece>(), null, At(3), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(At(3), plan!.Value.Sections[0].LPieceOrigin);
        Assert.Equal(At(10), plan.Value.Sections[0].LPieceEnd);
    }

    [Fact]
    public void CursorPastActiveEnd_SettingStartAddsForward()
    {
        var sections = new[] { Seg(1, 3) };
        var plan = TInterface.PieceStartSet(sections, 0, At(5), At(10), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Value.Sections.Count);
        Assert.Equal(At(5), plan.Value.Sections[^1].LPieceOrigin);
        Assert.True(plan.Value.Added);
    }

    [Fact]
    public void NoActiveSection_SettingEndCreatesZeroToCursor()
    {
        var plan = TInterface.PieceEndSet(Array.Empty<LPiece>(), null, At(4), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Equal(TimeSpan.Zero, plan!.Value.Sections[0].LPieceOrigin);
        Assert.Equal(At(4), plan.Value.Sections[0].LPieceEnd);
        Assert.True(plan.Value.Added);
    }

    [Fact]
    public void PriorSectionEnd_AnchorsCreatedSectionStart()
    {
        var sections = new[] { Seg(0, 2) };
        var plan = TInterface.PieceEndCreate(sections, At(5), 0, OverlapOff);
        Assert.NotNull(plan);
        Assert.Contains(plan!.Value.Sections, section => section.LPieceOrigin == At(2) && section.LPieceEnd == At(5));
    }
}
