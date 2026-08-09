using Cadroue.Infrastructure;

namespace Cadroue.Tests;

public sealed class TRecordCleanup : IDisposable
{
    private readonly string tRecordCleanupRoot;

    public TRecordCleanup()
    {
        tRecordCleanupRoot = Path.Combine(Path.GetTempPath(), "cadroue-retention-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tRecordCleanupRoot);
        LDepot.LDepotRootSet(tRecordCleanupRoot);
    }

    public string SeedRecord(string? folder, string name, int ageDays)
    {
        string directory = folder is null ? tRecordCleanupRoot : Path.Combine(tRecordCleanupRoot, folder);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, name);
        File.WriteAllText(path, "x");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-ageDays));
        return path;
    }

    public int RunCleanup(int budgetDays) => LRetentionSweep.LRetentionRun(budgetDays);

    public bool RecordExists(string path) => File.Exists(path);

    public void Dispose()
    {
        LDepot.LDepotRootSet(null);
        try
        {
            Directory.Delete(tRecordCleanupRoot, true);
        }
        catch (IOException)
        {
        }
    }
}
