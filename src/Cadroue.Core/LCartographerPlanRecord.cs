namespace Cadroue.Core;

public sealed class LCartographerPlanRecord
{
    public Guid LCartographerPlanId { get; set; }
    public Guid LCartographerEntryStage { get; set; }
    public DateTimeOffset LCartographerCreated { get; set; } = DateTimeOffset.Now;
    public List<LCartographerStageRecord> LCartographerStages { get; set; } = new();
    public HashSet<Guid> LCartographerDeliveredWork { get; set; } = new();
}

public sealed class LCartographerStageRecord
{
    public Guid LCartographerStageId { get; set; }
    public Guid LCartographerOriginalTab { get; set; }
    public string LCartographerLayoutKey { get; set; } = string.Empty;
    public string LCartographerTitle { get; set; } = string.Empty;
    public Guid LCartographerNextStage { get; set; }
    public LPresetRecord LCartographerExport { get; set; } = new();
    public LSceneTabRecord LCartographerLayout { get; set; } = new();
    public List<LCartographerFunnelRule> LCartographerFunnelRules { get; set; } = new();
    public List<LCartographerInputRecord> LCartographerPendingInputs { get; set; } = new();
}

public sealed class LCartographerFunnelRule
{
    public LSceneFunnelRule LCartographerRule { get; set; } = new();
    public Guid LCartographerTargetStage { get; set; }
}

public sealed class LCartographerInputRecord
{
    public string LCartographerPath { get; set; } = string.Empty;
    public Guid LCartographerSourceStage { get; set; }
}
