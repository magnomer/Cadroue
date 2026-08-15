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

public static class LInventory
{
    private static IReadOnlyCollection<string>? lInventoryInstalledNames;
    private static IReadOnlyCollection<string>? lInventoryFilterNames;

    public static void LInventoryWarm()
    {
        LInventoryInstalledRead();
        LInventoryFilterRead();
    }

    public static bool LInventoryFilterExist(string lInventoryFilter)
    {
        IReadOnlyCollection<string> lInventoryFilters = LInventoryFilterRead();
        return lInventoryFilters.Count == 0 || lInventoryFilters.Contains(lInventoryFilter);
    }

    public static IReadOnlyCollection<string> LInventoryFilterRead()
    {
        if (lInventoryFilterNames is { Count: > 0 })
        {
            return lInventoryFilterNames;
        }

        var lInventoryNames = LInventoryFiltersParse(LInventoryProcessRead("-filters"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (lInventoryNames.Count > 0)
        {
            lInventoryFilterNames = lInventoryNames;
        }

        return lInventoryNames;
    }

    public static IReadOnlyCollection<string> LInventoryInstalledRead()
    {
        if (lInventoryInstalledNames is { Count: > 0 })
        {
            return lInventoryInstalledNames;
        }

        var lInventoryNames = LInventoryEncodersRead()
            .Select(lInventoryEncoder => lInventoryEncoder.LInventoryEncoderName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (lInventoryNames.Count > 0)
        {
            lInventoryInstalledNames = lInventoryNames;
        }

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
    }

    public static IReadOnlyList<LInventoryEncoder> LInventoryEncodersRead() =>
        LInventoryEncodersParse(LInventoryProcessRead("-encoders"));

    public static string LInventoryVersionRead() =>
        LInventoryVersionParse(LInventoryProcessRead("-version"));

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

    private static string LInventoryProcessRead(string lInventoryArgument)
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
            lInventoryStart.ArgumentList.Add(lInventoryArgument);

            using var lInventoryProcess = Process.Start(lInventoryStart);
            if (lInventoryProcess is null)
            {
                return string.Empty;
            }

            LCustody.LCustodyAttach(lInventoryProcess);
            Task<string> lInventoryOutputTask = lInventoryProcess.StandardOutput.ReadToEndAsync();
            Task<string> lInventoryErrorTask = lInventoryProcess.StandardError.ReadToEndAsync();
            lInventoryProcess.WaitForExit();
            _ = lInventoryErrorTask.GetAwaiter().GetResult();
            return lInventoryOutputTask.GetAwaiter().GetResult();
        }
        catch (Exception lInventoryException)
            when (lInventoryException is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return string.Empty;
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
                || !LInventoryFilterFlagsCheck(lInventoryParts[0])
                || !lInventoryParts[2].Contains("->", StringComparison.Ordinal))
            {
                continue;
            }

            lInventoryList.Add(lInventoryParts[1]);
        }

        return lInventoryList;
    }

    private static bool LInventoryFilterFlagsCheck(string lInventoryFlags)
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
