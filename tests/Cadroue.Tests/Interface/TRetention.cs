using Cadroue.Infrastructure;

namespace Cadroue.Tests;

public sealed class TRetention : IDisposable
{
    private readonly string tRetentionRoot;

    public TRetention()
    {
        tRetentionRoot = Path.Combine(Path.GetTempPath(), "cadroue-retention-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tRetentionRoot);
        LDepot.LDepotRootSet(tRetentionRoot);
    }

    public string TRetentionRecordCreate(string? folder, string name, int ageDays)
    {
        string directory = folder is null ? tRetentionRoot : Path.Combine(tRetentionRoot, folder);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, name);
        File.WriteAllText(path, "x");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-ageDays));
        return path;
    }

    public int TRetentionRun(int budgetDays) => LRetentionSweep.LRetentionRun(budgetDays);

    public bool TRetentionExist(string path) => File.Exists(path);

    public void Dispose()
    {
        LDepot.LDepotRootSet(null);
        try
        {
            Directory.Delete(tRetentionRoot, true);
        }
        catch (IOException)
        {
        }
    }
}
