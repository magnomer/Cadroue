using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cadroue.Core;
using Cadroue.Media;

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

public static class LInventory
{
    private const int LInventoryTimeout = 20000;

    private static IReadOnlyCollection<string>? lInventoryInstalledNames;
    private static IReadOnlyCollection<string>? lInventoryFilterNames;
    private static LInventoryStatus lInventoryInstalledStatus;
    private static LInventoryStatus lInventoryFilterStatus;
    private static readonly Dictionary<string, IReadOnlyList<int>> lInventorySampleCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IReadOnlyList<string>> lInventoryLayoutCache = new(StringComparer.OrdinalIgnoreCase);

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

    private static IReadOnlyList<string> LInventoryLayoutParse(string lInventoryText)
    {
        const string lInventoryMark = "Supported channel layouts:";
        foreach (string lInventoryRawLine in lInventoryText.Split('\n'))
        {
            string lInventoryLine = lInventoryRawLine.TrimEnd('\r').Trim();
            int lInventoryStart = lInventoryLine.IndexOf(lInventoryMark, StringComparison.Ordinal);
            if (lInventoryStart < 0)
            {
                continue;
            }

            var lInventoryLayouts = new List<string>();
            foreach (string lInventoryToken in lInventoryLine[(lInventoryStart + lInventoryMark.Length)..]
                         .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (LInventoryLayoutCheck(lInventoryToken) && !lInventoryLayouts.Contains(lInventoryToken))
                {
                    lInventoryLayouts.Add(lInventoryToken);
                }
            }

            return lInventoryLayouts;
        }

        return Array.Empty<string>();
    }

