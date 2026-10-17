using System.IO;
using System.Reflection;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed record LPresetEntry(string LPresetEntryPath, LPresetDocument LPresetEntryValue);

public static class LPresetNative
{
    private const string LPresetResourcePrefix = "presets/";

    public static IReadOnlyList<LPresetGroup> LPresetNativeLoad()
    {
        Assembly lAssembly = typeof(LPresetNative).Assembly;
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
                return JsonSerializer.Deserialize<LPresetDocument>(lStream);
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
            LPresetStore.LPresetFileLoad);
    }

    private static IReadOnlyList<LPresetGroup> LPresetGroupsLoad(
        IEnumerable<string> lSources,
        Func<string, string> lRelativeRead,
        Func<string, LPresetDocument?> lPresetRead)
    {
        var lGroups = new SortedDictionary<string, List<LPresetEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (string lSource in lSources)
        {
            string lRelativePath = lRelativeRead(lSource).Replace('\\', '/');
            int lSeparatorIndex = lRelativePath.IndexOf('/');
            if (lSeparatorIndex <= 0)
            {
                continue;
            }

            string lGroupName = lRelativePath[..lSeparatorIndex];
            LPresetDocument lRecord = lPresetRead(lSource)
                ?? throw new InvalidDataException($"Native preset is invalid: {lSource}");
            if (!lGroups.TryGetValue(lGroupName, out List<LPresetEntry>? lGroupRecords))
            {
                lGroupRecords = [];
                lGroups.Add(lGroupName, lGroupRecords);
            }

            lGroupRecords.Add(new LPresetEntry(lRelativePath, lRecord));
        }

        return lGroups
            .Select(lGroup => new LPresetGroup(
                lGroup.Key,
                lGroup.Value
                    .OrderBy(lEntry => lEntry.LPresetEntryPath, StringComparer.OrdinalIgnoreCase)
                    .Select(lEntry => lEntry.LPresetEntryValue)
                    .ToArray()))
            .ToArray();
    }
}
