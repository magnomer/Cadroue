namespace Cadroue.Core;

public sealed class LCartographerPlanDocument
{
    public Guid LCartographerPlanId { get; set; }
    public Guid LCartographerEntryStage { get; set; }
    public DateTimeOffset LCartographerCreated { get; set; } = DateTimeOffset.Now;
    public List<LCartographerStageEntry> LCartographerStages { get; set; } = new();
    public HashSet<Guid> LCartographerDeliveredWork { get; set; } = new();
}

public sealed class LCartographerStageEntry
{
    public Guid LCartographerStageId { get; set; }
    public Guid LCartographerOriginalTab { get; set; }
    public string LCartographerLayoutKey { get; set; } = string.Empty;
    public string LCartographerTitle { get; set; } = string.Empty;
    public Guid LCartographerNextStage { get; set; }
    public LPresetDocument LCartographerExport { get; set; } = new();
    public LSceneTabEntry LCartographerLayout { get; set; } = new();
    public List<LCartographerFunnelRule> LCartographerFunnelRules { get; set; } = new();
    public List<LCartographerInputEntry> LCartographerPendingInputs { get; set; } = new();
}

public sealed class LCartographerFunnelRule
{
    public LSceneFunnelRule LCartographerRule { get; set; } = new();
    public Guid LCartographerTargetStage { get; set; }
}

public sealed class LCartographerInputEntry
{
    public string LCartographerPath { get; set; } = string.Empty;
    public Guid LCartographerSourceStage { get; set; }
}
