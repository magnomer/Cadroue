using System.IO;
using System.Text.Json;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.MigrationInterface;

public static class LCartographerPlanStore
{
    private const string LCartographerPlanFolder = "relayplans";
    private static readonly object lCartographerPlanGate = new();
    private static readonly JsonSerializerOptions lCartographerPlanJson = new() { WriteIndented = true };

    public static bool LCartographerPlanRead(Guid lCartographerPlanId, out LCartographerPlanRecord lCartographerPlan)
    {
        lock (lCartographerPlanGate)
        {
            try
            {
                string lCartographerPath = LCartographerPathRead(lCartographerPlanId);
                LCartographerPlanRecord? lCartographerRead = File.Exists(lCartographerPath)
                    ? JsonSerializer.Deserialize<LCartographerPlanRecord>(File.ReadAllText(lCartographerPath), lCartographerPlanJson)
                    : null;
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
            catch (Exception lCartographerError) when (lCartographerError is IOException or UnauthorizedAccessException or JsonException)
            {
                LTraceLog.LTraceWarningRecord($"Relay plan {lCartographerPlanId:N} could not be read: {lCartographerError.Message}");
                lCartographerPlan = new LCartographerPlanRecord();
                return false;
            }
        }
    }

    public static bool LCartographerPlanSave(LCartographerPlanRecord lCartographerPlan)
    {
        lock (lCartographerPlanGate)
        {
            string lCartographerPath = LCartographerPathRead(lCartographerPlan.LCartographerPlanId);
            string lCartographerTemporary = lCartographerPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(lCartographerPath)!);
                File.WriteAllText(lCartographerTemporary, JsonSerializer.Serialize(lCartographerPlan, lCartographerPlanJson));
                File.Move(lCartographerTemporary, lCartographerPath, true);
                return true;
            }
            catch (Exception lCartographerError) when (lCartographerError is IOException or UnauthorizedAccessException)
            {
                try
                {
                    if (File.Exists(lCartographerTemporary)) File.Delete(lCartographerTemporary);
                }
                catch (Exception) { }
                LTraceLog.LTraceWarningRecord($"Relay plan {lCartographerPlan.LCartographerPlanId:N} could not be saved: {lCartographerError.Message}");
                return false;
            }
        }
    }

    private static string LCartographerPathRead(Guid lCartographerPlanId) =>
        Path.Combine(LDepot.LDepotRootRead(), LCartographerPlanFolder, $"{lCartographerPlanId:N}.json");
}
