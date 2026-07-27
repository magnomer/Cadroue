namespace Cadroue.Core;

public sealed class LDepotWatch : IDisposable
{
    private const int LDepotSettleMilliseconds = 250;

    private readonly List<FileSystemWatcher> lDepotWatchers = new();
    private readonly System.Timers.Timer lDepotSettleTimer;
    private bool lDepotDisposed;

    public LDepotWatch()
    {
        lDepotSettleTimer = new System.Timers.Timer(LDepotSettleMilliseconds) { AutoReset = false };
        lDepotSettleTimer.Elapsed += (_, _) => LDepotChange?.Invoke();
    }

    public event Action? LDepotChange;

    public void LDepotWatchStart()
    {
        LDepotWatchStop();
        LDepot.LDepotEnsure();

        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            var lDepotWatcher = new FileSystemWatcher(LDepot.LDepotFolderRead(lDepotFolder), "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = false
            };
            lDepotWatcher.Created += LDepotChangeHandle;
            lDepotWatcher.Deleted += LDepotChangeHandle;
            lDepotWatcher.Changed += LDepotChangeHandle;
            lDepotWatcher.Renamed += LDepotChangeHandle;
            lDepotWatcher.EnableRaisingEvents = true;
            lDepotWatchers.Add(lDepotWatcher);
        }
    }

    public void LDepotWatchStop()
    {
        foreach (FileSystemWatcher lDepotWatcher in lDepotWatchers)
        {
            lDepotWatcher.EnableRaisingEvents = false;
            lDepotWatcher.Created -= LDepotChangeHandle;
            lDepotWatcher.Deleted -= LDepotChangeHandle;
            lDepotWatcher.Changed -= LDepotChangeHandle;
            lDepotWatcher.Renamed -= LDepotChangeHandle;
            lDepotWatcher.Dispose();
        }

        lDepotWatchers.Clear();
        lDepotSettleTimer.Stop();
    }

    public void Dispose()
    {
        if (lDepotDisposed)
        {
            return;
        }

        lDepotDisposed = true;
        LDepotWatchStop();
        lDepotSettleTimer.Dispose();
    }

    private void LDepotChangeHandle(object lDepotSender, FileSystemEventArgs lDepotEvent)
    {
        lDepotSettleTimer.Stop();
        lDepotSettleTimer.Start();
    }
}
