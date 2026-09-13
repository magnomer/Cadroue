using System.IO;
using System.Reflection;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed record LPresetEntry(string LPresetEntryPath, LPresetRecord LPresetEntryValue);

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
            LPresetRecord lRecord = lPresetRead(lSource)?.LPresetRecordNormalize()
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

    public static LPresetCatalog LPresetLoad()
    {
        string lPresetPath = LPresetPathCreate();
        try
        {
            using LLatchScope lPresetLatch = LLatch.LLatchClaim(lPresetPath);
            return LPresetCatalogRead(lPresetPath);
        }
        catch (Exception lPresetException) when (lPresetException is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return new LPresetCatalog(LPresetOutcome.LPresetUnreadable, []);
        }
    }

    public static bool LPresetSave(Func<LPresetCatalog, IReadOnlyList<LPresetRecord>?> lPresetResolve)
    {
        string lPresetPath = LPresetPathCreate();
        try
        {
            using LLatchScope lPresetLatch = LLatch.LLatchClaim(lPresetPath);
            LPresetCatalog lPresetCatalog = LPresetCatalogRead(lPresetPath);
            return lPresetResolve(lPresetCatalog) is { } lPresetRecords
                && LVault.LVaultSave(lPresetPath, lPresetRecords);
        }
        catch (Exception lPresetException) when (lPresetException is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static LPresetCatalog LPresetCatalogRead(string lPresetPath)
    {
        LVaultResult<List<LPresetRecord>> lPresetResult = LVault.LVaultRead<List<LPresetRecord>>(lPresetPath);
        if (lPresetResult is { LVaultOutcome: LVaultOutcome.LVaultLoaded, LVaultValue: { } lPresetRecords })
        {
            return new LPresetCatalog(LPresetOutcome.LPresetLoaded, LPresetRecordsNormalize(lPresetRecords));
        }

        bool lPresetDamaged = lPresetResult.LVaultOutcome == LVaultOutcome.LVaultUnreadable
            && File.Exists(lPresetPath);
        return new LPresetCatalog(
            lPresetDamaged ? LPresetOutcome.LPresetUnreadable : LPresetOutcome.LPresetMissing,
            []);
    }

    private static IReadOnlyList<LPresetRecord> LPresetRecordsNormalize(IEnumerable<LPresetRecord?> lPresetRecords) =>
        lPresetRecords
            .Where(lPresetRecord => lPresetRecord is not null)
            .Select(lPresetRecord => lPresetRecord!.LPresetRecordNormalize())
            .ToArray();

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
