using System.Text.Json;
using System.Text.Json.Serialization;

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

public static class LSectionPalette
{
    private const string LSectionHiddenFile = ".hidden.json";

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
