using System.Linq;

namespace Cadroue.Infrastructure;

public static partial class LInventory
{
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

    internal static string LInventoryVersionParse(string lInventoryText)
    {
        if (string.IsNullOrWhiteSpace(lInventoryText))
        {
            return string.Empty;
        }

        string lInventoryFirst = lInventoryText
            .Split('\n')
            .Select(lInventoryLine => lInventoryLine.Trim())
            .FirstOrDefault(lInventoryLine => lInventoryLine.Length > 0) ?? string.Empty;
        const string lInventoryMark = "ffmpeg version ";
        if (!lInventoryFirst.StartsWith(lInventoryMark, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        string lInventoryRest = lInventoryFirst[lInventoryMark.Length..];
        int lInventorySpace = lInventoryRest.IndexOf(' ');
        return lInventorySpace < 0 ? lInventoryRest : lInventoryRest[..lInventorySpace];
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
