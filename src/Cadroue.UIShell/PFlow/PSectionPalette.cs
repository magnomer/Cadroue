using Cadroue.Core;
using Cadroue.Application;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Cadroue.UIShell.PFlow;

internal static class PSectionPalette
{
    private sealed record PSectionSeed(string PSectionSeedName, string[] PSectionSeedHex);

    internal enum PSectionImportResult
    {
        PSectionImportLoaded,
        PSectionImportInvalid,
        PSectionImportReserved,
        PSectionImportFailed
    }

    private sealed record PSectionSwatch(
        string PSectionSwatchName,
        Color[] PSectionSwatchColors,
        string? PSectionSwatchPath);

    internal const string PSectionPaletteDefault = "Cadroue";

    private const string PSectionHiddenFile = ".hidden.json";

    private const byte PSectionBandAlpha = 0x99;

    private static readonly PSectionSeed[] pSectionNativeHex =
    {
        new("Cadroue", new[]
        {
            "#4A90D9", "#27AE60", "#E67E22", "#8E44AD", "#E74C3C",
            "#16A085", "#F1C40F", "#D6499A", "#34495E", "#7F8C8D"
        }),
        new("Muted", new[]
        {
            "#5B7C99", "#6A9C78", "#C08552", "#8E7CA6", "#C0736B",
            "#5F9EA0", "#B5A25D", "#A9788F", "#4F5D6B", "#8C8579"
        }),
        new("Vivid", new[]
        {
            "#2979FF", "#00C853", "#FF9100", "#AA00FF", "#FF1744",
            "#00BFA5", "#FFD600", "#F50057", "#00B0FF", "#64DD17"
        }),
        new("Contrast", new[]
        {
            "#332288", "#88CCEE", "#44AA99", "#117733", "#999933",
            "#DDCC77", "#CC6677", "#882255", "#AA4499", "#DDDDDD"
        }),
        new("Petroff", new[]
        {
            "#707480", "#FFA90D", "#832DB5", "#E76300", "#92DADD",
            "#A96B59", "#3F90DA", "#B8AB6F", "#BC1E00", "#94A4A2"
        }),
        new("Observable", new[]
        {
            "#9498A0", "#EFB118", "#A463F2", "#3CA951", "#FF8AB7",
            "#6CC5B0", "#FF725C", "#4269D0", "#9C6B4E", "#97BBF5"
        }),
        new("Tableau", new[]
        {
            "#9C755F", "#76B7B2", "#E15759", "#EDC948", "#B07AA1",
            "#F28E2B", "#4E79A7", "#FF9DA7", "#59A14F", "#BAB0AC"
        })
    };

    private static readonly PSectionSwatch[] pSectionPalettes = pSectionNativeHex
        .Select(pNative => new PSectionSwatch(
            pNative.PSectionSeedName,
            pNative.PSectionSeedHex.Select(pHex => PSectionHexParse(pHex) ?? Colors.Gray).ToArray(),
            null))
        .ToArray();

    private static readonly List<PSectionSwatch> pSectionLoaded = new();

    private static readonly HashSet<string> pSectionHidden = new(StringComparer.Ordinal);

    private static Dictionary<string, Brush[]> pSectionBandBrushes = PSectionBrushesCreate(PSectionBandAlpha);
    private static Dictionary<string, Brush[]> pSectionBadgeBrushes = PSectionBrushesCreate(0xFF);

    private static IEnumerable<PSectionSwatch> PSectionAllRead() =>
        pSectionPalettes
            .Where(pPalette => !pSectionHidden.Contains(pPalette.PSectionSwatchName))
            .Concat(pSectionLoaded);

    internal static bool PSectionNativeCheck(string pName) =>
        pSectionPalettes.Any(pPalette => string.Equals(pPalette.PSectionSwatchName, pName, StringComparison.Ordinal));

    internal static bool PSectionFixedCheck(string pName) =>
        string.Equals(pName, PSectionPaletteDefault, StringComparison.Ordinal);

    private static Dictionary<string, Brush[]> PSectionBrushesCreate(byte pSectionAlpha)
    {
        var pSectionSets = new Dictionary<string, Brush[]>(StringComparer.Ordinal);
        foreach (PSectionSwatch pSwatch in PSectionAllRead())
        {
            Color[] pColors = pSwatch.PSectionSwatchColors;
            var pSectionBrushes = new Brush[pColors.Length];
            for (int pSectionIndex = 0; pSectionIndex < pColors.Length; pSectionIndex++)
            {
                Color pSectionColor = pColors[pSectionIndex];
                var pSectionBrush = new SolidColorBrush(
                    Color.FromArgb(pSectionAlpha, pSectionColor.R, pSectionColor.G, pSectionColor.B));
                pSectionBrush.Freeze();
                pSectionBrushes[pSectionIndex] = pSectionBrush;
            }

            pSectionSets[pSwatch.PSectionSwatchName] = pSectionBrushes;
        }

        return pSectionSets;
    }

    internal static IReadOnlyList<string> PSectionPaletteNames =>
        PSectionAllRead().Select(pPalette => pPalette.PSectionSwatchName).ToArray();

    internal static bool PSectionPaletteCheck(string pName) =>
        PSectionAllRead().Any(pPalette => string.Equals(pPalette.PSectionSwatchName, pName, StringComparison.Ordinal));

    internal static int PSectionActiveCount => PSectionSetRead(pSectionBadgeBrushes).Length;

    internal static IReadOnlyList<Brush> PSectionBadgesRead(string pName) =>
        pSectionBadgeBrushes.TryGetValue(pName, out Brush[]? pBrushes)
            ? pBrushes
            : pSectionBadgeBrushes[PSectionPaletteDefault];

