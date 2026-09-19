using System.Text.Json;
using System.Text.Json.Serialization;
using Cadroue.Application;

namespace Cadroue.Infrastructure;

public enum LSectionImportResult
{
    LSectionImportCopied,
    LSectionImportReserved,
    LSectionImportFailed
}

public sealed record LSectionPaletteFile(
    string LSectionPaletteName,
    string[] LSectionPaletteColors,
    string LSectionPalettePath);

public sealed class LSectionPaletteRecord
{
    [JsonPropertyName("Name")]
    public string LSectionPaletteName { get; set; } = string.Empty;

    [JsonPropertyName("Colors")]
    public string[] LSectionPaletteColors { get; set; } = Array.Empty<string>();
}

public sealed record LSectionEntry(string LSectionEntryName, string[] LSectionEntryColors, string? LSectionEntryPath);

public static class LSectionPalette
{
    public const string LSectionPaletteDefault = "Cadroue";

    private const string LSectionHiddenFile = ".hidden.json";

    private static readonly LSectionEntry[] lSectionNative =
    [
        new("Cadroue", [
            "#4A90D9", "#27AE60", "#E67E22", "#8E44AD", "#E74C3C",
            "#16A085", "#F1C40F", "#D6499A", "#34495E", "#7F8C8D"
        ], null),
        new("Muted", [
            "#5B7C99", "#6A9C78", "#C08552", "#8E7CA6", "#C0736B",
            "#5F9EA0", "#B5A25D", "#A9788F", "#4F5D6B", "#8C8579"
        ], null),
        new("Vivid", [
            "#2979FF", "#00C853", "#FF9100", "#AA00FF", "#FF1744",
            "#00BFA5", "#FFD600", "#F50057", "#00B0FF", "#64DD17"
        ], null),
        new("Contrast", [
            "#332288", "#88CCEE", "#44AA99", "#117733", "#999933",
            "#DDCC77", "#CC6677", "#882255", "#AA4499", "#DDDDDD"
        ], null),
        new("Petroff", [
            "#707480", "#FFA90D", "#832DB5", "#E76300", "#92DADD",
            "#A96B59", "#3F90DA", "#B8AB6F", "#BC1E00", "#94A4A2"
        ], null),
        new("Observable", [
            "#9498A0", "#EFB118", "#A463F2", "#3CA951", "#FF8AB7",
            "#6CC5B0", "#FF725C", "#4269D0", "#9C6B4E", "#97BBF5"
        ], null),
        new("Tableau", [
            "#9C755F", "#76B7B2", "#E15759", "#EDC948", "#B07AA1",
            "#F28E2B", "#4E79A7", "#FF9DA7", "#59A14F", "#BAB0AC"
        ], null)
    ];

    private static readonly List<LSectionEntry> lSectionLoaded = [];

    private static readonly HashSet<string> lSectionHidden = new(StringComparer.Ordinal);

    public static int LSectionActiveCount => LSectionActiveRead().LSectionEntryColors.Length;

    public static IReadOnlyList<string> LSectionNamesRead() =>
        LSectionEntriesRead().Select(lEntry => lEntry.LSectionEntryName).ToArray();


    public static bool LSectionNativeCheck(string lName) =>
        lSectionNative.Any(lEntry => string.Equals(lEntry.LSectionEntryName, lName, StringComparison.Ordinal));

    public static string? LSectionPathFind(string lName) =>
        lSectionLoaded.FirstOrDefault(lEntry => lEntry.LSectionEntryName == lName)?.LSectionEntryPath;

    public static string? LSectionNameFind(string lPath) =>
        lSectionLoaded
            .FirstOrDefault(lEntry =>
                string.Equals(lEntry.LSectionEntryPath, lPath, StringComparison.OrdinalIgnoreCase))
            ?.LSectionEntryName;

    public static string[] LSectionHexRead(string lName) =>
        LSectionEntryFind(lName)?.LSectionEntryColors.ToArray() ?? [];

