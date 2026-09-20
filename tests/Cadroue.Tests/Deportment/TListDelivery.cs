using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Cartographer")]
public sealed class TListDelivery
{
    private static (LStrip, LStripTab, LDocket) TListBuild(params string[] paths)
    {
        LStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => name);
        LStripTab tab = TInterface.TStripTabCreate("Convert");
        TInterface.TStripAdd(strip, tab);
        LDocket docket = TInterface.TDocketCreate();
        TInterface.TDocketPathsAdd(docket, paths);
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate("Convert"), docket);
        TInterface.TListRelayAttach(strip);
        return (strip, tab, docket);
    }

    private static LWorkItem TWorkBuild(string source, Guid batch, Guid relaySource) =>
        TInterface.TWorkRelayCreate(source, batch, relaySource);

    [Fact]
    public void DeliveredAdd_ListsExistingMediaInTheTargetTab_RefusesMissingOrUnknown()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "delivery", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string clip = Path.Combine(folder, "clip.mp4");
        File.WriteAllBytes(clip, []);
        try
        {
            (_, LStripTab tab, LDocket docket) = TListBuild();
            Guid batch = Guid.NewGuid();

            Assert.True(TInterface.TListDeliveredAdd(tab.LStripTabId, clip, batch));
            Assert.False(TInterface.TListDeliveredAdd(tab.LStripTabId, Path.Combine(folder, "gone.mp4"), batch));
            Assert.False(TInterface.TListDeliveredAdd(Guid.NewGuid(), clip, batch));

            LDocketEntry entry = Assert.Single(TInterface.TDocketItemsRead(docket));
            Assert.True(entry.LDocketEntryDelivered);
            Assert.False(entry.LDocketEntryLocked);
            Assert.Equal(batch, entry.LDocketEntryBatch);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void SourceClaim_LocksTheSourceInItsTab_ReleaseUnlocksIt()
    {
        (_, LStripTab tab, LDocket docket) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        Guid batch = Guid.NewGuid();
        LWorkItem work = TWorkBuild(@"C:\a.mp4", batch, tab.LStripTabId);

        Assert.True(TInterface.TListSourceClaim(work));
        Assert.True(TInterface.TDocketItemsRead(docket)[0].LDocketEntryLocked);
        Assert.False(TInterface.TDocketItemsRead(docket)[1].LDocketEntryLocked);

        TInterface.TListSourceRelease([(@"C:\a.mp4", batch, work)]);
        Assert.False(TInterface.TDocketItemsRead(docket)[0].LDocketEntryLocked);
    }

    [Fact]
    public void BatchRemove_DropsDeliveredFiles_UnlocksOrphans()
    {
        (_, LStripTab tab, LDocket docket) = TListBuild(@"C:\a.mp4");
        Guid batch = Guid.NewGuid();
        LWorkItem work = TWorkBuild(@"C:\a.mp4", batch, tab.LStripTabId);
        TInterface.TListSourceClaim(work);
        TInterface.TDocketDeliveredAdd(docket, @"C:\delivered.mp4", batch, false);

        TInterface.TListBatchRemove(batch);

        LDocketEntry entry = Assert.Single(TInterface.TDocketItemsRead(docket));
        Assert.Equal(@"C:\a.mp4", entry.LDocketEntryPath);
        Assert.False(entry.LDocketEntryLocked);
    }

    [Fact]
    public void DeliveredRemove_ForcedDropsTheSourceFromItsTab()
    {
        (_, LStripTab tab, LDocket docket) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        LWorkItem work = TWorkBuild(@"C:\a.mp4", Guid.NewGuid(), tab.LStripTabId);

        TInterface.TListDeliveredRemove(work, true);

        Assert.Equal([@"C:\b.mp4"], TInterface.TDocketPathsRead(docket));
        TInterface.TListDeliveredRemove(TWorkBuild(@"C:\b.mp4", Guid.NewGuid(), Guid.NewGuid()), true);
        Assert.Equal([@"C:\b.mp4"], TInterface.TDocketPathsRead(docket));
    }
}
