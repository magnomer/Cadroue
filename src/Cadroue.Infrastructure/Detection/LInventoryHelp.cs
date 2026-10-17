namespace Cadroue.Infrastructure;

public static class LInventoryHelp
{
    private static readonly Dictionary<string, IReadOnlyList<int>> lInventorySampleCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IReadOnlyList<string>> lInventoryLayoutCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Task> lInventoryHelpTasks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object lInventoryHelpGate = new();

    public static event Action<string>? LInventoryHelpReady;

    public static IReadOnlyList<string> LInventoryLayoutRead(string lInventoryEncoder)
    {
        if (string.IsNullOrWhiteSpace(lInventoryEncoder))
        {
            return Array.Empty<string>();
        }

        lock (lInventoryHelpGate)
        {
            if (lInventoryLayoutCache.TryGetValue(lInventoryEncoder, out IReadOnlyList<string>? lInventoryCached))
            {
                return lInventoryCached;
            }
        }

        LInventoryHelpStart(lInventoryEncoder);
        return Array.Empty<string>();
    }

    public static IReadOnlyList<int> LInventorySampleRead(string lInventoryEncoder)
    {
        if (string.IsNullOrWhiteSpace(lInventoryEncoder))
        {
            return Array.Empty<int>();
        }

        lock (lInventoryHelpGate)
        {
            if (lInventorySampleCache.TryGetValue(lInventoryEncoder, out IReadOnlyList<int>? lInventoryCached))
            {
                return lInventoryCached;
            }
        }

        LInventoryHelpStart(lInventoryEncoder);
        return Array.Empty<int>();
    }

    internal static void LInventoryHelpReset()
    {
        lock (lInventoryHelpGate)
        {
            lInventorySampleCache.Clear();
            lInventoryLayoutCache.Clear();
            lInventoryHelpTasks.Clear();
        }
    }

    private static void LInventoryHelpStart(string lInventoryEncoder)
    {
        lock (lInventoryHelpGate)
        {
            if (lInventoryHelpTasks.ContainsKey(lInventoryEncoder))
            {
                return;
            }

            lInventoryHelpTasks[lInventoryEncoder] = Task.Run(() => LInventoryHelpLoad(lInventoryEncoder));
        }
    }

    private static async Task LInventoryHelpLoad(string lInventoryEncoder)
    {
        LInventoryProcess lInventoryProcess = await LInventoryProcess
            .LInventoryProcessRead("-h", "encoder=" + lInventoryEncoder).ConfigureAwait(false);
        lock (lInventoryHelpGate)
        {
            lInventoryLayoutCache[lInventoryEncoder] = lInventoryProcess.LInventoryProcessSuccess
                ? LInventoryFfmpeg.LInventoryLayoutParse(lInventoryProcess.LInventoryProcessOut)
                : Array.Empty<string>();
            lInventorySampleCache[lInventoryEncoder] = lInventoryProcess.LInventoryProcessSuccess
                ? LInventoryFfmpeg.LInventorySampleParse(lInventoryProcess.LInventoryProcessOut)
                : Array.Empty<int>();
        }

        LInventoryHelpReady?.Invoke(lInventoryEncoder);
    }
}
