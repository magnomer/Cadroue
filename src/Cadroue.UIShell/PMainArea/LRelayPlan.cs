using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

internal sealed class LRelayPlanRecord
{
    public Guid LRelayPlanId { get; set; }
    public Guid LRelayEntryStage { get; set; }
    public DateTimeOffset LRelayCreated { get; set; } = DateTimeOffset.Now;
    public List<LRelayStageRecord> LRelayStages { get; set; } = new();
    public HashSet<Guid> LRelayDeliveredWork { get; set; } = new();
}

internal sealed class LRelayStageRecord
{
    public Guid LRelayStageId { get; set; }
    public Guid LRelayOriginalTab { get; set; }
    public string LRelayLayoutKey { get; set; } = string.Empty;
    public string LRelayTitle { get; set; } = string.Empty;
    public Guid LRelayNextStage { get; set; }
    public LPresetRecord LRelayExport { get; set; } = new();
    public LSceneTabRecord LRelayLayout { get; set; } = new();
    public List<LRelayFunnelRuleRecord> LRelayFunnelRules { get; set; } = new();
    public List<LRelayInputRecord> LRelayPendingInputs { get; set; } = new();
}

internal sealed class LRelayFunnelRuleRecord
{
    public LSceneFunnelRule LRelayRule { get; set; } = new();
    public Guid LRelayTargetStage { get; set; }
}

internal sealed class LRelayInputRecord
{
    public string LRelayPath { get; set; } = string.Empty;
    public Guid LRelaySourceStage { get; set; }
}

internal static class LRelayPlanStore
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
                string lRelayPath = LRelayPlanPathRead(lRelayPlanId);
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
            string lRelayPath = LRelayPlanPathRead(lRelayPlan.LRelayPlanId);
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

    private static string LRelayPlanPathRead(Guid lRelayPlanId) =>
        Path.Combine(LDepot.LDepotRootRead(), LRelayPlanFolder, $"{lRelayPlanId:N}.json");
}

internal static class LRelayPlan
{
    public static LRelayPlanRecord? LRelayPlanCreate(Guid lRelayPlanId, Guid lRelayTarget)
    {
        if (LTabset.LTabsetCurrent is not { } lRelayTabset)
        {
            return null;
        }

        PTabRecord[] lRelayTabs = lRelayTabset.PTabsetRecords.ToArray();
        var lRelayPlan = new LRelayPlanRecord { LRelayPlanId = lRelayPlanId };
        var lRelayStages = new Dictionary<Guid, LRelayStageRecord>();

        Guid LRelayStageCreate(Guid lRelayTabId)
        {
            if (lRelayTabId == Guid.Empty || lRelayTabId == LCourier.LCourierFinishTarget)
            {
                return lRelayTabId;
            }

            if (lRelayStages.TryGetValue(lRelayTabId, out LRelayStageRecord? lRelayExisting))
            {
                return lRelayExisting.LRelayStageId;
            }

            PTabRecord? lRelayTab = lRelayTabs.FirstOrDefault(lRelayItem => lRelayItem.PTabId == lRelayTabId);
            if (lRelayTab is null)
            {
                return Guid.Empty;
            }

            var lRelayStage = new LRelayStageRecord
            {
                LRelayStageId = Guid.NewGuid(),
                LRelayOriginalTab = lRelayTab.PTabId,
                LRelayLayoutKey = lRelayTab.PTabLayoutKey,
                LRelayTitle = lRelayTab.PTabTitle,
                LRelayExport = lRelayTab.PTabWorkspace.PWorkspaceExportState.LPresetRecordCreate(),
                LRelayLayout = lRelayTab.PTabWorkspace.PWorkspaceLayoutRead().LSceneTabClone()
            };
            lRelayStages.Add(lRelayTabId, lRelayStage);

            if (lRelayTab.PTabWorkspace.PWorkspaceSurface is PFunnelTab)
            {
                foreach (LSceneFunnelRule lRelayRule in lRelayStage.LRelayLayout.LSceneFunnelRules)
                {
                    Guid lRelayRuleTarget = lRelayRule.LSceneFunnelTarget >= 0
                        && lRelayRule.LSceneFunnelTarget < lRelayTabs.Length
                        ? LRelayStageCreate(lRelayTabs[lRelayRule.LSceneFunnelTarget].PTabId)
                        : Guid.Empty;
                    lRelayStage.LRelayFunnelRules.Add(new LRelayFunnelRuleRecord
                    {
                        LRelayRule = lRelayRule.LSceneFunnelClone(),
                        LRelayTargetStage = lRelayRuleTarget
                    });
                }
            }
            else
            {
                lRelayStage.LRelayNextStage = LRelayStageCreate(LCourier.LCourierTargetRead(lRelayTabId));
            }

            return lRelayStage.LRelayStageId;
        }

        lRelayPlan.LRelayEntryStage = LRelayStageCreate(lRelayTarget);
        lRelayPlan.LRelayStages = lRelayStages.Values.ToList();
        return lRelayPlan.LRelayEntryStage == Guid.Empty ? null : lRelayPlan;
    }

