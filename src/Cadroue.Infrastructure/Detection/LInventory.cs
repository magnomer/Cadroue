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

    public static void LInventoryPrepare()
    {
        LInventoryInstalledRead();
        LInventoryFilterRead();
    }

    public static bool LInventoryFilterExist(string lInventoryFilter)
    {
        IReadOnlyCollection<string> lInventoryFilters = LInventoryFilterRead();
        return lInventoryFilters.Count == 0 || lInventoryFilters.Contains(lInventoryFilter);
    }

    public static bool LInventoryFilterConfirm(string lInventoryFilter)
    {
        IReadOnlyCollection<string> lInventoryFilters = LInventoryFilterRead();
        return lInventoryFilterStatus == LInventoryStatus.LInventoryStatusPresent
            && lInventoryFilters.Contains(lInventoryFilter);
    }

    public static IReadOnlyCollection<string> LInventoryFilterRead()
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
        lInventoryInstalledNames = null;
        lInventoryFilterNames = null;
        lInventoryInstalledStatus = LInventoryStatus.LInventoryStatusPending;
        lInventoryFilterStatus = LInventoryStatus.LInventoryStatusPending;
        lInventorySampleCache.Clear();
        lInventoryLayoutCache.Clear();
    }

    public static IReadOnlyList<string> LInventoryLayoutRead(string lInventoryEncoder)
    {
        if (string.IsNullOrWhiteSpace(lInventoryEncoder))
        {
            return Array.Empty<string>();
        }

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

    public static IReadOnlyList<int> LInventorySampleRead(string lInventoryEncoder)
    {
        if (string.IsNullOrWhiteSpace(lInventoryEncoder))
        {
            return Array.Empty<int>();
        }

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
