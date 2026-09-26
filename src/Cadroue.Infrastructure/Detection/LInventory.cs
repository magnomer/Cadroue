using System.Linq;

namespace Cadroue.Infrastructure;

public enum LInventoryKind
{
    LInventoryKindVideo,
    LInventoryKindAudio,
    LInventoryKindSubtitle,
    LInventoryKindOther
}

public sealed record LInventoryEncoder(
    string LInventoryEncoderName,
    LInventoryKind LInventoryEncoderKind,
    bool LInventoryEncoderExperimental,
    string LInventoryEncoderSummary);

public sealed record LInventoryFeature(
    string LInventoryVersion,
    string LInventoryLocation,
    IReadOnlyDictionary<string, bool> LInventoryMap);

public enum LInventoryStatus
{
    LInventoryStatusPending,
    LInventoryStatusFailed,
    LInventoryStatusEmpty,
    LInventoryStatusPresent
}

public static partial class LInventory
{
    private static IReadOnlyCollection<string>? lInventoryInstalledNames;
    private static IReadOnlyCollection<string>? lInventoryFilterNames;
    private static LInventoryStatus lInventoryFilterStatus;
    private static Task? lInventoryPrepareTask;
    private static readonly Dictionary<string, IReadOnlyList<int>> lInventorySampleCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IReadOnlyList<string>> lInventoryLayoutCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Task> lInventoryHelpTasks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object lInventoryGate = new();

    public static event Action<string>? LInventoryHelpReady;

    public static Task LInventoryPrepare()
    {
        lock (lInventoryGate)
        {
            return lInventoryPrepareTask ??= Task.Run(LInventoryLoadRun);
        }
    }

    public static void LInventoryPrepareStart() => _ = LInventoryPrepare();

    private static async Task LInventoryLoadRun()
    {
        LInventoryProcess lInventoryEncoders = await LInventoryProcessRead("-encoders").ConfigureAwait(false);
        LInventoryProcess lInventoryFilters = await LInventoryProcessRead("-filters").ConfigureAwait(false);
        lock (lInventoryGate)
        {
            if (lInventoryEncoders.LInventoryProcessSuccess)
            {
                var lInventoryNames = LInventoryEncodersParse(lInventoryEncoders.LInventoryProcessOut)
                    .Select(lInventoryEncoder => lInventoryEncoder.LInventoryEncoderName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                lInventoryInstalledNames = lInventoryNames;
            }

            if (lInventoryFilters.LInventoryProcessSuccess)
            {
                var lInventoryNames = LInventoryFiltersParse(lInventoryFilters.LInventoryProcessOut)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                lInventoryFilterNames = lInventoryNames;
                lInventoryFilterStatus = lInventoryNames.Count > 0
                    ? LInventoryStatus.LInventoryStatusPresent
                    : LInventoryStatus.LInventoryStatusEmpty;
            }
            else
            {
                lInventoryFilterStatus = LInventoryStatus.LInventoryStatusFailed;
            }
        }
    }

    public static async Task<LInventoryFeature> LInventoryFeatureRead(IReadOnlyList<string> lInventoryFilters)
    {
        await LInventoryPrepare().ConfigureAwait(false);
        string lInventoryVersion = await LInventoryVersionRead().ConfigureAwait(false);
        string lInventoryLocation = LInventoryLocationResolve();
        var lInventoryMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(lInventoryVersion))
        {
            foreach (string lInventoryFilter in lInventoryFilters)
            {
                lInventoryMap[lInventoryFilter] = LInventoryFilterConfirm(lInventoryFilter);
            }
        }

        return new LInventoryFeature(lInventoryVersion, lInventoryLocation, lInventoryMap);
    }

    private static string LInventoryLocationResolve()
    {
        string lInventoryExe = Cadroue.Media.LTool.LToolFfmpegRead();
        if (!Path.IsPathRooted(lInventoryExe))
        {
            return string.Empty;
        }

        string? lInventoryFolder = Path.GetDirectoryName(lInventoryExe);
        return string.IsNullOrEmpty(lInventoryFolder) ? lInventoryExe : lInventoryFolder;
    }

