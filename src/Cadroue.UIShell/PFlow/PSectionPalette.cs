using Cadroue.Core;
using Cadroue.Application;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Cadroue.UIShell.PFlow;

internal static class PSectionPalette
{
    internal const string PSectionPaletteDefault = "Cadroue";

    private const string PSectionHiddenFile = ".hidden.json";

    private const byte PSectionBandAlpha = 0x99;

    private static readonly (string Name, string[] Hex)[] pSectionNativeHex =
    {
        ("Cadroue", new[]
        {
            "#4A90D9", "#27AE60", "#E67E22", "#8E44AD", "#E74C3C",
            "#16A085", "#F1C40F", "#D6499A", "#34495E", "#7F8C8D"
        }),
        ("Muted", new[]
        {
            "#5B7C99", "#6A9C78", "#C08552", "#8E7CA6", "#C0736B",
            "#5F9EA0", "#B5A25D", "#A9788F", "#4F5D6B", "#8C8579"
        }),
        ("Vivid", new[]
        {
            "#2979FF", "#00C853", "#FF9100", "#AA00FF", "#FF1744",
            "#00BFA5", "#FFD600", "#F50057", "#00B0FF", "#64DD17"
        }),
        ("Contrast", new[]
        {
            "#332288", "#88CCEE", "#44AA99", "#117733", "#999933",
            "#DDCC77", "#CC6677", "#882255", "#AA4499", "#DDDDDD"
        }),
        ("Petroff", new[]
        {
            "#707480", "#FFA90D", "#832DB5", "#E76300", "#92DADD",
            "#A96B59", "#3F90DA", "#B8AB6F", "#BC1E00", "#94A4A2"
        }),
        ("Observable", new[]
        {
            "#9498A0", "#EFB118", "#A463F2", "#3CA951", "#FF8AB7",
            "#6CC5B0", "#FF725C", "#4269D0", "#9C6B4E", "#97BBF5"
        }),
        ("Tableau", new[]
        {
            "#9C755F", "#76B7B2", "#E15759", "#EDC948", "#B07AA1",
            "#F28E2B", "#4E79A7", "#FF9DA7", "#59A14F", "#BAB0AC"
        })
    };

    private static readonly (string Name, Color[] Colors)[] pSectionPalettes = pSectionNativeHex
        .Select(pNative => (pNative.Name, pNative.Hex.Select(pHex => PSectionHexParse(pHex) ?? Colors.Gray).ToArray()))
        .ToArray();

    private static readonly List<(string Name, Color[] Colors, string Path)> pSectionLoaded = new();

    private static readonly HashSet<string> pSectionHidden = new(StringComparer.Ordinal);

    private static Dictionary<string, Brush[]> pSectionBandBrushes = PSectionBrushesCreate(PSectionBandAlpha);
    private static Dictionary<string, Brush[]> pSectionBadgeBrushes = PSectionBrushesCreate(0xFF);

    private static IEnumerable<(string Name, Color[] Colors)> PSectionAllRead() =>
        pSectionPalettes
            .Where(pPalette => !pSectionHidden.Contains(pPalette.Name))
            .Concat(pSectionLoaded.Select(pEntry => (pEntry.Name, pEntry.Colors)));

    internal static bool PSectionNativeCheck(string pName) =>
        pSectionPalettes.Any(pPalette => string.Equals(pPalette.Name, pName, StringComparison.Ordinal));

    internal static bool PSectionFixedCheck(string pName) =>
        string.Equals(pName, PSectionPaletteDefault, StringComparison.Ordinal);