    public static string LSectionColorRead(int lColorIndex)
    {
        string[] lColors = LSectionActiveRead().LSectionEntryColors;
        return lColors[((lColorIndex % lColors.Length) + lColors.Length) % lColors.Length];
    }

    public static IReadOnlyList<string> LSectionColorsRead(string lName) =>
        (LSectionEntryFind(lName) ?? lSectionNative[0]).LSectionEntryColors;

    public static void LSectionRegistryLoad()
    {
        string lFolder = LDepot.LDepotPaletteRead();
        lSectionHidden.Clear();
        foreach (string lName in LSectionHiddenLoad(lFolder))
        {
            if (!string.Equals(lName, LSectionPaletteDefault, StringComparison.Ordinal))
            {
                lSectionHidden.Add(lName);
            }
        }

        lSectionLoaded.Clear();
        foreach (LSectionPaletteFile lFile in LSectionPaletteLoad(lFolder))
        {
            string[] lColors = lFile.LSectionPaletteColors
                .Select(LSectionHexNormalize)
                .OfType<string>()
                .ToArray();
            if (lColors.Length == 0)
            {
                continue;
            }

            lSectionLoaded.Add(new LSectionEntry(
                LSectionSpareResolve(lFile.LSectionPaletteName), lColors, lFile.LSectionPalettePath));
        }
    }

    public static string? LSectionHexNormalize(string lHex)
    {
        string lText = lHex.Trim();
        if (lText.Length is not (7 or 9) || lText[0] != '#' || !lText[1..].All(Uri.IsHexDigit))
        {
            return null;
        }

        return $"#{lText[^6..].ToUpperInvariant()}";
    }

    private static LSectionEntry LSectionActiveRead() =>
        LSectionEntryFind(LPreference.LPreferenceStateCurrent.LPreferenceSectionPalette) ?? lSectionNative[0];

    private static LSectionEntry? LSectionEntryFind(string lName) =>
        LSectionEntriesRead()
            .FirstOrDefault(lEntry => string.Equals(lEntry.LSectionEntryName, lName, StringComparison.Ordinal));

    private static IEnumerable<LSectionEntry> LSectionEntriesRead() =>
        lSectionNative
            .Where(lEntry => !lSectionHidden.Contains(lEntry.LSectionEntryName))
            .Concat(lSectionLoaded);

    private static string LSectionSpareResolve(string lName)
    {
        string lCandidate = lName;
        int lSuffix = 2;
        while (lSectionNative.Any(lEntry => lEntry.LSectionEntryName == lCandidate)
            || lSectionLoaded.Any(lEntry => lEntry.LSectionEntryName == lCandidate))
        {
            lCandidate = $"{lName} {lSuffix++}";
        }

        return lCandidate;
    }

    public static IReadOnlyList<LSectionPaletteFile> LSectionPaletteLoad(string lSectionFolder)
    {
        var lSectionLoaded = new List<LSectionPaletteFile>();
        foreach (string lSectionPath in LSectionFilesRead(lSectionFolder))
        {
            if (LSectionPaletteRead(lSectionPath) is { } lSectionPalette)
            {
                lSectionLoaded.Add(lSectionPalette);
            }
        }

        return lSectionLoaded;
    }

