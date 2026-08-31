using Xunit;

namespace Cadroue.Tests;

[Collection("Placement")]
public sealed class TPlacementPersistence
{
    [Fact]
    public void SavingMultipleWindows_PreservesEveryPlacement()
    {
        using var placement = new TPlacement();

        Assert.True(placement.TPlacementSave("First", 10));
        Assert.True(placement.TPlacementSave("Second", 20));

        Assert.Equal(10, placement.TPlacementRead("First")!.LPlacementLeft);
        Assert.Equal(20, placement.TPlacementRead("Second")!.LPlacementLeft);
    }

    [Fact]
    public void SavingAfterUnreadableCatalogue_DoesNotRewriteIt()
    {
        using var placement = new TPlacement();
        placement.TPlacementMalformSave();

        bool saved = placement.TPlacementSave("ClosingWindow", 10);

        Assert.False(saved);
        Assert.False(File.Exists(placement.TPlacementPath));
        Assert.Equal("{ invalid placement json", File.ReadAllText(placement.TPlacementPath + ".corrupt"));
    }

    [Fact]
    public void SavingAfterQuarantine_RecoversOnNextAttempt()
    {
        using var placement = new TPlacement();
        placement.TPlacementMalformSave();

        Assert.False(placement.TPlacementSave("FirstClose", 10));
        Assert.True(placement.TPlacementSave("SecondClose", 20));

        Assert.Equal(20, placement.TPlacementRead("SecondClose")!.LPlacementLeft);
        Assert.Equal("{ invalid placement json", File.ReadAllText(placement.TPlacementPath + ".corrupt"));
    }

    [Fact]
    public void FailedWrite_IsReported()
    {
        using var placement = new TPlacement();
        placement.TPlacementBlockCreate();

        bool saved = placement.TPlacementSave("BlockedWindow", 10);

        Assert.False(saved);
        Assert.True(Directory.Exists(placement.TPlacementPath));
        Assert.False(File.Exists(placement.TPlacementPath + ".tmp"));
    }
}