    private static Dictionary<string, Brush[]> PSectionBrushesCreate(byte pSectionAlpha)
    {
        var pSectionSets = new Dictionary<string, Brush[]>(StringComparer.Ordinal);
        foreach ((string pName, Color[] pColors) in PSectionAllRead())
        {
            var pSectionBrushes = new Brush[pColors.Length];
            for (int pSectionIndex = 0; pSectionIndex < pColors.Length; pSectionIndex++)
            {
                Color pSectionColor = pColors[pSectionIndex];
                var pSectionBrush = new SolidColorBrush(
                    Color.FromArgb(pSectionAlpha, pSectionColor.R, pSectionColor.G, pSectionColor.B));
                pSectionBrush.Freeze();
                pSectionBrushes[pSectionIndex] = pSectionBrush;
            }

            pSectionSets[pName] = pSectionBrushes;
        }

        return pSectionSets;
    }

    internal static IReadOnlyList<string> PSectionPaletteNames =>
        PSectionAllRead().Select(pPalette => pPalette.Name).ToArray();

    internal static bool PSectionPaletteCheck(string pName) =>
        PSectionAllRead().Any(pPalette => string.Equals(pPalette.Name, pName, StringComparison.Ordinal));

    internal static int PSectionActiveCount => PSectionSetRead(pSectionBadgeBrushes).Length;

    internal static IReadOnlyList<Brush> PSectionBadgesRead(string pName) =>
        pSectionBadgeBrushes.TryGetValue(pName, out Brush[]? pBrushes)
            ? pBrushes
            : pSectionBadgeBrushes[PSectionPaletteDefault];

    internal static Brush PSectionPaletteRead(int pColorIndex)
    {
        Brush[] pSet = PSectionSetRead(pSectionBandBrushes);
        return pSet[Math.Abs(pColorIndex) % pSet.Length];
    }

    internal static Brush PSectionBadgeRead(int pColorIndex)
    {
        Brush[] pSet = PSectionSetRead(pSectionBadgeBrushes);
        return pSet[Math.Abs(pColorIndex) % pSet.Length];
    }

    private static Brush[] PSectionSetRead(Dictionary<string, Brush[]> pSectionSets)
    {
        string pActive = LPreference.LPreferenceStateCurrent.LPreferenceSectionPalette;
        return pSectionSets.TryGetValue(pActive, out Brush[]? pBrushes)
            ? pBrushes
            : pSectionSets[PSectionPaletteDefault];
    }

    internal static void PSectionPaletteLoad()
    {
        PSectionHiddenLoad();
        pSectionLoaded.Clear();
        foreach (string pFilePath in PSectionFilesRead())
        {
            if (PSectionFileRead(pFilePath) is not { } pPalette)
            {
                continue;
            }

            string pName = pPalette.Name;
            int pSuffix = 2;
            while (pSectionPalettes.Any(pBuiltIn => pBuiltIn.Name == pName)
                || pSectionLoaded.Any(pOther => pOther.Name == pName))
            {
                pName = $"{pPalette.Name} {pSuffix++}";
            }

            pSectionLoaded.Add((pName, pPalette.Colors, pFilePath));
        }

        pSectionBandBrushes = PSectionBrushesCreate(PSectionBandAlpha);
        pSectionBadgeBrushes = PSectionBrushesCreate(0xFF);
    }