    public static LRelayPlanRecord LRelayPlanCopy(LRelayPlanRecord lRelayTemplate, Guid lRelayPlanId)
    {
        string lRelayJson = JsonSerializer.Serialize(lRelayTemplate);
        LRelayPlanRecord lRelayCopy = JsonSerializer.Deserialize<LRelayPlanRecord>(lRelayJson)!;
        lRelayCopy.LRelayPlanId = lRelayPlanId;
        lRelayCopy.LRelayCreated = DateTimeOffset.Now;
        lRelayCopy.LRelayDeliveredWork.Clear();
        foreach (LRelayStageRecord lRelayStage in lRelayCopy.LRelayStages)
        {
            lRelayStage.LRelayPendingInputs.Clear();
        }
        return lRelayCopy;
    }

    public static Guid LRelayFunnelTargetRead(LRelayStageRecord lRelayStage, string lRelayPath)
    {
        string lRelayName = Path.GetFileName(lRelayPath);
        foreach (LRelayFunnelRuleRecord lRelayRule in lRelayStage.LRelayFunnelRules)
        {
            if (LRelayRuleMatch(lRelayRule.LRelayRule, lRelayName))
            {
                return lRelayRule.LRelayTargetStage;
            }
        }

        return Guid.Empty;
    }

    private static bool LRelayRuleMatch(LSceneFunnelRule lRelayRule, string lRelayName)
    {
        if (lRelayRule.LSceneFunnelType == (int)PFunnelForm.Regex)
        {
            if (string.IsNullOrWhiteSpace(lRelayRule.LSceneFunnelRegex)) return false;
            try
            {
                string lRelaySubject = lRelayRule.LSceneFunnelWhole
                    ? lRelayName
                    : Path.GetFileNameWithoutExtension(lRelayName);
                return Regex.IsMatch(lRelaySubject, lRelayRule.LSceneFunnelRegex, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException) { return false; }
        }

        var lRelayParts = new[]
        {
            (lRelayRule.LSceneFunnelContains, 0),
            (lRelayRule.LSceneFunnelStart, 1),
            (lRelayRule.LSceneFunnelEnd, 2),
            (lRelayRule.LSceneFunnelExtension, 3)
        };
        bool lRelayHasResult = false;
        bool lRelayResult = false;
        foreach ((LSceneFunnelMatch lRelayMatch, int lRelayKind) in lRelayParts)
        {
            if (string.IsNullOrWhiteSpace(lRelayMatch.LSceneFunnelText)) continue;
            StringComparison lRelayComparison = lRelayMatch.LSceneFunnelCase
                ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            bool lRelayCurrent = lRelayKind switch
            {
                0 => lRelayName.Contains(lRelayMatch.LSceneFunnelText, lRelayComparison),
                1 => lRelayName.StartsWith(lRelayMatch.LSceneFunnelText, lRelayComparison),
                2 => lRelayName.EndsWith(lRelayMatch.LSceneFunnelText, lRelayComparison),
                _ => string.Equals(Path.GetExtension(lRelayName).TrimStart('.'),
                    lRelayMatch.LSceneFunnelText.TrimStart('.'), lRelayComparison)
            };
            lRelayResult = !lRelayHasResult
                ? lRelayCurrent
                : lRelayMatch.LSceneFunnelJoin ? lRelayResult && lRelayCurrent : lRelayResult || lRelayCurrent;
            lRelayHasResult = true;
        }

        return lRelayHasResult && lRelayResult;
    }
}
