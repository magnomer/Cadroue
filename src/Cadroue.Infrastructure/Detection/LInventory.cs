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
    private static LInventoryStatus lInventoryInstalledStatus;
    private static LInventoryStatus lInventoryFilterStatus;
    private static readonly Dictionary<string, IReadOnlyList<int>> lInventorySampleCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IReadOnlyList<string>> lInventoryLayoutCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly object lInventoryGate = new();

    public static void LInventoryPrepare()
    {
        LInventoryInstalledRead();
        LInventoryFilterRead();
    }

    public static void LInventoryPrepareStart() =>
        _ = System.Threading.Tasks.Task.Run(LInventoryPrepare);

    public static Task<LInventoryFeature> LInventoryFeatureRead(IReadOnlyList<string> lInventoryFilters) =>
        System.Threading.Tasks.Task.Run(() =>
        {
            string lInventoryVersion = LInventoryVersionRead();
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
        });

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
            IReadOnlyCollection<string> lInventoryFilters = LInventoryFilterRead();
            return lInventoryFilterStatus == LInventoryStatus.LInventoryStatusPresent
                && lInventoryFilters.Contains(lInventoryFilter);
        }
    }

    public static IReadOnlyCollection<string> LInventoryFilterRead()
    {
        lock (lInventoryGate)
        {
            return LInventoryFilterResolve();
        }
    }

    private static IReadOnlyCollection<string> LInventoryFilterResolve()
    {
        if (lInventoryFilterNames is not null)
        {
            return lInventoryFilterNames;
        }

        if (lInventoryFilterStatus == LInventoryStatus.LInventoryStatusFailed)
        {
            return Array.Empty<string>();
        }

        LInventoryProcess lInventoryProcess = LInventoryProcessRead("-filters");
        if (!lInventoryProcess.LInventoryProcessSuccess)
        {
            lInventoryFilterStatus = LInventoryStatus.LInventoryStatusFailed;
            return Array.Empty<string>();
        }

        var lInventoryNames = LInventoryFiltersParse(lInventoryProcess.LInventoryProcessOut)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        lInventoryFilterNames = lInventoryNames;
        lInventoryFilterStatus = lInventoryNames.Count > 0
            ? LInventoryStatus.LInventoryStatusPresent
            : LInventoryStatus.LInventoryStatusEmpty;
        return lInventoryNames;
    }

    public static IReadOnlyCollection<string> LInventoryInstalledRead()
    {
        lock (lInventoryGate)
        {
            return LInventoryInstalledResolve();
        }
    }

    private static IReadOnlyCollection<string> LInventoryInstalledResolve()
    {
        if (lInventoryInstalledNames is not null)
        {
            return lInventoryInstalledNames;
        }

        if (lInventoryInstalledStatus == LInventoryStatus.LInventoryStatusFailed)
        {
            return Array.Empty<string>();
        }

        LInventoryProcess lInventoryProcess = LInventoryProcessRead("-encoders");
        if (!lInventoryProcess.LInventoryProcessSuccess)
        {
            lInventoryInstalledStatus = LInventoryStatus.LInventoryStatusFailed;
            return Array.Empty<string>();
        }

        var lInventoryNames = LInventoryEncodersParse(lInventoryProcess.LInventoryProcessOut)
            .Select(lInventoryEncoder => lInventoryEncoder.LInventoryEncoderName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        lInventoryInstalledNames = lInventoryNames;
        lInventoryInstalledStatus = lInventoryNames.Count > 0
            ? LInventoryStatus.LInventoryStatusPresent
            : LInventoryStatus.LInventoryStatusEmpty;
        return lInventoryNames;
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
            lInventoryInstalledStatus = LInventoryStatus.LInventoryStatusPending;
            lInventoryFilterStatus = LInventoryStatus.LInventoryStatusPending;
            lInventorySampleCache.Clear();
            lInventoryLayoutCache.Clear();
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

            LInventoryProcess lInventoryProcess = LInventoryProcessRead("-h", "encoder=" + lInventoryEncoder);
            IReadOnlyList<string> lInventoryLayouts = lInventoryProcess.LInventoryProcessSuccess
                ? LInventoryLayoutParse(lInventoryProcess.LInventoryProcessOut)
                : Array.Empty<string>();
            lInventoryLayoutCache[lInventoryEncoder] = lInventoryLayouts;
            return lInventoryLayouts;
        }
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

            LInventoryProcess lInventoryProcess = LInventoryProcessRead("-h", "encoder=" + lInventoryEncoder);
            IReadOnlyList<int> lInventoryRates = lInventoryProcess.LInventoryProcessSuccess
                ? LInventorySampleParse(lInventoryProcess.LInventoryProcessOut)
                : Array.Empty<int>();
            lInventorySampleCache[lInventoryEncoder] = lInventoryRates;
            return lInventoryRates;
        }
    }


    public static IReadOnlyList<LInventoryEncoder> LInventoryEncodersRead()
    {
        LInventoryProcess lInventoryProcess = LInventoryProcessRead("-encoders");
        return lInventoryProcess.LInventoryProcessSuccess
            ? LInventoryEncodersParse(lInventoryProcess.LInventoryProcessOut)
            : Array.Empty<LInventoryEncoder>();
    }

    public static string LInventoryVersionRead()
    {
        LInventoryProcess lInventoryProcess = LInventoryProcessRead("-version");
        return lInventoryProcess.LInventoryProcessSuccess
            ? LInventoryVersionParse(lInventoryProcess.LInventoryProcessOut)
            : string.Empty;
    }


    public static IReadOnlyList<LInventoryEncoder> LInventoryAudioRead() =>
        LInventoryEncodersRead()
            .Where(lInventoryEncoder => lInventoryEncoder.LInventoryEncoderKind == LInventoryKind.LInventoryKindAudio)
            .ToList();
}
