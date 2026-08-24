using Xunit;

namespace Cadroue.Tests;

[Collection("Placement")]
public sealed class PlacementPersistenceTests
{
    [Fact]
    public void SavingMultipleWindows_PreservesEveryPlacement()
    {
        using var placement = new TPlacement();

        Assert.True(placement.Save("First", 10));
        Assert.True(placement.Save("Second", 20));

        Assert.Equal(10, placement.Read("First")!.LPlacementLeft);
        Assert.Equal(20, placement.Read("Second")!.LPlacementLeft);
    }

    [Fact]
    public void SavingAfterUnreadableCatalogue_DoesNotRewriteIt()
    {
        using var placement = new TPlacement();
        placement.Malform();

        bool saved = placement.Save("ClosingWindow", 10);

        Assert.False(saved);
        Assert.False(File.Exists(placement.PathCurrent));
        Assert.Equal("{ invalid placement json", File.ReadAllText(placement.PathCurrent + ".corrupt"));
    }

    [Fact]
    public void FailedWrite_IsReported()
    {
        using var placement = new TPlacement();
        placement.BlockWrite();

        bool saved = placement.Save("BlockedWindow", 10);

        Assert.False(saved);
        Assert.True(Directory.Exists(placement.PathCurrent));
        Assert.False(File.Exists(placement.PathCurrent + ".tmp"));
    }
}
