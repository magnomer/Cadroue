using System.IO;
using System.Text.Json;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static class LCartographerPlanStore
{
    private const string LCartographerPlanFolder = "relayplans";
    private static readonly JsonSerializerOptions lCartographerPlanJson = new() { WriteIndented = true };
    private static readonly Dictionary<Guid, LCartographerPlanText> lCartographerPlanCache = new();

    private sealed record LCartographerPlanText(
        DateTime LCartographerPlanStamp, long LCartographerPlanLength, string LCartographerPlanJson);

    public static bool LCartographerPlanRead(Guid lCartographerPlanId, out LCartographerPlanRecord lCartographerPlan)
    {
        string lCartographerPath = LCartographerPathRead(lCartographerPlanId);
        try
        {
            var lCartographerFile = new FileInfo(lCartographerPath);
            if (!lCartographerFile.Exists)
            {
                LCartographerTextRemove(lCartographerPlanId);
                lCartographerPlan = new LCartographerPlanRecord();
                return false;
            }

            if (LCartographerTextRead(lCartographerPlanId, lCartographerFile) is { } lCartographerCached)
            {
                return LCartographerTextParse(lCartographerCached, out lCartographerPlan);
            }

            using (LLatch.LLatchClaim(lCartographerPath))
            {
                string lCartographerJson = File.ReadAllText(lCartographerPath);
                LCartographerTextSet(lCartographerPlanId, lCartographerPath, lCartographerJson);
                return LCartographerTextParse(lCartographerJson, out lCartographerPlan);
            }
        }
        catch (Exception lCartographerError) when (lCartographerError
            is IOException
            or UnauthorizedAccessException
            or JsonException
            or TimeoutException)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay plan {lCartographerPlanId:N} could not be read: {lCartographerError.Message}");
            lCartographerPlan = new LCartographerPlanRecord();
            return false;
        }
    }

    public static bool LCartographerPlanSave(LCartographerPlanRecord lCartographerPlan)
    {
        string lCartographerPath = LCartographerPathRead(lCartographerPlan.LCartographerPlanId);
        string lCartographerTemporary = lCartographerPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (LLatch.LLatchClaim(lCartographerPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(lCartographerPath)!);
                string lCartographerJson = JsonSerializer.Serialize(lCartographerPlan, lCartographerPlanJson);
                File.WriteAllText(lCartographerTemporary, lCartographerJson);
                File.Move(lCartographerTemporary, lCartographerPath, true);
                LCartographerTextSet(lCartographerPlan.LCartographerPlanId, lCartographerPath, lCartographerJson);
                return true;
            }
        }
        catch (Exception lCartographerError) when (lCartographerError
            is IOException
            or UnauthorizedAccessException
            or TimeoutException)
        {
            try
            {
                if (File.Exists(lCartographerTemporary)) File.Delete(lCartographerTemporary);
            }
            catch (Exception) { }
            LTraceLog.LTraceWarningRecord(
                $"Relay plan {lCartographerPlan.LCartographerPlanId:N} could not be saved: " +
                $"{lCartographerError.Message}");
            return false;
        }
    }

    public static void LCartographerPlanDelete(Guid lCartographerPlanId)
    {
        string lCartographerPath = LCartographerPathRead(lCartographerPlanId);
        LCartographerTextRemove(lCartographerPlanId);
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
            LTraceLog.LTraceWarningRecord(
                $"Relay plan {lCartographerPlanId:N} could not be deleted: {lCartographerError.Message}");
        }
    }

    private static string? LCartographerTextRead(Guid lCartographerPlanId, FileInfo lCartographerFile)
    {
        lock (lCartographerPlanCache)
        {
            return lCartographerPlanCache.TryGetValue(lCartographerPlanId, out LCartographerPlanText? lCartographerText)
                && lCartographerText.LCartographerPlanStamp == lCartographerFile.LastWriteTimeUtc
                && lCartographerText.LCartographerPlanLength == lCartographerFile.Length
                    ? lCartographerText.LCartographerPlanJson
                    : null;
        }
    }

    private static void LCartographerTextSet(
        Guid lCartographerPlanId, string lCartographerPath, string lCartographerJson)
    {
        var lCartographerFile = new FileInfo(lCartographerPath);
        lock (lCartographerPlanCache)
        {
            lCartographerPlanCache[lCartographerPlanId] = new LCartographerPlanText(
                lCartographerFile.LastWriteTimeUtc, lCartographerFile.Length, lCartographerJson);
        }
    }

    private static void LCartographerTextRemove(Guid lCartographerPlanId)
    {
        lock (lCartographerPlanCache)
        {
            lCartographerPlanCache.Remove(lCartographerPlanId);
        }
    }

    private static bool LCartographerTextParse(string lCartographerJson, out LCartographerPlanRecord lCartographerPlan)
    {
        LCartographerPlanRecord? lCartographerRead =
            JsonSerializer.Deserialize<LCartographerPlanRecord>(lCartographerJson, lCartographerPlanJson);
        if (lCartographerRead is null)
        {
            lCartographerPlan = new LCartographerPlanRecord();
            return false;
        }

        lCartographerRead.LCartographerStages ??= new();
        lCartographerRead.LCartographerDeliveredWork ??= new();
        foreach (LCartographerStageRecord lCartographerStage in lCartographerRead.LCartographerStages)
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
        Path.Combine(LDepot.LDepotRootRead(), LCartographerPlanFolder, $"{lCartographerPlanId:N}.json");
}