    public static LSectionPaletteFile? LSectionPaletteRead(string lSectionPath)
    {
        try
        {
            LSectionPaletteRecord? lSectionRecord = JsonSerializer.Deserialize<LSectionPaletteRecord>(
                File.ReadAllText(lSectionPath));
            if (lSectionRecord is null
                || string.IsNullOrWhiteSpace(lSectionRecord.LSectionPaletteName)
                || lSectionRecord.LSectionPaletteColors.Length == 0)
            {
                return null;
            }

            return new LSectionPaletteFile(
                lSectionRecord.LSectionPaletteName.Trim(),
                lSectionRecord.LSectionPaletteColors,
                lSectionPath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static bool LSectionPaletteSave(string lSectionPath, string lSectionName, string[] lSectionColors) =>
        LVault.LVaultSave(
            lSectionPath,
            new LSectionPaletteRecord { LSectionPaletteName = lSectionName, LSectionPaletteColors = lSectionColors });

    public static bool LSectionPaletteDelete(string lSectionPath)
    {
        try
        {
            File.Delete(lSectionPath);
            return true;
        }
        catch (Exception lSectionException) when (lSectionException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static LSectionImportResult LSectionPaletteImport(
        string lSectionFolder,
        string lSectionSourcePath,
        string lSectionName,
        out string lSectionTargetPath)
    {
        string lSectionFileName = LSectionFileResolve(lSectionName);
        lSectionTargetPath = string.Empty;
        if (lSectionFileName.StartsWith('.'))
        {
            return LSectionImportResult.LSectionImportReserved;
        }

        string lSectionSourceFull = Path.GetFullPath(lSectionSourcePath);
        lSectionTargetPath = Path.Combine(lSectionFolder, lSectionFileName);

        bool lSectionInside = string.Equals(
            Path.GetDirectoryName(lSectionSourceFull),
            Path.GetFullPath(lSectionFolder),
            StringComparison.OrdinalIgnoreCase);
        if (lSectionInside)
        {
            lSectionTargetPath = lSectionSourceFull;
            return LSectionImportResult.LSectionImportCopied;
        }

        lSectionTargetPath = LSectionVacantResolve(lSectionTargetPath);
        try
        {
            Directory.CreateDirectory(lSectionFolder);
            File.Copy(lSectionSourceFull, lSectionTargetPath);
            return LSectionImportResult.LSectionImportCopied;
        }
        catch (Exception lSectionException) when (lSectionException is IOException or UnauthorizedAccessException)
        {
            return LSectionImportResult.LSectionImportFailed;
        }
    }

    public static IReadOnlyList<string> LSectionHiddenLoad(string lSectionFolder)
    {
        try
        {
            string lSectionHiddenPath = Path.Combine(lSectionFolder, LSectionHiddenFile);
            if (!File.Exists(lSectionHiddenPath))
            {
                return Array.Empty<string>();
            }

            return JsonSerializer.Deserialize<string[]>(File.ReadAllText(lSectionHiddenPath)) ?? Array.Empty<string>();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    public static bool LSectionHiddenSave(string lSectionFolder, IReadOnlyList<string> lSectionHidden) =>
        LVault.LVaultSave(Path.Combine(lSectionFolder, LSectionHiddenFile), lSectionHidden.ToArray());

    private static IEnumerable<string> LSectionFilesRead(string lSectionFolder)
    {
        try
        {
            return Directory.EnumerateFiles(lSectionFolder, "*.json")
                .Where(lSectionPath => !string.Equals(
                    Path.GetFileName(lSectionPath),
                    LSectionHiddenFile,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(lSectionPath => lSectionPath)
                .ToArray();
        }
        catch (Exception lSectionException) when (lSectionException is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    private static string LSectionFileResolve(string lSectionName)
    {
        string lSectionSafe = new(lSectionName
            .Select(lSectionCharacter =>
                Path.GetInvalidFileNameChars().Contains(lSectionCharacter) ? '_' : lSectionCharacter)
            .ToArray());
        return $"{lSectionSafe}.json";
    }

    private static string LSectionVacantResolve(string lSectionTargetPath)
    {
        string lSectionFolder = Path.GetDirectoryName(lSectionTargetPath) ?? string.Empty;
        string lSectionStem = Path.GetFileNameWithoutExtension(lSectionTargetPath);
        string lSectionExtension = Path.GetExtension(lSectionTargetPath);
        string lSectionCandidate = lSectionTargetPath;
        int lSectionSuffix = 2;
        while (File.Exists(lSectionCandidate))
        {
            lSectionCandidate = Path.Combine(lSectionFolder, $"{lSectionStem} {lSectionSuffix++}{lSectionExtension}");
        }

        return lSectionCandidate;
    }
}