    internal static Brush PSectionPaletteRead(int pColorIndex) =>
        PSectionBrushRead(pSectionBandBrushes, pColorIndex);

    internal static Brush PSectionBadgeRead(int pColorIndex) =>
        PSectionBrushRead(pSectionBadgeBrushes, pColorIndex);

    private static Brush PSectionBrushRead(Dictionary<string, Brush[]> pSectionSets, int pColorIndex)
    {
        Brush[] pSet = PSectionSetRead(pSectionSets);
        return pSet[((pColorIndex % pSet.Length) + pSet.Length) % pSet.Length];
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

            string pName = pPalette.PSectionSwatchName;
            int pSuffix = 2;
            while (pSectionPalettes.Any(pBuiltIn => pBuiltIn.PSectionSwatchName == pName)
                || pSectionLoaded.Any(pOther => pOther.PSectionSwatchName == pName))
            {
                pName = $"{pPalette.PSectionSwatchName} {pSuffix++}";
            }

            pSectionLoaded.Add(pPalette with { PSectionSwatchName = pName });
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

        if (pSectionLoaded.FirstOrDefault(pLoaded => pLoaded.PSectionSwatchName == pName)
            ?.PSectionSwatchPath
            is not { } pEntryPath)
        {
            return false;
        }

        try
        {
            File.Delete(pEntryPath);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        PSectionPaletteLoad();
        return true;
    }

    internal static PSectionImportResult PSectionPaletteImport(string pSourcePath, out string? pLoadedName)
    {
        pLoadedName = null;
        if (PSectionFileRead(pSourcePath) is not { } pPalette)
        {
            return PSectionImportResult.PSectionImportInvalid;
        }

        string pFileName = PSectionFileCreate(pPalette.PSectionSwatchName);
        if (pFileName.StartsWith('.'))
        {
            return PSectionImportResult.PSectionImportReserved;
        }

        string pPaletteFolder = Cadroue.Infrastructure.LDepot.LDepotPaletteRead();
        string pSourceFull = Path.GetFullPath(pSourcePath);
        string pTargetPath = Path.Combine(pPaletteFolder, pFileName);
        bool pInside = string.Equals(
            Path.GetDirectoryName(pSourceFull),
            Path.GetFullPath(pPaletteFolder),
            StringComparison.OrdinalIgnoreCase);
        if (pInside)
        {
            pTargetPath = pSourceFull;
        }
        else
        {
            pTargetPath = PSectionVacantResolve(pTargetPath);
            try
            {
                Directory.CreateDirectory(pPaletteFolder);
                File.Copy(pSourceFull, pTargetPath);
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
                return PSectionImportResult.PSectionImportFailed;
            }
        }

        PSectionPaletteLoad();
        pLoadedName = pSectionLoaded
            .FirstOrDefault(pEntry => string.Equals(pEntry.PSectionSwatchPath, pTargetPath, StringComparison.OrdinalIgnoreCase))
            ?.PSectionSwatchName;
        return pLoadedName is null
            ? PSectionImportResult.PSectionImportInvalid
            : PSectionImportResult.PSectionImportLoaded;
    }

    private static string PSectionVacantResolve(string pTargetPath)
    {
        string pFolder = Path.GetDirectoryName(pTargetPath) ?? string.Empty;
        string pStem = Path.GetFileNameWithoutExtension(pTargetPath);
        string pExtension = Path.GetExtension(pTargetPath);
        string pCandidate = pTargetPath;
        int pSuffix = 2;
        while (File.Exists(pCandidate))
        {
            pCandidate = Path.Combine(pFolder, $"{pStem} {pSuffix++}{pExtension}");
        }

        return pCandidate;
    }

    internal static void PSectionPaletteSave(string pName, string pTargetPath)
    {
        if (PSectionAllRead().FirstOrDefault(pEntry => pEntry.PSectionSwatchName == pName) is not { } pPalette)
        {
            return;
        }

        var pRecord = new PSectionPaletteRecord
        {
            PSectionPaletteName = pPalette.PSectionSwatchName,
            PSectionPaletteColors = pPalette.PSectionSwatchColors.Select(PSectionHexFormat).ToArray()
        };
        File.WriteAllText(
            pTargetPath,
            JsonSerializer.Serialize(pRecord, new JsonSerializerOptions { WriteIndented = true }));
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

            foreach (string pName in JsonSerializer.Deserialize<string[]>(File.ReadAllText(pHiddenPath))
                ?? Array.Empty<string>())
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
                .Where(pPath => !string.Equals(
                    Path.GetFileName(pPath),
                    PSectionHiddenFile,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(pPath => pPath);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    private static PSectionSwatch? PSectionFileRead(string pFilePath)
    {
        try
        {
            PSectionPaletteRecord? pRecord = JsonSerializer.Deserialize<PSectionPaletteRecord>(
                File.ReadAllText(pFilePath));
            if (pRecord is null
                || string.IsNullOrWhiteSpace(pRecord.PSectionPaletteName)
                || pRecord.PSectionPaletteColors.Length == 0)
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

            return pColors.Count == 0
                ? null
                : new PSectionSwatch(pRecord.PSectionPaletteName.Trim(), pColors.ToArray(), pFilePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string PSectionFileCreate(string pName)
    {
        string pSafe = new(pName
            .Select(pCharacter => Path.GetInvalidFileNameChars().Contains(pCharacter) ? '_' : pCharacter)
            .ToArray());
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