    internal static bool PSectionPaletteRemove(string pName)
    {
        if (PSectionFixedCheck(pName))
        {
            return false;
        }

        if (PSectionNativeCheck(pName))
        {
            pSectionHidden.Add(pName);
            PSectionHiddenSave();
            PSectionPaletteLoad();
            return true;
        }

        (string Name, Color[] Colors, string Path) pEntry =
            pSectionLoaded.FirstOrDefault(pLoaded => pLoaded.Name == pName);
        if (pEntry.Path is null)
        {
            return false;
        }

        try
        {
            File.Delete(pEntry.Path);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        PSectionPaletteLoad();
        return true;
    }

    internal static string? PSectionPaletteImport(string pSourcePath)
    {
        if (PSectionFileRead(pSourcePath) is not { } pPalette)
        {
            return null;
        }

        string pTargetPath = Path.Combine(
            Cadroue.Infrastructure.LDepot.LDepotPaletteRead(),
            PSectionFileCreate(pPalette.Name));
        File.Copy(pSourcePath, pTargetPath, true);
        PSectionPaletteLoad();
        return PSectionAllRead()
            .Select(pEntry => pEntry.Name)
            .LastOrDefault(pName => pName == pPalette.Name || pName.StartsWith(pPalette.Name + " ", StringComparison.Ordinal))
            ?? pPalette.Name;
    }

    internal static void PSectionPaletteSave(string pName, string pTargetPath)
    {
        (string Name, Color[] Colors) pPalette = PSectionAllRead().FirstOrDefault(pEntry => pEntry.Name == pName);
        if (pPalette.Colors is null)
        {
            return;
        }

        var pRecord = new PSectionPaletteRecord
        {
            PSectionPaletteName = pPalette.Name,
            PSectionPaletteColors = pPalette.Colors.Select(PSectionHexFormat).ToArray()
        };
        File.WriteAllText(pTargetPath, JsonSerializer.Serialize(pRecord, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string PSectionHiddenRead() =>
        Path.Combine(Cadroue.Infrastructure.LDepot.LDepotPaletteRead(), PSectionHiddenFile);

    private static void PSectionHiddenLoad()
    {
        pSectionHidden.Clear();
        try
        {
            string pHiddenPath = PSectionHiddenRead();
            if (!File.Exists(pHiddenPath))
            {
                return;
            }

            foreach (string pName in JsonSerializer.Deserialize<string[]>(File.ReadAllText(pHiddenPath)) ?? Array.Empty<string>())
            {
                if (!PSectionFixedCheck(pName))
                {
                    pSectionHidden.Add(pName);
                }
            }
        }
        catch (Exception)
        {
            pSectionHidden.Clear();
        }
    }

    private static void PSectionHiddenSave()
    {
        try
        {
            File.WriteAllText(
                PSectionHiddenRead(),
                JsonSerializer.Serialize(pSectionHidden.ToArray(), new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static IEnumerable<string> PSectionFilesRead()
    {
        try
        {
            return Directory.EnumerateFiles(Cadroue.Infrastructure.LDepot.LDepotPaletteRead(), "*.json")
                .Where(pPath => !string.Equals(Path.GetFileName(pPath), PSectionHiddenFile, StringComparison.OrdinalIgnoreCase))
                .OrderBy(pPath => pPath);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    private static (string Name, Color[] Colors)? PSectionFileRead(string pFilePath)
    {
        try
        {
            PSectionPaletteRecord? pRecord = JsonSerializer.Deserialize<PSectionPaletteRecord>(File.ReadAllText(pFilePath));
            if (pRecord is null || string.IsNullOrWhiteSpace(pRecord.PSectionPaletteName) || pRecord.PSectionPaletteColors.Length == 0)
            {
                return null;
            }

            var pColors = new List<Color>();
            foreach (string pHex in pRecord.PSectionPaletteColors)
            {
                if (PSectionHexParse(pHex) is { } pColor)
                {
                    pColors.Add(pColor);
                }
            }

            return pColors.Count == 0 ? null : (pRecord.PSectionPaletteName.Trim(), pColors.ToArray());
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string PSectionFileCreate(string pName)
    {
        string pSafe = new(pName.Select(pCharacter => Path.GetInvalidFileNameChars().Contains(pCharacter) ? '_' : pCharacter).ToArray());
        return $"{pSafe}.json";
    }

    private static string PSectionHexFormat(Color pColor) => $"#{pColor.R:X2}{pColor.G:X2}{pColor.B:X2}";

    private static Color? PSectionHexParse(string pHex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(pHex.Trim());
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed class PSectionPaletteRecord
    {
        [JsonPropertyName("Name")]
        public string PSectionPaletteName { get; set; } = string.Empty;

        [JsonPropertyName("Colors")]
        public string[] PSectionPaletteColors { get; set; } = Array.Empty<string>();
    }
}
