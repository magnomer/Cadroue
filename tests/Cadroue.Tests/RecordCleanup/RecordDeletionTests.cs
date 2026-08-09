using Xunit;

namespace Cadroue.Tests;

public sealed class RecordDeletionTests
{
    [Fact]
    public void RunRemovesOnlyExpiredUnprotectedFiles()
    {
        using var cleanup = new TRecordCleanup();

        string doneOld = cleanup.SeedRecord("done", "old.json", 60);
        string doneNew = cleanup.SeedRecord("done", "new.json", 0);
        string audioOld = cleanup.SeedRecord("audiowork", "clip.wav", 60);
        string paletteOld = cleanup.SeedRecord("palettes", "set.json", 60);
        string indexOld = cleanup.SeedRecord(null, "work.db", 60);

        int removed = cleanup.RunCleanup(30);

        Assert.Equal(2, removed);
        Assert.False(cleanup.RecordExists(doneOld));
        Assert.False(cleanup.RecordExists(audioOld));
        Assert.True(cleanup.RecordExists(doneNew));
        Assert.True(cleanup.RecordExists(paletteOld));
        Assert.True(cleanup.RecordExists(indexOld));
    }
}
