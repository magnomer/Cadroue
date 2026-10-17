using System.IO;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LPresetStore
{
    private const string LPresetFileName = "LExportSpecificPresets.json";

    public static LPresetCatalog LPresetLoad()
    {
        string lPresetPath = LPresetPathCreate();
        try
        {
            using LLatchScope lPresetLatch = LLatch.LLatchClaim(lPresetPath);
            return LPresetCatalogRead(lPresetPath);
        }
        catch (Exception lPresetException) when (
            lPresetException is TimeoutException
                or IOException
                or UnauthorizedAccessException)
        {
            return new LPresetCatalog(LPresetOutcome.LPresetUnreadable, []);
        }
    }

    public static bool LPresetSave(Func<LPresetCatalog, IReadOnlyList<LPresetDocument>?> lPresetResolve)
    {
        string lPresetPath = LPresetPathCreate();
        try
        {
            using LLatchScope lPresetLatch = LLatch.LLatchClaim(lPresetPath);
            LPresetCatalog lPresetCatalog = LPresetCatalogRead(lPresetPath);
            return lPresetResolve(lPresetCatalog) is { } lPresetRecords
                && LVault.LVaultSave(lPresetPath, lPresetRecords);
        }
        catch (Exception lPresetException) when (
            lPresetException is TimeoutException
                or IOException
                or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static LPresetCatalog LPresetCatalogRead(string lPresetPath)
    {
        LVaultResult<List<LPresetDocument>> lPresetResult = LVault.LVaultRead<List<LPresetDocument>>(lPresetPath);
        if (lPresetResult is { LVaultOutcome: LVaultOutcome.LVaultLoaded, LVaultValue: { } lPresetRecords })
        {
            return new LPresetCatalog(LPresetOutcome.LPresetLoaded, lPresetRecords.OfType<LPresetDocument>().ToArray());
        }

        bool lPresetDamaged = lPresetResult.LVaultOutcome == LVaultOutcome.LVaultUnreadable
            && File.Exists(lPresetPath);
        return new LPresetCatalog(
            lPresetDamaged ? LPresetOutcome.LPresetUnreadable : LPresetOutcome.LPresetMissing,
            []);
    }

    public static bool LPresetFileSave(LPresetDocument lRecord, string lPresetFilePath) =>
        !LPresetCatalogCheck(lPresetFilePath) && LVault.LVaultSave(lPresetFilePath, lRecord);

    public static bool LPresetCatalogCheck(string lPresetFilePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lPresetFilePath),
                Path.GetFullPath(LPresetPathCreate()),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lPathException) when (
            lPathException is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return false;
        }
    }

    public static LPresetDocument? LPresetFileLoad(string lPresetFilePath)
    {
        string lPresetJson = File.ReadAllText(lPresetFilePath);
        return JsonSerializer.Deserialize<LPresetDocument>(lPresetJson) is { } lPresetRecord
            ? LPresetRange.LPresetRecordNormalize(lPresetRecord)
            : null;
    }

    private static string LPresetPathCreate() => Path.Combine(LDepot.LDepotHomeRead(), LPresetFileName);
}