    public static bool LInventoryFilterExist(string lInventoryFilter)
    {
        IReadOnlyCollection<string> lInventoryFilters = LInventoryFilterRead();
        return lInventoryFilters.Count == 0 || lInventoryFilters.Contains(lInventoryFilter);
    }

    public static bool LInventoryFilterConfirm(string lInventoryFilter)
    {
        lock (lInventoryGate)
        {
            return lInventoryFilterStatus == LInventoryStatus.LInventoryStatusPresent
                && lInventoryFilterNames is not null
                && lInventoryFilterNames.Contains(lInventoryFilter);
        }
    }

    public static IReadOnlyCollection<string> LInventoryFilterRead()
    {
        LInventoryPrepareStart();
        lock (lInventoryGate)
        {
            return lInventoryFilterNames ?? Array.Empty<string>();
        }
    }

    public static IReadOnlyCollection<string> LInventoryInstalledRead()
    {
        LInventoryPrepareStart();
        lock (lInventoryGate)
        {
            return lInventoryInstalledNames ?? Array.Empty<string>();
        }
    }

    public static bool LInventoryInstalledCheck(string lInventoryName)
    {
        IReadOnlyCollection<string> lInventoryNames = LInventoryInstalledRead();
        return lInventoryNames.Count == 0 || lInventoryNames.Contains(lInventoryName);
    }

    public static void LInventoryReset()
    {
        lock (lInventoryGate)
        {
            lInventoryInstalledNames = null;
            lInventoryFilterNames = null;
            lInventoryPrepareTask = null;
            lInventoryFilterStatus = LInventoryStatus.LInventoryStatusPending;
            lInventorySampleCache.Clear();
            lInventoryLayoutCache.Clear();
            lInventoryHelpTasks.Clear();
        }
    }

    public static IReadOnlyList<string> LInventoryLayoutRead(string lInventoryEncoder)
    {
        if (string.IsNullOrWhiteSpace(lInventoryEncoder))
        {
            return Array.Empty<string>();
        }

        lock (lInventoryGate)
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

        lock (lInventoryGate)
        {
            if (lInventorySampleCache.TryGetValue(lInventoryEncoder, out IReadOnlyList<int>? lInventoryCached))
            {
                return lInventoryCached;
            }
        }

        LInventoryHelpStart(lInventoryEncoder);
        return Array.Empty<int>();
    }

    private static void LInventoryHelpStart(string lInventoryEncoder)
    {
        lock (lInventoryGate)
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
        LInventoryProcess lInventoryProcess =
            await LInventoryProcessRead("-h", "encoder=" + lInventoryEncoder).ConfigureAwait(false);
        lock (lInventoryGate)
        {
            lInventoryLayoutCache[lInventoryEncoder] = lInventoryProcess.LInventoryProcessSuccess
                ? LInventoryLayoutParse(lInventoryProcess.LInventoryProcessOut)
                : Array.Empty<string>();
            lInventorySampleCache[lInventoryEncoder] = lInventoryProcess.LInventoryProcessSuccess
                ? LInventorySampleParse(lInventoryProcess.LInventoryProcessOut)
                : Array.Empty<int>();
        }

        LInventoryHelpReady?.Invoke(lInventoryEncoder);
    }

    public static async Task<IReadOnlyList<LInventoryEncoder>> LInventoryEncodersRead()
    {
        LInventoryProcess lInventoryProcess = await LInventoryProcessRead("-encoders").ConfigureAwait(false);
        return lInventoryProcess.LInventoryProcessSuccess
            ? LInventoryEncodersParse(lInventoryProcess.LInventoryProcessOut)
            : Array.Empty<LInventoryEncoder>();
    }

    public static async Task<string> LInventoryVersionRead()
    {
        LInventoryProcess lInventoryProcess = await LInventoryProcessRead("-version").ConfigureAwait(false);
        return lInventoryProcess.LInventoryProcessSuccess
            ? LInventoryVersionParse(lInventoryProcess.LInventoryProcessOut)
            : string.Empty;
    }

    public static async Task<IReadOnlyList<LInventoryEncoder>> LInventoryAudioRead() =>
        (await LInventoryEncodersRead().ConfigureAwait(false))
            .Where(lInventoryEncoder => lInventoryEncoder.LInventoryEncoderKind == LInventoryKind.LInventoryKindAudio)
            .ToList();
}
