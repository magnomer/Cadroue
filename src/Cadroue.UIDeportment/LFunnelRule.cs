using Cadroue.Core;

namespace Cadroue.UIDeportment;

public enum LFunnelForm { LFunnelFormFilename, LFunnelFormRegex, LFunnelFormRemainder }

public enum LFunnelKind { LFunnelKindContains, LFunnelKindPrefix, LFunnelKindEnd, LFunnelKindExtension }

public sealed record LFunnelCondition(
    LFunnelKind LFunnelConditionKind,
    string LFunnelConditionLabel,
    bool LFunnelConditionJoin);

public sealed record LFunnelTarget(Guid LFunnelTargetId, string LFunnelTargetTitle, string LFunnelTargetKey);

public sealed record LFunnelSlot(
    LFunnelRule LFunnelSlotRule,
    int LFunnelSlotIndex,
    int LFunnelSlotOrder,
    bool LFunnelSlotSelected);

public sealed class LFunnelRule
{
    private static readonly IReadOnlyDictionary<LFunnelForm, string> lFunnelTitles =
        new Dictionary<LFunnelForm, string>
        {
            [LFunnelForm.LFunnelFormFilename] = "Inspector.Funnel.Filename",
            [LFunnelForm.LFunnelFormRegex] = "Inspector.Funnel.Regex",
            [LFunnelForm.LFunnelFormRemainder] = "Inspector.Funnel.Remainder",
        };

    public LFunnelRule(LFunnelForm lForm)
    {
        LFunnelRuleForm = lForm;
    }

    public LFunnelForm LFunnelRuleForm { get; }

    public string LFunnelRuleTitle => lFunnelTitles[LFunnelRuleForm];

    public bool LFunnelRulePattern => LFunnelRuleForm == LFunnelForm.LFunnelFormRegex;

    public bool LFunnelRuleFields => LFunnelRuleForm == LFunnelForm.LFunnelFormFilename;

    public LSceneFunnelMatch LFunnelRuleContains { get; internal set; } = new();

    public LSceneFunnelMatch LFunnelRulePrefix { get; internal set; } = new();

    public LSceneFunnelMatch LFunnelRuleEnd { get; internal set; } = new();

    public LSceneFunnelMatch LFunnelRuleExtension { get; internal set; } = new();

    public string LFunnelRuleRegex { get; internal set; } = string.Empty;

    public bool LFunnelRuleWhole { get; internal set; }

    public Guid LFunnelRuleTarget { get; internal set; }

    public int LFunnelRulePending { get; internal set; } = -1;

    public bool LFunnelRuleCollapsed { get; internal set; }
}
