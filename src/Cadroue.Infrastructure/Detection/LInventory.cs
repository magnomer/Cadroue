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

    public static void LInventoryReset() => lInventoryInstalledNames = null;

    public static IReadOnlyList<LInventoryEncoder> LInventoryEncodersRead()
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
            lInventoryStart.ArgumentList.Add("-encoders");

            using var lInventoryProcess = Process.Start(lInventoryStart);
            if (lInventoryProcess is null)
            {
                return Array.Empty<LInventoryEncoder>();
            }

            LCustody.LCustodyAttach(lInventoryProcess);
            Task<string> lInventoryOutputTask = lInventoryProcess.StandardOutput.ReadToEndAsync();
            Task<string> lInventoryErrorTask = lInventoryProcess.StandardError.ReadToEndAsync();
            lInventoryProcess.WaitForExit();
            _ = lInventoryErrorTask.GetAwaiter().GetResult();
            return LInventoryEncodersParse(lInventoryOutputTask.GetAwaiter().GetResult());
        }
        catch (Exception lInventoryException)
            when (lInventoryException is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return Array.Empty<LInventoryEncoder>();
        }
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
