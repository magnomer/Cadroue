using System.Text.Json;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LCartographerPlanStore
{
    private const string LCartographerPlanFolder = "relayplans";
    private static readonly JsonSerializerOptions lCartographerPlanJson = new() { WriteIndented = true };
    private static readonly Dictionary<Guid, LCartographerPlanEntry> lCartographerPlanCache = new();

    private sealed record LCartographerPlanEntry(
        DateTime LCartographerPlanStamp,
        long LCartographerPlanLength,
        LCartographerPlanDocument LCartographerPlanParsed);

    public static bool LCartographerPlanRead(Guid lCartographerPlanId, out LCartographerPlanDocument lCartographerPlan)
    {
        string lCartographerPath = LCartographerPathRead(lCartographerPlanId);
        lCartographerPlan = new LCartographerPlanDocument();
        try
        {
            var lCartographerFile = new FileInfo(lCartographerPath);
            if (!lCartographerFile.Exists)
            {
                LCartographerCacheRemove(lCartographerPlanId);
                return false;
            }

            if (LCartographerCacheRead(lCartographerPlanId, lCartographerFile) is { } lCartographerCached)
            {
                lCartographerPlan = lCartographerCached.LCartographerPlanParsed;
                return true;
            }

            using (LLatch.LLatchClaim(lCartographerPath))
            {
                string lCartographerJson = File.ReadAllText(lCartographerPath);
                if (!LCartographerTextParse(lCartographerJson, out lCartographerPlan))
                {
                    return false;
                }

                LCartographerCacheSet(lCartographerPlanId, lCartographerPath, lCartographerPlan);
                return true;
            }
        }
        catch (Exception lCartographerError) when (lCartographerError
            is IOException
            or UnauthorizedAccessException
            or JsonException
            or TimeoutException)
        {
            LTraceEngine.LTraceWarningSeam(
                $"Relay plan {lCartographerPlanId:N} could not be read: {lCartographerError.Message}");
            return false;
        }
    }

    public static bool LCartographerPlanSave(LCartographerPlanDocument lCartographerPlan)
    {
        Guid lCartographerPlanId = lCartographerPlan.LCartographerPlanId;
        string lCartographerPath = LCartographerPathRead(lCartographerPlanId);
        string lCartographerTemporary = lCartographerPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (LLatch.LLatchClaim(lCartographerPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(lCartographerPath)!);
                string lCartographerJson = JsonSerializer.Serialize(lCartographerPlan, lCartographerPlanJson);
                File.WriteAllText(lCartographerTemporary, lCartographerJson);
                File.Move(lCartographerTemporary, lCartographerPath, true);
                LCartographerCacheSet(lCartographerPlanId, lCartographerPath, lCartographerPlan);
                return true;
            }
        }
        catch (Exception lCartographerError) when (lCartographerError
            is IOException
            or UnauthorizedAccessException
            or TimeoutException)
        {
            LCartographerCacheRemove(lCartographerPlanId);
            try
            {
                if (File.Exists(lCartographerTemporary)) File.Delete(lCartographerTemporary);
            }
            catch (Exception) { }
            LTraceEngine.LTraceWarningSeam(
                $"Relay plan {lCartographerPlanId:N} could not be saved: " +
                $"{lCartographerError.Message}");
            return false;
        }
    }

    public static void LCartographerPlanDelete(Guid lCartographerPlanId)
    {
        string lCartographerPath = LCartographerPathRead(lCartographerPlanId);
        LCartographerCacheRemove(lCartographerPlanId);
        try
        {
            using (LLatch.LLatchClaim(lCartographerPath))
            {
                if (File.Exists(lCartographerPath))
                {
                    File.Delete(lCartographerPath);
                }
            }
        }
        catch (Exception lCartographerError) when (lCartographerError
            is IOException
            or UnauthorizedAccessException
            or TimeoutException)
        {
            LTraceEngine.LTraceWarningSeam(
                $"Relay plan {lCartographerPlanId:N} could not be deleted: {lCartographerError.Message}");
        }
    }

    private static LCartographerPlanEntry? LCartographerCacheRead(
        Guid lCartographerPlanId, FileInfo lCartographerFile)
    {
        lock (lCartographerPlanCache)
        {
            return lCartographerPlanCache.TryGetValue(
                    lCartographerPlanId, out LCartographerPlanEntry? lCartographerEntry)
                && lCartographerEntry.LCartographerPlanStamp == lCartographerFile.LastWriteTimeUtc
                && lCartographerEntry.LCartographerPlanLength == lCartographerFile.Length
                    ? lCartographerEntry
                    : null;
        }
    }

    private static void LCartographerCacheSet(
        Guid lCartographerPlanId, string lCartographerPath, LCartographerPlanDocument lCartographerPlan)
    {
        var lCartographerFile = new FileInfo(lCartographerPath);
        lock (lCartographerPlanCache)
        {
            lCartographerPlanCache[lCartographerPlanId] = new LCartographerPlanEntry(
                lCartographerFile.LastWriteTimeUtc, lCartographerFile.Length, lCartographerPlan);
        }
    }

    private static void LCartographerCacheRemove(Guid lCartographerPlanId)
    {
        lock (lCartographerPlanCache)
        {
            lCartographerPlanCache.Remove(lCartographerPlanId);
        }
    }

    private static bool LCartographerTextParse(
        string lCartographerJson, out LCartographerPlanDocument lCartographerPlan)
    {
        LCartographerPlanDocument? lCartographerRead =
            JsonSerializer.Deserialize<LCartographerPlanDocument>(lCartographerJson, lCartographerPlanJson);
        if (lCartographerRead is null)
        {
            lCartographerPlan = new LCartographerPlanDocument();
            return false;
        }

        lCartographerRead.LCartographerStages ??= new();
        lCartographerRead.LCartographerDeliveredWork ??= new();
        foreach (LCartographerStageEntry lCartographerStage in lCartographerRead.LCartographerStages)
        {
            lCartographerStage.LCartographerLayout ??= new();
            lCartographerStage.LCartographerExport ??= new();
            lCartographerStage.LCartographerFunnelRules ??= new();
            lCartographerStage.LCartographerPendingInputs ??= new();
        }

        lCartographerPlan = lCartographerRead;
        return true;
    }

    private static string LCartographerPathRead(Guid lCartographerPlanId) =>
        Path.Combine(LDepotEngine.LDepotRootSeam(), LCartographerPlanFolder, $"{lCartographerPlanId:N}.json");
}
