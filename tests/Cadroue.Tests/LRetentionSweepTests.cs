using Cadroue.Infrastructure;
using Xunit;

namespace Cadroue.Tests;

public sealed class LRetentionSweepTests
{
    [Fact]
    public void LRetentionRunRemovesOnlyExpiredUnprotectedFiles()
    {
        string lRoot = Path.Combine(Path.GetTempPath(), "cadroue-retention-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(lRoot);

        try
        {
            LDepot.LDepotRootSet(lRoot);

            DateTime lOld = DateTime.UtcNow.AddDays(-60);
            DateTime lNew = DateTime.UtcNow;

            string lDoneOld = Seed(lRoot, "done", "old.json", lOld);
            string lDoneNew = Seed(lRoot, "done", "new.json", lNew);
            string lAudioOld = Seed(lRoot, "audiowork", "clip.wav", lOld);
            string lPaletteOld = Seed(lRoot, "palettes", "set.json", lOld);
            string lIndexOld = Seed(lRoot, null, "work.db", lOld);

            int lRemoved = LRetentionSweep.LRetentionRun(30);

            Assert.Equal(2, lRemoved);
            Assert.False(File.Exists(lDoneOld));
            Assert.False(File.Exists(lAudioOld));
            Assert.True(File.Exists(lDoneNew));
            Assert.True(File.Exists(lPaletteOld));
            Assert.True(File.Exists(lIndexOld));
        }
        finally
        {
            LDepot.LDepotRootSet(null);
            try
            {
                Directory.Delete(lRoot, true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static string Seed(string lRoot, string? lFolder, string lName, DateTime lWriteUtc)
    {
        string lDirectory = lFolder is null ? lRoot : Path.Combine(lRoot, lFolder);
        Directory.CreateDirectory(lDirectory);
        string lPath = Path.Combine(lDirectory, lName);
        File.WriteAllText(lPath, "x");
        File.SetLastWriteTimeUtc(lPath, lWriteUtc);
        return lPath;
    }
}
