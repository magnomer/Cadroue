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

        int removed = cleanup.TRetentionRun(30);

        Assert.Equal(2, removed);
        Assert.False(cleanup.TRetentionExist(doneOld));
        Assert.False(cleanup.TRetentionExist(audioOld));
        Assert.True(cleanup.TRetentionExist(doneNew));
        Assert.True(cleanup.TRetentionExist(paletteOld));
        Assert.True(cleanup.TRetentionExist(indexOld));
    }
}
