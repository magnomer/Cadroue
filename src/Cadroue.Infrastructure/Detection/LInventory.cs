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
    IReadOnlyDictionary<string, bool> LInventoryMap)
{
    public static async Task<LInventoryFeature> LInventoryFeatureRead(IReadOnlyList<string> lInventoryFilters)
    {
        await LInventory.LInventoryPrepare().ConfigureAwait(false);
        string lInventoryVersion = await LInventory.LInventoryVersionRead().ConfigureAwait(false);
        string lInventoryLocation = LInventoryLocationResolve();
        var lInventoryMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(lInventoryVersion))
        {
            foreach (string lInventoryFilter in lInventoryFilters)
            {
                lInventoryMap[lInventoryFilter] = LInventory.LInventoryFilterConfirm(lInventoryFilter);
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
}

public enum LInventoryStatus
{
    LInventoryStatusPending,
    LInventoryStatusFailed,
    LInventoryStatusEmpty,
    LInventoryStatusPresent
}

public static class LInventory
{
    private static IReadOnlyCollection<string>? lInventoryInstalledNames;
    private static IReadOnlyCollection<string>? lInventoryFilterNames;
    private static LInventoryStatus lInventoryFilterStatus;
    private static Task? lInventoryPrepareTask;
    private static readonly object lInventoryGate = new();

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
        LInventoryProcess lInventoryEncoders =
            await LInventoryProcess.LInventoryProcessRead("-encoders").ConfigureAwait(false);
        LInventoryProcess lInventoryFilters =
            await LInventoryProcess.LInventoryProcessRead("-filters").ConfigureAwait(false);
        lock (lInventoryGate)
        {
            if (lInventoryEncoders.LInventoryProcessSuccess)
            {
                var lInventoryNames = LInventoryFfmpeg.LInventoryEncodersParse(lInventoryEncoders.LInventoryProcessOut)
                    .Select(lInventoryEncoder => lInventoryEncoder.LInventoryEncoderName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                lInventoryInstalledNames = lInventoryNames;
            }

            if (lInventoryFilters.LInventoryProcessSuccess)
            {
                var lInventoryNames = LInventoryFfmpeg.LInventoryFiltersParse(lInventoryFilters.LInventoryProcessOut)
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
        }

        LInventoryHelp.LInventoryHelpReset();
    }

    public static async Task<IReadOnlyList<LInventoryEncoder>> LInventoryEncodersRead()
    {
        LInventoryProcess lInventoryProcess =
            await LInventoryProcess.LInventoryProcessRead("-encoders").ConfigureAwait(false);
        return lInventoryProcess.LInventoryProcessSuccess
            ? LInventoryFfmpeg.LInventoryEncodersParse(lInventoryProcess.LInventoryProcessOut)
            : Array.Empty<LInventoryEncoder>();
    }

    public static async Task<string> LInventoryVersionRead()
    {
        LInventoryProcess lInventoryProcess =
            await LInventoryProcess.LInventoryProcessRead("-version").ConfigureAwait(false);
        return lInventoryProcess.LInventoryProcessSuccess
            ? LInventoryFfmpeg.LInventoryVersionParse(lInventoryProcess.LInventoryProcessOut)
            : string.Empty;
    }

    public static async Task<IReadOnlyList<LInventoryEncoder>> LInventoryAudioRead() =>
        (await LInventoryEncodersRead().ConfigureAwait(false))
            .Where(lInventoryEncoder => lInventoryEncoder.LInventoryEncoderKind == LInventoryKind.LInventoryKindAudio)
            .ToList();
}
