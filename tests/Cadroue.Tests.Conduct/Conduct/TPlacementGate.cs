using System;
using System.Collections.Generic;
using System.IO;
using Cadroue.Conduct;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TPlacementGate
{
    private readonly TPlacementPort tPlacementPort = new();
    private readonly CPlacement tPlacementArea;

    public TPlacementGate()
    {
        tPlacementArea = TAtelier.TAtelierCreate(port => port with { LAtelierPlacement = tPlacementPort })
            .CAtelierPlacement;
    }

    [Fact]
    public void PlacementSaveCheck_KnownCornerAndPositiveSize_Keeps()
    {
        Assert.True(CPlacement.CPlacementSaveCheck(-20, 0, 1, 1));
        Assert.True(CPlacement.CPlacementSaveCheck(0, 0, double.NaN, double.NaN));
    }

    [Theory]
    [InlineData(double.NaN, 0, 10, 10)]
    [InlineData(0, double.NaN, 10, 10)]
    [InlineData(0, 0, 0, 10)]
    [InlineData(0, 0, 10, -1)]
    public void PlacementSave_UnknownCornerOrEmptySize_WritesNothing(
        double left, double top, double width, double height)
    {
        bool written = false;

        Assert.False(tPlacementArea.CPlacementSave(
            "loupe", left, top, width, height, (_, _, _, _, _) => (written = true, null)));
        Assert.False(written);
        Assert.Empty(tPlacementPort.TPlacementCalls);
    }

    [Fact]
    public void PlacementSave_KeptRectangle_AnswersTheWrite()
    {
        List<string> writes = [];

        Assert.True(tPlacementArea.CPlacementSave("loupe", 1, 2, 3, 4, (key, left, top, width, height) =>
        {
            writes.Add($"{key}:{left},{top},{width},{height}");
            return (true, null);
        }));
        Assert.False(tPlacementArea.CPlacementSave("loupe", 1, 2, 3, 4, (_, _, _, _, _) => (false, null)));
        Assert.Equal(["loupe:1,2,3,4"], writes);
    }

    [Fact]
    public void PlacementCarryCheck_HeldOrNot_ImportsOnlyWhereNoneIsHeld()
    {
        Assert.True(CPlacement.CPlacementCarryCheck(false));
        Assert.False(CPlacement.CPlacementCarryCheck(true));
    }

    [Fact]
    public void PlacementImport_NoneHeld_SavesAndRecordsTheCarry()
    {
        List<string> saves = [];

        tPlacementArea.CPlacementImport(
            [("Loupe", "loupe")], _ => false, (key, left, top, width, height) =>
            {
                saves.Add($"{key}:{left},{top},{width},{height}");
                return true;
            });

        Assert.Equal(["key:1,2,3,4"], saves);
        Assert.Equal(["preference:1", "carried:key"], tPlacementPort.TPlacementCalls);
    }

    [Fact]
    public void PlacementImport_HeldOrUnsaved_RecordsNoCarry()
    {
        tPlacementArea.CPlacementImport([("Loupe", "loupe")], _ => true, (_, _, _, _, _) => true);
        tPlacementArea.CPlacementImport([("Loupe", "loupe")], _ => false, (_, _, _, _, _) => false);

        Assert.Equal(["preference:1", "preference:1"], tPlacementPort.TPlacementCalls);
    }

    [Fact]
    public void PlacementImport_Failure_EndsTheImportTraced()
    {
        tPlacementArea.CPlacementImport(
            [("Loupe", "loupe")], _ => throw new InvalidOperationException("broken"), (_, _, _, _, _) => true);

        Assert.Equal(["preference:1", "uncarried:broken"], tPlacementPort.TPlacementCalls);
    }

    [Fact]
    public void PlacementRecords_EachFailure_ReachesThePortInOrder()
    {
        tPlacementArea.CPlacementUnreadableRecord(new InvalidDataException("bad"));
        Assert.False(tPlacementArea.CPlacementSave("loupe", 1, 2, 3, 4, (_, _, _, _, _) => (false, "worse")));
        Assert.False(tPlacementArea.CPlacementSave("loupe", 1, 2, 3, 4, (_, _, _, _, _) => (false, null)));
        Assert.True(tPlacementArea.CPlacementSave("loupe", 1, 2, 3, 4, (_, _, _, _, _) => (true, null)));

        Assert.Equal(
            ["unreadable:bad", "unreadable:worse", "blocked:loupe", "unsaved:loupe"], tPlacementPort.TPlacementCalls);
    }
}
