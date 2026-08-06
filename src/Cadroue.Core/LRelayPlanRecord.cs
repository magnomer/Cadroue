namespace Cadroue.Core;

public sealed class LRelayPlanRecord
{
    public Guid LRelayPlanId { get; set; }
    public Guid LRelayEntryStage { get; set; }
    public DateTimeOffset LRelayCreated { get; set; } = DateTimeOffset.Now;
    public List<LRelayStageRecord> LRelayStages { get; set; } = new();
    public HashSet<Guid> LRelayDeliveredWork { get; set; } = new();
}

public sealed class LRelayStageRecord
{
    public Guid LRelayStageId { get; set; }
    public Guid LRelayOriginalTab { get; set; }
    public string LRelayLayoutKey { get; set; } = string.Empty;
    public string LRelayTitle { get; set; } = string.Empty;
    public Guid LRelayNextStage { get; set; }
    public LPresetRecord LRelayExport { get; set; } = new();
    public LSceneTabRecord LRelayLayout { get; set; } = new();
    public List<LFunnelRule> LRelayFunnelRules { get; set; } = new();
    public List<LRelayInputRecord> LRelayPendingInputs { get; set; } = new();
}

public sealed class LFunnelRule
{
    public LSceneFunnelRule LRelayRule { get; set; } = new();
    public Guid LRelayTargetStage { get; set; }
}

public sealed class LRelayInputRecord
{
    public string LRelayPath { get; set; } = string.Empty;
    public Guid LRelaySourceStage { get; set; }
}
