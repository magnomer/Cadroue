using Xunit;

namespace Cadroue.Tests;

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
}