    private static bool LInventoryLayoutCheck(string lInventoryToken)
    {
        if (lInventoryToken.Length == 0
            || string.Equals(lInventoryToken, "channels", StringComparison.Ordinal)
            || !char.IsLetterOrDigit(lInventoryToken[0])
            || lInventoryToken.All(char.IsDigit))
        {
            return false;
        }

        foreach (char lInventoryChar in lInventoryToken)
        {
            if (!char.IsLetterOrDigit(lInventoryChar) && lInventoryChar is not ('.' or '(' or ')' or '-' or '+'))
            {
                return false;
            }
        }

        return true;
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

    private static IReadOnlyList<int> LInventorySampleParse(string lInventoryText)
    {
        const string lInventoryMark = "Supported sample rates:";
        foreach (string lInventoryRawLine in lInventoryText.Split('\n'))
        {
            string lInventoryLine = lInventoryRawLine.TrimEnd('\r').Trim();
            int lInventoryStart = lInventoryLine.IndexOf(lInventoryMark, StringComparison.Ordinal);
            if (lInventoryStart < 0)
            {
                continue;
            }

            var lInventoryRates = new List<int>();
            foreach (string lInventoryToken in lInventoryLine[(lInventoryStart + lInventoryMark.Length)..]
                         .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(lInventoryToken, out int lInventoryRate) && lInventoryRate > 0)
                {
                    lInventoryRates.Add(lInventoryRate);
                }
            }

            lInventoryRates.Sort();
            return lInventoryRates;
        }

        return Array.Empty<int>();
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

    private static string LInventoryVersionParse(string lInventoryText)
    {
        if (string.IsNullOrWhiteSpace(lInventoryText))
        {
            return string.Empty;
        }

        string lInventoryFirst = lInventoryText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(string.Empty)
            .Trim();
        const string lInventoryMark = "ffmpeg version ";
        int lInventoryStart = lInventoryFirst.IndexOf(lInventoryMark, StringComparison.OrdinalIgnoreCase);
        if (lInventoryStart < 0)
        {
            return lInventoryFirst;
        }

        string lInventoryRest = lInventoryFirst[(lInventoryStart + lInventoryMark.Length)..];
        int lInventorySpace = lInventoryRest.IndexOf(' ');
        return lInventorySpace < 0 ? lInventoryRest : lInventoryRest[..lInventorySpace];
    }

    private static LInventoryProcess LInventoryProcessRead(params string[] lInventoryArguments)
    {
        try
        {
            var lInventoryStart = new ProcessStartInfo(LTool.LToolFfmpegRead())
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            lInventoryStart.ArgumentList.Add("-hide_banner");
            foreach (string lInventoryArgument in lInventoryArguments)
            {
                lInventoryStart.ArgumentList.Add(lInventoryArgument);
            }

            using var lInventoryProcess = Process.Start(lInventoryStart);
            if (lInventoryProcess is null)
            {
                return LInventoryProcess.LInventoryProcessFailure;
            }

            LCustody.LCustodyAttach(lInventoryProcess);
            Task<string> lInventoryOutputTask = lInventoryProcess.StandardOutput.ReadToEndAsync();
            Task<string> lInventoryErrorTask = lInventoryProcess.StandardError.ReadToEndAsync();
            if (!lInventoryProcess.WaitForExit(LInventoryTimeout))
            {
                LInventoryProcessInterrupt(lInventoryProcess);
                return LInventoryProcess.LInventoryProcessFailure;
            }

            string lInventoryError = lInventoryErrorTask.GetAwaiter().GetResult();
            string lInventoryOutput = lInventoryOutputTask.GetAwaiter().GetResult();
            return new LInventoryProcess(lInventoryProcess.ExitCode == 0, lInventoryOutput, lInventoryError);
        }
        catch (Exception lInventoryException)
            when (lInventoryException is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return LInventoryProcess.LInventoryProcessFailure;
        }
    }

    private sealed record LInventoryProcess(bool LInventoryProcessSuccess, string LInventoryProcessOut, string LInventoryProcessError)
    {
        public static readonly LInventoryProcess LInventoryProcessFailure = new(false, string.Empty, string.Empty);
    }

    private static void LInventoryProcessInterrupt(Process lInventoryProcess)
    {
        try
        {
            lInventoryProcess.Kill(true);
        }
        catch (Exception lInventoryException)
            when (lInventoryException is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException)
        {
        }
    }

    private static IReadOnlyList<string> LInventoryFiltersParse(string lInventoryText)
    {
        var lInventoryList = new List<string>();
        foreach (string lInventoryRawLine in lInventoryText.Split('\n'))
        {
            string[] lInventoryParts = lInventoryRawLine
                .TrimEnd('\r')
                .TrimStart()
                .Split((char[]?)null, 4, StringSplitOptions.RemoveEmptyEntries);
            if (lInventoryParts.Length < 3
                || lInventoryParts[0].Length is < 1 or > 3
                || !LInventoryFilterCheck(lInventoryParts[0])
                || !lInventoryParts[2].Contains("->", StringComparison.Ordinal))
            {
                continue;
            }

            lInventoryList.Add(lInventoryParts[1]);
        }

        return lInventoryList;
    }

    private static bool LInventoryFilterCheck(string lInventoryFlags)
    {
        foreach (char lInventoryChar in lInventoryFlags)
        {
            if (lInventoryChar is not ('.' or 'T' or 'S' or 'C'))
            {
                return false;
            }
        }

        return true;
    }

    public static IReadOnlyList<LInventoryEncoder> LInventoryAudioRead() =>
        LInventoryEncodersRead()
            .Where(lInventoryEncoder => lInventoryEncoder.LInventoryEncoderKind == LInventoryKind.LInventoryKindAudio)
            .ToList();

    private static IReadOnlyList<LInventoryEncoder> LInventoryEncodersParse(string lInventoryText)
    {
        var lInventoryList = new List<LInventoryEncoder>();
        bool lInventoryStarted = false;
        foreach (string lInventoryRawLine in lInventoryText.Split('\n'))
        {
            string lInventoryLine = lInventoryRawLine.TrimEnd('\r');
            if (!lInventoryStarted)
            {
                if (lInventoryLine.Contains("------", StringComparison.Ordinal))
                {
                    lInventoryStarted = true;
                }

                continue;
            }

            string[] lInventoryParts = lInventoryLine
                .TrimStart()
                .Split((char[]?)null, 3, StringSplitOptions.RemoveEmptyEntries);
            if (lInventoryParts.Length < 2 || lInventoryParts[0].Length != 6 || !LInventoryFlagsCheck(lInventoryParts[0]))
            {
                continue;
            }

            LInventoryKind lInventoryKind = lInventoryParts[0][0] switch
            {
                'V' => LInventoryKind.LInventoryKindVideo,
                'A' => LInventoryKind.LInventoryKindAudio,
                'S' => LInventoryKind.LInventoryKindSubtitle,
                _ => LInventoryKind.LInventoryKindOther
            };

            lInventoryList.Add(new LInventoryEncoder(
                lInventoryParts[1],
                lInventoryKind,
                lInventoryParts[0][3] == 'X',
                lInventoryParts.Length >= 3 ? lInventoryParts[2].Trim() : string.Empty));
        }

        return lInventoryList;
    }

    private static bool LInventoryFlagsCheck(string lInventoryFlags)
    {
        foreach (char lInventoryChar in lInventoryFlags)
        {
            if (lInventoryChar != '.' && !char.IsLetter(lInventoryChar))
            {
                return false;
            }
        }

        return true;
    }
}
