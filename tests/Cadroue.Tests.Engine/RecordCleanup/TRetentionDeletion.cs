using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

[Collection("Schedule")]
public sealed class TRetentionDeletion
{
    [Fact]
    public void RunRemovesOnlyExpiredUnprotectedFiles()
    {
        using var cleanup = new TRetention();

        string doneOld = cleanup.TRetentionRecordCreate("done", "old.json", 60);
        string doneNew = cleanup.TRetentionRecordCreate("done", "new.json", 0);
        string audioOld = cleanup.TRetentionRecordCreate("audiowork", "clip.wav", 60);
        string paletteOld = cleanup.TRetentionRecordCreate("palettes", "set.json", 60);
        string indexOld = cleanup.TRetentionRecordCreate(null, "work.db", 60);
        string placementOld = cleanup.TRetentionRecordCreate(null, "placement.json", 60);
        string logOld = cleanup.TRetentionRecordCreate("log", "Cadroue-old.log", 60);
        string recordOld = cleanup.TRetentionRecordCreate("filerecord", "clip.cad", 60);
        string mpvOld = cleanup.TRetentionRecordCreate("local-mpv", "libmpv-2.dll", 60);
        string flyleafOld = cleanup.TRetentionRecordCreate("local-flyleaf", "FlyleafLib.dll", 60);

        int removed = cleanup.TRetentionRun(30);

        Assert.Equal(2, removed);
        Assert.False(cleanup.TRetentionExist(doneOld));
        Assert.False(cleanup.TRetentionExist(audioOld));
        Assert.True(cleanup.TRetentionExist(doneNew));
        Assert.True(cleanup.TRetentionExist(paletteOld));
        Assert.True(cleanup.TRetentionExist(indexOld));
        Assert.True(cleanup.TRetentionExist(placementOld));
        Assert.True(cleanup.TRetentionExist(logOld));
        Assert.True(cleanup.TRetentionExist(recordOld));
        Assert.True(cleanup.TRetentionExist(mpvOld));
        Assert.True(cleanup.TRetentionExist(flyleafOld));
    }

    [Fact]
    public void Run_FileGoneBeforeDelete_CountsNothing()
    {
        using var cleanup = new TRetention();
        string doneOld = cleanup.TRetentionRecordCreate("done", "old.json", 60);
        File.Delete(doneOld);

        Assert.Equal(0, cleanup.TRetentionRun(30));
    }

    [Fact]
    public void Run_FileHeldByOther_CountsNothing()
    {
        using var cleanup = new TRetention();
        string doneOld = cleanup.TRetentionRecordCreate("done", "old.json", 60);

        using (new FileStream(doneOld, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            Assert.Equal(0, cleanup.TRetentionRun(30));
        }

        Assert.True(cleanup.TRetentionExist(doneOld));
    }

    [Fact]
    public void Run_FolderGone_ReadsNoFiles()
    {
        using var cleanup = new TRetention();
        cleanup.TRetentionRecordCreate("done", "old.json", 60);
        string done = cleanup.TRetentionFolderResolve("done");
        Directory.Delete(done, true);

        Assert.Equal(0, cleanup.TRetentionRun(30));
    }
}
