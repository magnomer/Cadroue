namespace Cadroue.Infrastructure;

public static class LDepotOwnership
{
    private const long LDepotOwnMilliseconds = 3000;

    private static readonly Dictionary<string, long> lDepotOwnPaths = new(StringComparer.OrdinalIgnoreCase);

    public static void LDepotOwnershipAdd(string lDepotFilePath)
    {
        lock (lDepotOwnPaths)
        {
            lDepotOwnPaths[Path.GetFullPath(lDepotFilePath)] = Environment.TickCount64;
        }
    }

    internal static bool LDepotOwnershipCheck(FileSystemEventArgs lDepotEvent)
    {
        long lDepotNow = Environment.TickCount64;
        lock (lDepotOwnPaths)
        {
            foreach (string lDepotStale in lDepotOwnPaths
                .Where(lDepotEntry => lDepotNow - lDepotEntry.Value > LDepotOwnMilliseconds)
                .Select(lDepotEntry => lDepotEntry.Key)
                .ToArray())
            {
                lDepotOwnPaths.Remove(lDepotStale);
            }

            return lDepotOwnPaths.ContainsKey(lDepotEvent.FullPath)
                && (lDepotEvent is not RenamedEventArgs lDepotRename
                    || !lDepotRename.OldFullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    || lDepotOwnPaths.ContainsKey(lDepotRename.OldFullPath));
        }
    }
}
