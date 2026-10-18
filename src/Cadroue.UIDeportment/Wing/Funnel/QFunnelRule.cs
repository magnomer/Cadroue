using Cadroue.Core;

namespace Cadroue.UIDeportment;

public enum QFunnelForm { QFunnelFormFilename, QFunnelFormRegex, QFunnelFormRemainder }

public enum QFunnelKind { QFunnelKindContains, QFunnelKindPrefix, QFunnelKindEnd, QFunnelKindExtension }

public sealed record QFunnelCondition(
    QFunnelKind QFunnelConditionKind,
    string QFunnelConditionLabel,
    bool QFunnelConditionJoin);

public sealed record QFunnelTarget(Guid QFunnelTargetId, string QFunnelTargetTitle, string QFunnelTargetKey);

public sealed record QFunnelSlot(
    QFunnelRule QFunnelSlotRule,
    int QFunnelSlotIndex,
    int QFunnelSlotOrder,
    bool QFunnelSlotSelected);

public sealed class QFunnelRule
{
    private static readonly IReadOnlyDictionary<QFunnelForm, string> qFunnelTitles =
        new Dictionary<QFunnelForm, string>
        {
            [QFunnelForm.QFunnelFormFilename] = "Inspector.Funnel.Filename",
            [QFunnelForm.QFunnelFormRegex] = "Inspector.Funnel.Regex",
            [QFunnelForm.QFunnelFormRemainder] = "Inspector.Funnel.Remainder",
        };

    public QFunnelRule(QFunnelForm lForm)
    {
        QFunnelRuleForm = lForm;
    }

    public QFunnelForm QFunnelRuleForm { get; }

    public string QFunnelRuleTitle => qFunnelTitles[QFunnelRuleForm];

    public bool QFunnelRulePattern => QFunnelRuleForm == QFunnelForm.QFunnelFormRegex;

    public bool QFunnelRuleFields => QFunnelRuleForm == QFunnelForm.QFunnelFormFilename;

    public LSceneFunnelCondition QFunnelRuleContains { get; internal set; } = new();

    public LSceneFunnelCondition QFunnelRulePrefix { get; internal set; } = new();

    public LSceneFunnelCondition QFunnelRuleEnd { get; internal set; } = new();

    public LSceneFunnelCondition QFunnelRuleExtension { get; internal set; } = new();

    public string QFunnelRuleRegex { get; internal set; } = string.Empty;

    public bool QFunnelRuleWhole { get; internal set; }

    public Guid QFunnelRuleTarget { get; internal set; }

    public int QFunnelRulePending { get; internal set; } = -1;

    public bool QFunnelRuleCollapsed { get; internal set; }
}
