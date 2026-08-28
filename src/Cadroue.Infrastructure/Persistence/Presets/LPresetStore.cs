using System.IO;
using System.Reflection;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LPresetStore
{
    private const string LPresetFolderName = "Cadroue";
    private const string LPresetFileName = "LExportSpecificPresets.json";
    private const string LPresetResourcePrefix = "presets/";

    public static IReadOnlyList<LPresetGroup> LPresetNativeLoad()
    {
        Assembly lAssembly = typeof(LPresetStore).Assembly;
        IEnumerable<string> lResourceNames = lAssembly.GetManifestResourceNames()
            .Where(lName => lName.StartsWith(LPresetResourcePrefix, StringComparison.Ordinal)
                && lName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

        return LPresetGroupsLoad(
            lResourceNames,
            lResourceName => lResourceName[LPresetResourcePrefix.Length..],
            lResourceName =>
            {
                using Stream lStream = lAssembly.GetManifestResourceStream(lResourceName)
                    ?? throw new InvalidDataException($"Native preset resource is unavailable: {lResourceName}");
                return JsonSerializer.Deserialize<LPresetRecord>(lStream);
            });
    }

    public static IReadOnlyList<LPresetGroup> LPresetNativeLoad(string lPresetFolderPath)
    {
        IEnumerable<string> lFilePaths = Directory.EnumerateFiles(
            lPresetFolderPath,
            "*.json",
            SearchOption.AllDirectories);
        return LPresetGroupsLoad(
            lFilePaths,
            lFilePath => Path.GetRelativePath(lPresetFolderPath, lFilePath).Replace('\\', '/'),
            LPresetFileLoad);
    }

    private static IReadOnlyList<LPresetGroup> LPresetGroupsLoad(
        IEnumerable<string> lSources,
        Func<string, string> lRelativeRead,
        Func<string, LPresetRecord?> lPresetRead)
    {
        var lGroups = new SortedDictionary<string, List<(string Path, LPresetRecord Record)>>(StringComparer.OrdinalIgnoreCase);
        foreach (string lSource in lSources)
        {
            string lRelativePath = lRelativeRead(lSource).Replace('\\', '/');
            int lSeparatorIndex = lRelativePath.IndexOf('/');
            if (lSeparatorIndex <= 0)
            {
                continue;
            }

            string lGroupName = lRelativePath[..lSeparatorIndex];
            LPresetRecord lRecord = lPresetRead(lSource)
                ?? throw new InvalidDataException($"Native preset is invalid: {lSource}");
            if (!lGroups.TryGetValue(lGroupName, out List<(string Path, LPresetRecord Record)>? lGroupRecords))
            {
                lGroupRecords = [];
                lGroups.Add(lGroupName, lGroupRecords);
            }

            lGroupRecords.Add((lRelativePath, lRecord));
        }

        return lGroups
            .Select(lGroup => new LPresetGroup(
                lGroup.Key,
                lGroup.Value
                    .OrderBy(lEntry => lEntry.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(lEntry => lEntry.Record)
                    .ToArray()))
            .ToArray();
    }

    public static IReadOnlyList<LPresetRecord>? LPresetLoad()
    {
        string lPresetPath = LPresetPathCreate();
        if (!File.Exists(lPresetPath))
        {
            return null;
        }

        try
        {
            string lPresetJson = File.ReadAllText(lPresetPath);
            return JsonSerializer.Deserialize<List<LPresetRecord>>(lPresetJson);
        }
        catch
        {
            return null;
        }
    }

    public static void LPresetSave(IReadOnlyList<LPresetRecord> lRecords)
    {
        string lPresetPath = LPresetPathCreate();
        string? lPresetFolder = Path.GetDirectoryName(lPresetPath);
        if (!string.IsNullOrWhiteSpace(lPresetFolder))
        {
            Directory.CreateDirectory(lPresetFolder);
        }

        string lPresetJson = JsonSerializer.Serialize(lRecords, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPresetPath, lPresetJson);
    }

    public static void LPresetFileSave(LPresetRecord lRecord, string lPresetFilePath)
    {
        string lPresetJson = JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPresetFilePath, lPresetJson);
    }

    public static LPresetRecord? LPresetFileLoad(string lPresetFilePath)
    {
        string lPresetJson = File.ReadAllText(lPresetFilePath);
        return JsonSerializer.Deserialize<LPresetRecord>(lPresetJson);
    }

    private static string LPresetPathCreate()
    {
        string lAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lAppData, LPresetFolderName, LPresetFileName);
    }
}
