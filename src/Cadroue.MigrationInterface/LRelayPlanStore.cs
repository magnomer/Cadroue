using System.IO;
using System.Text.Json;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.MigrationInterface;

public static class LRelayPlanStore
{
    private const string LRelayPlanFolder = "relayplans";
    private static readonly object lRelayPlanGate = new();
    private static readonly JsonSerializerOptions lRelayPlanJson = new() { WriteIndented = true };

    public static bool LRelayPlanRead(Guid lRelayPlanId, out LRelayPlanRecord lRelayPlan)
    {
        lock (lRelayPlanGate)
        {
            try
            {
                string lRelayPath = LRelayPathRead(lRelayPlanId);
                LRelayPlanRecord? lRelayRead = File.Exists(lRelayPath)
                    ? JsonSerializer.Deserialize<LRelayPlanRecord>(File.ReadAllText(lRelayPath), lRelayPlanJson)
                    : null;
                if (lRelayRead is null)
                {
                    lRelayPlan = new LRelayPlanRecord();
                    return false;
                }

                lRelayRead.LRelayStages ??= new();
                lRelayRead.LRelayDeliveredWork ??= new();
                foreach (LRelayStageRecord lRelayStage in lRelayRead.LRelayStages)
                {
                    lRelayStage.LRelayLayout ??= new();
                    lRelayStage.LRelayExport ??= new();
                    lRelayStage.LRelayFunnelRules ??= new();
                    lRelayStage.LRelayPendingInputs ??= new();
                }

                lRelayPlan = lRelayRead;
                return true;
            }
            catch (Exception lRelayError) when (lRelayError is IOException or UnauthorizedAccessException or JsonException)
            {
                LTraceLog.LTraceWarningRecord($"Relay plan {lRelayPlanId:N} could not be read: {lRelayError.Message}");
                lRelayPlan = new LRelayPlanRecord();
                return false;
            }
        }
    }

    public static bool LRelayPlanSave(LRelayPlanRecord lRelayPlan)
    {
        lock (lRelayPlanGate)
        {
            string lRelayPath = LRelayPathRead(lRelayPlan.LRelayPlanId);
            string lRelayTemporary = lRelayPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(lRelayPath)!);
                File.WriteAllText(lRelayTemporary, JsonSerializer.Serialize(lRelayPlan, lRelayPlanJson));
                File.Move(lRelayTemporary, lRelayPath, true);
                return true;
            }
            catch (Exception lRelayError) when (lRelayError is IOException or UnauthorizedAccessException)
            {
                try
                {
                    if (File.Exists(lRelayTemporary)) File.Delete(lRelayTemporary);
                }
                catch (Exception) { }
                LTraceLog.LTraceWarningRecord($"Relay plan {lRelayPlan.LRelayPlanId:N} could not be saved: {lRelayError.Message}");
                return false;
            }
        }
    }

    private static string LRelayPathRead(Guid lRelayPlanId) =>
        Path.Combine(LDepot.LDepotRootRead(), LRelayPlanFolder, $"{lRelayPlanId:N}.json");
}
